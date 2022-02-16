using Agents.Net;
using CrossChainArbitrageBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossChainArbitrageBot.Messages
{
    internal class StableTokenBridged : Message
    {
        public StableTokenBridged(Message predecessorMessage, bool success, double amountSend, double originalTargetAmount, BlockchainName targetChain) : base(predecessorMessage)
        {
            Success = success;
            AmountSend = amountSend;
            OriginalTargetAmount = originalTargetAmount;
            TargetChain = targetChain;
        }

        public StableTokenBridged(IEnumerable<Message> predecessorMessages, bool success, double amountSend, double originalTargetAmount, BlockchainName targetChain) : base(predecessorMessages)
        {
            Success = success;
            AmountSend = amountSend;
            OriginalTargetAmount = originalTargetAmount;
            TargetChain = targetChain;
        }

        public bool Success { get; }

        public double AmountSend { get; }

        public double OriginalTargetAmount { get; }

        public BlockchainName TargetChain { get; }

        protected override string DataToString()
        {
            return $"{nameof(Success)}: {Success}; {nameof(AmountSend)}: {AmountSend}; {nameof(OriginalTargetAmount)}: {OriginalTargetAmount}; {nameof(TargetChain)}: {TargetChain}";
        }
    }
}
