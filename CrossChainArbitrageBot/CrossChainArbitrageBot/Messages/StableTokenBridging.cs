using System.Collections.Generic;
using Agents.Net;
using CrossChainArbitrageBot.Models;

namespace CrossChainArbitrageBot.Messages
{
    internal class StableTokenBridging : Message
    {
        public StableTokenBridging(Message predecessorMessage, BlockchainName sourceChain, double amount, double originalTargetAmount) : base(predecessorMessage)
        {
            SourceChain = sourceChain;
            Amount = amount;
            OriginalTargetAmount = originalTargetAmount;
        }

        public StableTokenBridging(IEnumerable<Message> predecessorMessages, BlockchainName sourceChain, double amount, double originalTargetAmount) : base(predecessorMessages)
        {
            SourceChain = sourceChain;
            Amount = amount;
            OriginalTargetAmount = originalTargetAmount;
        }

        public BlockchainName SourceChain { get; }
        public double Amount { get; }
        public double OriginalTargetAmount { get; }

        protected override string DataToString()
        {
            return $"{nameof(SourceChain)}: {SourceChain}; {nameof(Amount)}: {Amount}; {nameof(OriginalTargetAmount)}: {OriginalTargetAmount}";
        }
    }
}
