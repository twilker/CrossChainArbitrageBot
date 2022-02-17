using System.Collections.Generic;
using Agents.Net;

namespace CrossChainArbitrageBot.Messages
{
    internal class TradeInitiating : Message
    {
        public string FromTokenId { get; }
        public string ToTokenId { get; }
        public double Amount { get; }
        public TradingPlatform Platform { get; }

        public TradeInitiating(Message predecessorMessage, string fromTokenId, string toTokenId, double amount, TradingPlatform platform) : base(predecessorMessage)
        {
            FromTokenId = fromTokenId;
            ToTokenId = toTokenId;
            Amount = amount;
            Platform = platform;
        }

        public TradeInitiating(IEnumerable<Message> predecessorMessages, string fromTokenId, string toTokenId, double amount, TradingPlatform platform) : base(predecessorMessages)
        {
            FromTokenId = fromTokenId;
            ToTokenId = toTokenId;
            Amount = amount;
            Platform = platform;
        }

        protected override string DataToString()
        {
            return $"{nameof(FromTokenId)}: {FromTokenId}; {nameof(ToTokenId)}: {ToTokenId}; {nameof(Amount)}; " +
                   $"{nameof(Platform)}: {Platform}";
        }
    }

    public enum TradingPlatform
    {
        PancakeSwap,
        TraderJoe
    }
}
