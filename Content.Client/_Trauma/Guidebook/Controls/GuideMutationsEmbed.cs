// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Client.Guidebook.Richtext;
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Client.Guidebook.Controls;

public sealed class GuideMutationsEmbed : BoxContainer, IDocumentTag
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public GuideMutationsEmbed()
    {
        IoCManager.InjectDependencies(this);

        Orientation = LayoutOrientation.Vertical;

        var mutation = _entMan.System<MutationSystem>();

        var names = new List<string>();
        var ids = new List<string>(mutation.AllMutations.Count);
        foreach (var id in mutation.AllMutations.Keys)
        {
            ids.Add(id);
        }
        ids.Sort();
        foreach (var id in ids)
        {
            var comp = mutation.AllMutations[id];
            AddChild(new MutationInfoControl(_proto, mutation, id, comp, names));
        }
    }

    bool IDocumentTag.TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = this;
        return true;
    }
}
