using Content.Shared.Armor;
using Content.Shared.Examine;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared._Mono.Emp;

public sealed class EmpResistanceSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<EmpResistanceComponent, GetVerbsEvent<ExamineVerb>>(OnExamine);
    }

    private void OnExamine(Entity<EmpResistanceComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        var msg = FormatEmp(ent);

        _examine.AddDetailedExamineVerb(args, ent.Comp, msg,
            Loc.GetString("battery-examinable-verb-text"),
            "/Textures/Interface/VerbIcons/smite.svg.192dpi.png",
            Loc.GetString("battery-examinable-verb-message"));
    }

    private FormattedMessage FormatEmp(Entity<EmpResistanceComponent> ent)
    {
        var msg = new FormattedMessage();
        msg.AddMarkupOrThrow(Loc.GetString("battery-examine-emp", ("num", MathF.Round((1f - ent.Comp.Coefficient) * 100, 5))));
        return msg;
    }
}
