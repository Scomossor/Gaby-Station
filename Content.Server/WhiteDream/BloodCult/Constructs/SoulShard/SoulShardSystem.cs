// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Bible;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Server._EinsteinEngines.Language;
using Content.Shared.Bible.Components;
using Content.Shared.Interaction;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.WhiteDream.BloodCult;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.WhiteDream.BloodCult.Constructs.SoulShard;

public sealed partial class SoulShardSystem : EntitySystem
{
    [Dependency] private AppearanceSystem _appearanceSystem = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private SharedPointLightSystem _lightSystem = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private LanguageSystem _language = default!;
    [Dependency] private SharedRoleSystem _roleSystem = default!;
    [Dependency] private PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SoulShardComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SoulShardComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SoulShardComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<SoulShardComponent, MindAddedMessage>(OnShardMindAdded);
        SubscribeLocalEvent<SoulShardComponent, MindRemovedMessage>(OnShardMindRemoved);
    }

    private void OnMapInit(Entity<SoulShardComponent> shard, ref MapInitEvent args)
    {
        if (!shard.Comp.IsBlessed)
            return;

        _appearanceSystem.SetData(shard, SoulShardVisualState.Blessed, true);
        _lightSystem.SetColor(shard, shard.Comp.BlessedLightColor);
    }

    private void OnActivate(Entity<SoulShardComponent> shard, ref ActivateInWorldEvent args)
    {
        // Dumont
        if (!shard.Comp.IsBlessed)
        {
            if (!HasComp<BloodCultistComponent>(args.User))
                return;

            if (shard.Comp.ShadeUid.HasValue)
            {
                DespawnShade(shard);
                return;
            }

            if (_mind.TryGetMind(shard, out var cultMindId, out _))
                SpawnShade(shard, shard.Comp.ShadeProto, cultMindId);

            return;
        }

        if (shard.Comp.ShadeUid.HasValue)
        {
            DespawnShade(shard);
            return;
        }

        if (_mind.TryGetMind(shard, out var holyMindId, out _))
            SpawnShade(shard, shard.Comp.PurifiedShadeProto, holyMindId);
    }

    private void OnInteractUsing(Entity<SoulShardComponent> shard, ref InteractUsingEvent args)
    {
        if (shard.Comp.IsBlessed || !TryComp(args.Used, out BibleComponent? bible))
            return;

        _popup.PopupEntity(Loc.GetString("bible-sizzle"), args.User, args.User);
        _audio.PlayPvs(bible.HealSoundPath, args.User);
        _appearanceSystem.SetData(shard, SoulShardVisualState.Blessed, true);
        _lightSystem.SetColor(shard, shard.Comp.BlessedLightColor);
        shard.Comp.IsBlessed = true;
    }

    private void OnShardMindAdded(Entity<SoulShardComponent> shard, ref MindAddedMessage args)
    {
        if (!TryComp<MindContainerComponent>(shard, out var mindContainer) || mindContainer.Mind is not { } mind)
            return;

        _roleSystem.MindRemoveRole<TraitorRoleComponent>(mind);
        _language.UpdateEntityLanguages(shard.Owner);
        UpdateGlowVisuals(shard, true);
    }

    private void OnShardMindRemoved(Entity<SoulShardComponent> shard, ref MindRemovedMessage args) =>
        UpdateGlowVisuals(shard, false);

    private void SpawnShade(Entity<SoulShardComponent> shard, EntProtoId proto, EntityUid mindId)
    {
        var position = _transform.GetMapCoordinates(shard);
        var shadeUid = Spawn(proto, position);
        _mind.TransferTo(mindId, shadeUid);
        _mind.UnVisit(mindId);
        shard.Comp.ShadeUid = shadeUid;
    }

    private void DespawnShade(Entity<SoulShardComponent> shard)
    {
        if (shard.Comp.ShadeUid is not { } shade || TerminatingOrDeleted(shade))
        {
            shard.Comp.ShadeUid = null;
            return;
        }

        if (_mind.TryGetMind(shade, out var mindId, out _))
        {
            _mind.TransferTo(mindId, shard);
            _mind.UnVisit(mindId);
        }

        QueueDel(shade);
        shard.Comp.ShadeUid = null;
    }

    private void UpdateGlowVisuals(Entity<SoulShardComponent> shard, bool state)
    {
        _appearanceSystem.SetData(shard, SoulShardVisualState.HasMind, state);
        _lightSystem.SetEnabled(shard, state);
    }
}
