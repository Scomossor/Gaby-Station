using System;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Gabystation.ServerCurrency.UI
{
    [Serializable, NetSerializable]
    public enum BuyIdList
    {
        AntagToken,
        GhostToken,
        EventToken
    }

    [Serializable, NetSerializable]
    public sealed class CurrencyEuiState : EuiStateBase
    {

    }
    public static class CurrencyEuiMsg
    {
        [Serializable, NetSerializable]
        public sealed class Close : EuiMessageBase
        {
        }

        [Serializable, NetSerializable]
        public sealed class Buy : EuiMessageBase
        {
            public BuyIdList BuyId;
        }
    }
}
