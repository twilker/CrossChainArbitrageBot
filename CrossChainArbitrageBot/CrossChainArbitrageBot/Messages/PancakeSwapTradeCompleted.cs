using Agents.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossChainArbitrageBot.Messages
{
    internal class PancakeSwapTradeCompleted : Message
    {
        public PancakeSwapTradeCompleted(Message predecessorMessage, bool success) : base(predecessorMessage)
        {
            Success = success;
        }

        public PancakeSwapTradeCompleted(IEnumerable<Message> predecessorMessages, bool success) : base(predecessorMessages)
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
