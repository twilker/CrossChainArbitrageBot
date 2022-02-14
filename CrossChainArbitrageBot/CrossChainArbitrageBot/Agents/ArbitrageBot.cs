using Agents.Net;
using CrossChainArbitrageBot.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossChainArbitrageBot.Agents
{
    [Produces(typeof(PancakeSwapTradeInitiating))]
    internal class ArbitrageBot : Agent
    {
        public ArbitrageBot(IMessageBoard messageBoard) : base(messageBoard)
        {
        }

        protected override void ExecuteCore(Message messageData)
        {
        }
    }
}
