using System.Collections.Generic;
using Agents.Net;

namespace CrossChainArbitrageBot.Messages
{
    internal class TradeCompleted : Message
    {
        public TradeCompleted(Message predecessorMessage, bool success) : base(predecessorMessage)
        {
            Success = success;
        }

        public TradeCompleted(IEnumerable<Message> predecessorMessages, bool success) : base(predecessorMessages)
        {
            Success = success;
        }

        public bool Success { get; }

        protected override string DataToString()
        {
            return $"{nameof(Success)}: {Success}";
        }
    }
}
