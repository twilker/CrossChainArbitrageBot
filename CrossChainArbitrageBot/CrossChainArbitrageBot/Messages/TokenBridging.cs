using System.Collections.Generic;
using Agents.Net;
using CrossChainArbitrageBot.Models;
using Nethereum.Util;

namespace CrossChainArbitrageBot.Messages
{
    internal class TokenBridging : Message
    {
        public TokenBridging(Message predecessorMessage, BlockchainName sourceChain, double amount, double originalTargetAmount, TokenType tokenType, string targetWallet, int decimals) : base(predecessorMessage)
        {
            SourceChain = sourceChain;
            Amount = amount;
            OriginalTargetAmount = originalTargetAmount;
            TokenType = tokenType;
            TargetWallet = targetWallet;
            Decimals = decimals;
        }

        public TokenBridging(IEnumerable<Message> predecessorMessages, BlockchainName sourceChain, double amount, double originalTargetAmount, TokenType tokenType, string targetWallet, int decimals) : base(predecessorMessages)
        {
            SourceChain = sourceChain;
            Amount = amount;
            OriginalTargetAmount = originalTargetAmount;
            TokenType = tokenType;
            TargetWallet = targetWallet;
            Decimals = decimals;
        }

        public BlockchainName SourceChain { get; }
        public double Amount { get; }
        public double OriginalTargetAmount { get; }
        public TokenType TokenType { get; }
        public string TargetWallet { get; }
        public int Decimals { get; }

        protected override string DataToString()
        {
            return $"{nameof(SourceChain)}: {SourceChain}; {nameof(Amount)}: {Amount}; {nameof(OriginalTargetAmount)}: {OriginalTargetAmount}; {nameof(TokenType)}: {TokenType}," +
                   $"{nameof(TargetWallet)}: {TargetWallet}, {nameof(Decimals)}: {Decimals}";
        }
    }
}
