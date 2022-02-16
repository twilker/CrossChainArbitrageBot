using Agents.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossChainArbitrageBot.Messages
{
    internal class TradeInitiating : Message
    {
        public string FromTokenId { get; }
        public string ToTokenId { get; }
        public double Amount { get; }

        public TradeInitiating(Message predecessorMessage, string fromTokenId, string toTokenId, double amount) : base(predecessorMessage)
        {
            FromTokenId = fromTokenId;
            ToTokenId = toTokenId;
            Amount = amount;
        }

        public TradeInitiating(IEnumerable<Message> predecessorMessages, string fromTokenId, string toTokenId, double amount) : base(predecessorMessages)
        {
            FromTokenId = fromTokenId;
            ToTokenId = toTokenId;
            Amount = amount;
        }

        protected override string DataToString()
        {
            return $"{nameof(FromTokenId)}: {FromTokenId}; {nameof(ToTokenId)}: {ToTokenId}; {nameof(Amount)}";
        }
    }

    public enum TradingPlatform
    {
        PancakeSwap,
        TraderJoe
    }
}
