using Agents.Net;
using CrossChainArbitrageBot.Messages;
using CrossChainArbitrageBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CrossChainArbitrageBot.Agents
{
    [Consumes(typeof(TransactionStarted))]
    [Consumes(typeof(DataUpdated))]
    [Consumes(typeof(PancakeSwapTradeCompleted))]
    [Consumes(typeof(StableTokenBridged))]
    [Produces(typeof(PancakeSwapTradeInitiating))]
    [Produces(typeof(TransactionFinished))]
    internal class ArbitrageBot : Agent
    {
        private readonly MessageCollector<DataUpdated, TransactionStarted> collector;
        private int ongoingTransaction = 0;

        public ArbitrageBot(IMessageBoard messageBoard) : base(messageBoard)
        {
            collector = new MessageCollector<DataUpdated,TransactionStarted>(OnCollected);
        }

        private void OnCollected(MessageCollection<DataUpdated, TransactionStarted> set)
        {
            set.MarkAsConsumed(set.Message2);
            if(Interlocked.Exchange(ref ongoingTransaction, 1) != 0)
            {
                OnMessage(new TransactionFinished(set, TransactionResult.Rejected));
            }

            switch (set.Message2.Chain)
            {
                case Models.BlockchainName.Bsc:
                    DataUpdate lastUpdate = set.Message1.Updates.First((u) => u.BlockchainName == Models.BlockchainName.Bsc);
                    switch (set.Message2.Type)
                    {
                        case Models.TransactionType.StableToUnstable:
                            OnMessage(new ImportantNotice(set, $"Trading {lastUpdate.StableAmount * set.Message2.TransactionAmount:F2} {lastUpdate.StableSymbol} for {lastUpdate.UnstableSymbol}"));
                            OnMessage(new PancakeSwapTradeInitiating(set, lastUpdate.StableId, lastUpdate.UnstableId,
                                                                     lastUpdate.StableAmount * set.Message2.TransactionAmount));
                            break;
                        case Models.TransactionType.UnstableToStable:
                            OnMessage(new ImportantNotice(set, $"Trading {lastUpdate.UnstableAmount * set.Message2.TransactionAmount:F2} {lastUpdate.UnstableSymbol} for {lastUpdate.StableSymbol}"));
                            OnMessage(new PancakeSwapTradeInitiating(set, lastUpdate.UnstableId, lastUpdate.StableId,
                                                                     lastUpdate.UnstableAmount * set.Message2.TransactionAmount));
                            break;
                        case Models.TransactionType.BridgeStable:
                            var targetUpdate = set.Message1.Updates.First((u) => u.BlockchainName == Models.BlockchainName.Avalanche);
                            OnMessage(new ImportantNotice(set, $"Bridging {lastUpdate.StableAmount * set.Message2.TransactionAmount:F2} {lastUpdate.StableSymbol} to Avalanche"));
                            OnMessage(new StableTokenBridging(set, Models.BlockchainName.Bsc,
                                                              lastUpdate.StableAmount * set.Message2.TransactionAmount,
                                                              targetUpdate.StableAmount));
                            break;
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

        public record AwaitingBridgeAmount(BlockchainName TargetChain, double OriginalAmount, double SendAmount, Message OriginalMessage);
        private AwaitingBridgeAmount? awaitingBridgeAmount;

        protected override async void ExecuteCore(Message messageData)
        {
            if(messageData.TryGet(out PancakeSwapTradeCompleted tradeCompleted))
            {
                ongoingTransaction = 0;
                OnMessage(new TransactionFinished(messageData, tradeCompleted.Success 
                                                                ? TransactionResult.Success 
                                                                : TransactionResult.Failed));
                return;
            }
            else if(messageData.TryGet(out StableTokenBridged tokenBridged))
            {
                if (tokenBridged.Success)
                {
                    awaitingBridgeAmount = new AwaitingBridgeAmount(tokenBridged.TargetChain, tokenBridged.OriginalTargetAmount,
                                                                    tokenBridged.AmountSend, tokenBridged);
                }
                else
                {
                    ongoingTransaction = 0;
                    OnMessage(new TransactionFinished(messageData, TransactionResult.Failed));
                }
                return;
            }
            else if(messageData.TryGet(out DataUpdated dataUpdated) && 
                    awaitingBridgeAmount != null &&
                    dataUpdated.Updates.First(u => u.BlockchainName == awaitingBridgeAmount.TargetChain)
                               .StableAmount >= awaitingBridgeAmount.OriginalAmount+awaitingBridgeAmount.SendAmount*0.5)
            {
                //TODO potential double execution here
                OnMessage(new TransactionFinished(awaitingBridgeAmount.OriginalMessage, TransactionResult.Success));
                awaitingBridgeAmount = null;
            }
            collector.Push(messageData);
        }
    }
}
