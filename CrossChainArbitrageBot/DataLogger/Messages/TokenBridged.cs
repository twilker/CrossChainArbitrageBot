using Agents.Net;
using DataLogger.Models;

namespace DataLogger.Messages
{
    internal class TokenBridged : Message
    {
        public TokenBridged(Message predecessorMessage, bool success, double amountSend, double originalTargetAmount, BlockchainName targetChain, TokenType tokenType) : base(predecessorMessage)
        {
            Success = success;
            AmountSend = amountSend;
            OriginalTargetAmount = originalTargetAmount;
            TargetChain = targetChain;
            TokenType = tokenType;
        }

        public TokenBridged(IEnumerable<Message> predecessorMessages, bool success, double amountSend, double originalTargetAmount, BlockchainName targetChain, TokenType tokenType) : base(predecessorMessages)
        {
            Success = success;
            AmountSend = amountSend;
            OriginalTargetAmount = originalTargetAmount;
            TargetChain = targetChain;
            TokenType = tokenType;
        }

        public bool Success { get; }

        public double AmountSend { get; }

        public double OriginalTargetAmount { get; }
        
        public TokenType TokenType { get; }

        public BlockchainName TargetChain { get; }

        protected override string DataToString()
        {
            return $"{nameof(Success)}: {Success}; {nameof(AmountSend)}: {AmountSend}; {nameof(OriginalTargetAmount)}: {OriginalTargetAmount}; {nameof(TargetChain)}: {TargetChain};" +
                   $"{nameof(TokenType)}: {TokenType}";
        }
    }
}
