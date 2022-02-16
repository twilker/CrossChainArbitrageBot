using Agents.Net;
using CrossChainArbitrageBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossChainArbitrageBot.Messages
{
    internal class TransactionStarted : Message
    {
        public TransactionStarted(Message predecessorMessage, double transactionAmount, BlockchainName chain, TransactionType type) : base(predecessorMessage)
        {
            TransactionAmount = transactionAmount;
            Chain = chain;
            Type = type;
        }

        public TransactionStarted(IEnumerable<Message> predecessorMessages, double transactionAmount, BlockchainName chain, TransactionType type) : base(predecessorMessages)
        {
            TransactionAmount = transactionAmount;
            Chain = chain;
            Type = type;
        }

        public double TransactionAmount { get; }
        public BlockchainName Chain { get; }
        public TransactionType Type { get; }

        protected override string DataToString()
        {
            return $"{nameof(TransactionAmount)}: {TransactionAmount}; {nameof(Chain)}: {Chain}; {nameof(Type)}: {Type}";
        }
    }
}
