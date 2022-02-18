using System.Collections.Generic;
using Agents.Net;

namespace CrossChainArbitrageBot.Messages
{
    internal class TradeInitiating : Message
    {
        public string FromTokenId { get; }
        public string ToTokenId { get; }
        public double Amount { get; }
        public int FromTokenDecimals { get; }
        public TradingPlatform Platform { get; }

        public TradeInitiating(Message predecessorMessage, string fromTokenId, string toTokenId, double amount, TradingPlatform platform, int fromTokenDecimals) : base(predecessorMessage)
        {
            FromTokenId = fromTokenId;
            ToTokenId = toTokenId;
            Amount = amount;
            Platform = platform;
            FromTokenDecimals = fromTokenDecimals;
        }

        public TradeInitiating(IEnumerable<Message> predecessorMessages, string fromTokenId, string toTokenId, double amount, TradingPlatform platform, int fromTokenDecimals) : base(predecessorMessages)
        {
            FromTokenId = fromTokenId;
            ToTokenId = toTokenId;
            Amount = amount;
            Platform = platform;
            FromTokenDecimals = fromTokenDecimals;
        }

        protected override string DataToString()
        {
            return $"{nameof(FromTokenId)}: {FromTokenId}; {nameof(FromTokenDecimals)}: {FromTokenDecimals}; " +
                   $"{nameof(ToTokenId)}: {ToTokenId}; {nameof(Amount)}; {nameof(Platform)}: {Platform}";
        }
    }

    public enum TradingPlatform
    {
        PancakeSwap,
        TraderJoe
    }
}
