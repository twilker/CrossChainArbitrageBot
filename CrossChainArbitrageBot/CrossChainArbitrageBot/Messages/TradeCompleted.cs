using Agents.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
