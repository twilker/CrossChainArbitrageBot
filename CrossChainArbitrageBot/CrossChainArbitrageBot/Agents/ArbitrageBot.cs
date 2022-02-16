using Agents.Net;
using CrossChainArbitrageBot.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossChainArbitrageBot.Agents
{
    [Consumes(typeof(TransactionStarted))]
    [Consumes(typeof(DataUpdated))]
    [Produces(typeof(PancakeSwapTradeInitiating))]
    internal class ArbitrageBot : Agent
    {
        private readonly MessageCollector<DataUpdated, TransactionStarted> collector;

        public ArbitrageBot(IMessageBoard messageBoard) : base(messageBoard)
        {
            collector = new MessageCollector<DataUpdated,TransactionStarted>(OnCollected);
        }

        private void OnCollected(MessageCollection<DataUpdated, TransactionStarted> set)
        {
            set.MarkAsConsumed(set.Message2);
            //TODO Block new transactions as long as transsaction is running - meaning this class sends finished and blocked messages

            switch (set.Message2.Chain)
            {
                case Models.BlockchainName.Bsc:
                    DataUpdate lastUpdate = set.Message1.Updates.First((u) => u.BlockchainName == Models.BlockchainName.Bsc);
                    switch (set.Message2.Type)
                    {
                        case Models.TransactionType.StableToUnstable:
                            OnMessage(new PancakeSwapTradeInitiating(set, lastUpdate.StableId, lastUpdate.UnstableId,
                                                                     lastUpdate.StableAmount * set.Message2.TransactionAmount));
                            break;
                        case Models.TransactionType.UnstableToStable:
                            OnMessage(new PancakeSwapTradeInitiating(set, lastUpdate.UnstableId, lastUpdate.StableId,
                                                                     lastUpdate.UnstableAmount * set.Message2.TransactionAmount));
                            break;
                        case Models.TransactionType.Bridge:
                            throw new InvalidOperationException("Not Implemented.");
                        case Models.TransactionType.StableToGas:
                            throw new InvalidOperationException("Not Implemented.");
                        default:
                            throw new InvalidOperationException("Not Implemented.");
                    }
                    break;
                case Models.BlockchainName.Avalanche:
                    throw new InvalidOperationException("Not Implemented.");
                default:
                    throw new InvalidOperationException("Not Implemented.");
            }
        }

        protected override void ExecuteCore(Message messageData)
        {
            collector.Push(messageData);
        }
    }
}
