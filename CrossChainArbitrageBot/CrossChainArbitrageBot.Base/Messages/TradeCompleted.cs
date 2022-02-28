using Agents.Net;
using CrossChainArbitrageBot.Base.Models;

namespace CrossChainArbitrageBot.Base.Messages;

public class TradeCompleted : Message
{
    public TradeCompleted(Message predecessorMessage, bool success, BlockchainName blockchainName, double originalAmount, double amountExpected, TokenType tokenType) : base(predecessorMessage)
    {
        Success = success;
        BlockchainName = blockchainName;
        OriginalAmount = originalAmount;
        AmountExpected = amountExpected;
        TokenType = tokenType;
    }

    public TradeCompleted(IEnumerable<Message> predecessorMessages, bool success, BlockchainName blockchainName, double originalAmount, double amountExpected, TokenType tokenType) : base(predecessorMessages)
    {
        Success = success;
        BlockchainName = blockchainName;
        OriginalAmount = originalAmount;
        AmountExpected = amountExpected;
        TokenType = tokenType;
    }

    public bool Success { get; }
    public BlockchainName BlockchainName { get; }
    public double OriginalAmount { get; }
    public double AmountExpected { get; }
    public TokenType TokenType { get; }

    protected override string DataToString()
    {
        return $"{nameof(Success)}: {Success}, {nameof(BlockchainName)}: {BlockchainName}, {nameof(OriginalAmount)}: {OriginalAmount}, {nameof(AmountExpected)}: {AmountExpected}, {nameof(TokenType)}: {TokenType}";
    }
}