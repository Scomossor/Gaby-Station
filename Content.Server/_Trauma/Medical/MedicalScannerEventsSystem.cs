// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Cloning.Components;
using Content.Server.Medical.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Trauma.Common.Medical;
using Robust.Shared.Containers;

namespace Content.Trauma.Server.Medical;

public sealed class MedicalScannerEventsSystem : EntitySystem
{
    [Dependency] private SharedDeviceLinkSystem _links = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedicalScannerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MedicalScannerComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<MedicalScannerComponent, EntInsertedIntoContainerMessage>(OnSubjectInserted);
        SubscribeLocalEvent<MedicalScannerComponent, EntRemovedFromContainerMessage>(OnSubjectRemoved);
    }

    private void OnMapInit(Entity<MedicalScannerComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<DeviceLinkSinkComponent>(ent, out var sink))
            return;

        foreach (var fonte in sink.LinkedSources)
        {
            if (!HasComp<DeviceLinkSourceComponent>(fonte))
                continue;

            foreach (var (saida, entrada) in _links.GetLinks(fonte, ent.Owner))
            {
                if (saida != CloningConsoleComponent.ScannerPort || entrada != MedicalScannerComponent.ScannerPort)
                    continue;

                var ev = new ScannerConnectedEvent(ent.Owner);
                RaiseLocalEvent(fonte, ref ev);
                ent.Comp.ConnectedConsole = fonte;
                return;
            }
        }
    }

    private void OnNewLink(EntityUid uid, MedicalScannerComponent comp, NewLinkEvent args)
    {
        if (args.Sink != uid
            || args.SinkPort != MedicalScannerComponent.ScannerPort
            || args.SourcePort != CloningConsoleComponent.ScannerPort)
            return;

        var ev = new ScannerConnectedEvent(uid);
        RaiseLocalEvent(args.Source, ref ev);
        comp.ConnectedConsole = args.Source;
    }

    private void OnSubjectInserted(Entity<MedicalScannerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (ent.Comp.ConnectedConsole is not { } console || args.Container != ent.Comp.BodyContainer)
            return;

        var ev = new ScannerInsertedEvent(ent.Owner, args.Entity);
        RaiseLocalEvent(console, ref ev);
    }

    private void OnSubjectRemoved(Entity<MedicalScannerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (ent.Comp.ConnectedConsole is not { } console || args.Container != ent.Comp.BodyContainer)
            return;

        var ev = new ScannerEjectedEvent(ent.Owner, args.Entity);
        RaiseLocalEvent(console, ref ev);
    }
}
