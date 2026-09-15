// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Actions;
using Content.Server.Mind;
using Content.Server.WhiteDream.BloodCult.Gamerule;
using Content.Server._EinsteinEngines.Language;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.WhiteDream.BloodCult;
using Content.Shared.WhiteDream.BloodCult.Constructs;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;

namespace Content.Server.WhiteDream.BloodCult.Constructs;

public sealed partial class ConstructSystem : EntitySystem
{
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private AppearanceSystem _appearanceSystem = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private LanguageSystem _language = default!;
    [Dependency] private MindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConstructComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ConstructComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<ConstructComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<ConstructComponent, MobStateChangedEvent>(OnConstructStateChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ConstructComponent>();
        while (query.MoveNext(out var uid, out var construct))
        {
            if (!construct.Transforming)
                continue;

            construct.TransformAccumulator += frameTime;
            if (construct.TransformAccumulator < construct.TransformDelay)
                continue;

            construct.TransformAccumulator = 0f;
            construct.Transforming = false;
            _appearanceSystem.SetData(uid, ConstructVisualsState.Transforming, false);
        }
    }

    private void OnMindAdded(Entity<ConstructComponent> construct, ref MindAddedMessage args) =>
        _language.UpdateEntityLanguages(construct.Owner);

    private void OnMapInit(Entity<ConstructComponent> construct, ref MapInitEvent args)
    {
        foreach (var actionId in construct.Comp.Actions)
        {
            var action = _actions.AddAction(construct, actionId);
            construct.Comp.ActionEntities.Add(action);
        }

        _appearanceSystem.SetData(construct, ConstructVisualsState.Transforming, true);
        construct.Comp.Transforming = true;
        var cultistRule = EntityQueryEnumerator<BloodCultRuleComponent>();
        while (cultistRule.MoveNext(out _, out var rule))
        {
            rule.Constructs.Add(construct);
            // Dumont
            rule.TotalConstructs++;
        }
    }

    /// <summary>
    ///     WhiteDream - a shattering construct breaks apart on death, drops the soul it was carrying
    ///     as a shard and leaves nothing else behind.
    /// </summary>
    private void OnConstructStateChanged(Entity<ConstructComponent> construct, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead || !construct.Comp.ShattersOnDeath)
            return;

        var coordinates = Transform(construct.Owner).Coordinates;

        _audio.PlayPvs(construct.Comp.ShatterSound, coordinates, AudioParams.Default.WithVolume(2f));

        if (construct.Comp.ShatterEffect is { } effect)
            Spawn(effect, coordinates);

        // Whoever was piloting it goes back into a shard, so the cult can rebuild them.
        if (_mind.TryGetMind(construct.Owner, out var mindId, out _))
        {
            var shard = Spawn(construct.Comp.ShardProto, coordinates);
            _mind.TransferTo(mindId, shard);
            _mind.UnVisit(mindId);
            _language.UpdateEntityLanguages(shard);
        }
        else
        {
            Spawn(construct.Comp.ShardGhostProto, coordinates);
        }

        QueueDel(construct.Owner);
    }

    private void OnComponentShutdown(Entity<ConstructComponent> construct, ref ComponentShutdown args)
    {
        foreach (var actionEntity in construct.Comp.ActionEntities)
            _actions.RemoveAction(actionEntity);

        var cultistRule = EntityQueryEnumerator<BloodCultRuleComponent>();
        while (cultistRule.MoveNext(out _, out var rule))
            rule.Constructs.Remove(construct);
    }
}
