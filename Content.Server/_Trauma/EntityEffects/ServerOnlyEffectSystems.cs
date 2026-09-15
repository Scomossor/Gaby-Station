// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared.Atmos.Components;
using Content.Shared.Chat;
using Content.Shared.EntityEffects;
using Content.Shared.Random.Helpers;
using Content.Shared.Speech;
using Content.Trauma.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Trauma.Server.EntityEffects;

public sealed partial class RevertPolymorphEffectSystem : TraumaEntityEffectSystem<PolymorphedEntityComponent, RevertPolymorph>
{
    [Dependency] private PolymorphSystem _polymorph = default!;

    protected override void Effect(Entity<PolymorphedEntityComponent> ent, RevertPolymorph effect, EntityEffectBaseArgs args)
    {
        _polymorph.Revert(ent.AsNullable());
    }
}

public sealed partial class SpeakEffectSystem : TraumaEntityEffectSystem<SpeechComponent, Speak>
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ChatSystem _chat = default!;

    protected override void Effect(Entity<SpeechComponent> ent, Speak effect, EntityEffectBaseArgs args)
    {
        var proto = _proto.Index(effect.Id);
        var picked = _random.Pick(proto);

        // prepend the prefix
        if (effect.Prefix is { } prefix)
            picked = Loc.GetString(prefix) + picked;

        // this is still logged so admins can know e.g. what started a dispute
        _chat.TrySendInGameICMessage(ent, picked, InGameICChatType.Speak, hideChat: effect.HideChat);
    }
}

public sealed partial class FlammableEffectSystem : TraumaEntityEffectSystem<FlammableComponent, Flammable>
{
    [Dependency] private FlammableSystem _flammable = default!;

    protected override void Effect(Entity<FlammableComponent> ent, Flammable effect, EntityEffectBaseArgs args)
    {
        // The multiplier is determined by if the entity is already on fire, and if the multiplier for existing FireStacks has a value.
        var multiplier = ent.Comp.FireStacks == 0f || effect.MultiplierOnExisting == null
            ? effect.Multiplier
            : effect.MultiplierOnExisting.Value;

        var scale = args is EntityEffectReagentArgs reagent ? reagent.Scale.Float() : 1f;
        _flammable.AdjustFireStacks(ent, scale * multiplier, ent.Comp, fireProtectionPenetration: effect.FireProtectionPenetration);
    }
}
