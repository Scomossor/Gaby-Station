// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body.Part;
using Content.Shared.Interaction;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Interaction;

public sealed partial class ExtraReachSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExtraReachComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ExtraReachComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ExtraReachComponent, BodyPartAddedEvent>(OnPartAdded);
        SubscribeLocalEvent<ExtraReachComponent, BodyPartRemovedEvent>(OnPartRemoved);
        // run before TK so it can use the extra reach for its check
        SubscribeLocalEvent<ExtraReachComponent, InRangeOverrideEvent>(OnRangeOverride,
            before: new[] { typeof(TelekinesisSystem) });
    }

    private void OnStartup(Entity<ExtraReachComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<BodyPartComponent>(ent, out var part) && part.Body is {} body)
            Apply(ent, body);
    }

    private void OnShutdown(Entity<ExtraReachComponent> ent, ref ComponentShutdown args)
    {
        Unapply(ent);
    }

    private void OnPartAdded(Entity<ExtraReachComponent> ent, ref BodyPartAddedEvent args)
    {
        if (args.Part.Owner == ent.Owner && args.Part.Comp.Body is {} body)
            Apply(ent, body);
    }

    private void OnPartRemoved(Entity<ExtraReachComponent> ent, ref BodyPartRemovedEvent args)
    {
        if (args.Part.Owner == ent.Owner)
            Unapply(ent);
    }

    private void OnRangeOverride(Entity<ExtraReachComponent> ent, ref InRangeOverrideEvent args)
    {
        args.Range += ent.Comp.Bonus;
    }

    private void Apply(Entity<ExtraReachComponent> ent, EntityUid body)
    {
        // components are networked this doesnt need to get trolled
        if (_timing.ApplyingState || ent.Comp.AppliedTo != null)
            return;

        ent.Comp.AppliedTo = body;
        ModifyReach(body, ent.Comp.Bonus);
    }

    private void Unapply(Entity<ExtraReachComponent> ent)
    {
        if (_timing.ApplyingState || ent.Comp.AppliedTo is not {} body)
            return;

        ent.Comp.AppliedTo = null;
        ModifyReach(body, -ent.Comp.Bonus);
    }

    public void ModifyReach(EntityUid uid, float reach)
    {
        // don't care if the body is being deleted
        if (TerminatingOrDeleted(uid))
            return;

        var comp = EnsureComp<ExtraReachComponent>(uid);
        comp.Bonus += reach;
        Dirty(uid, comp);

        // remove the component if it goes to 0f
        if (Math.Abs(comp.Bonus) < 0.001f)
            RemComp(uid, comp);
    }
}
