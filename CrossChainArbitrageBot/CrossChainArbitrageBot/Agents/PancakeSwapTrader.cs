using Agents.Net;
using CrossChainArbitrageBot.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossChainArbitrageBot.Agents
{
    [Consumes(typeof(PancakeSwapTradeInitiating))]
    internal class PancakeSwapTrader : Agent
    {
        public PancakeSwapTrader(IMessageBoard messageBoard) : base(messageBoard)
        {
        }

        protected override void ExecuteCore(Message messageData)
        {
            
        }
    }
}
