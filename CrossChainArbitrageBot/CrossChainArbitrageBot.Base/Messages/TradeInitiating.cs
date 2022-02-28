using Agents.Net;
using CrossChainArbitrageBot.Base.Models;

namespace CrossChainArbitrageBot.Base.Messages;

public class TradeInitiating : Message
{
    public string FromTokenId { get; }
    public string ToTokenId { get; }
    public double Amount { get; }
    public int FromTokenDecimals { get; }
    public TradingPlatform Platform { get; }
    public string WalletAddress { get; }
    public Liquidity LiquidityPair { get; }
    public double OriginalTargetAmount { get; }
    public double ExpectedAmount { get; }
    public TokenType TokenType { get; }

    public TradeInitiating(Message predecessorMessage, string fromTokenId, string toTokenId, double amount, TradingPlatform platform, int fromTokenDecimals, string walletAddress, Liquidity liquidityPair, double originalTargetAmount, double expectedAmount, TokenType tokenType) : base(predecessorMessage)
    {
        FromTokenId = fromTokenId;
        ToTokenId = toTokenId;
        Amount = amount;
        Platform = platform;
        FromTokenDecimals = fromTokenDecimals;
        WalletAddress = walletAddress;
        LiquidityPair = liquidityPair;
        OriginalTargetAmount = originalTargetAmount;
        ExpectedAmount = expectedAmount;
        TokenType = tokenType;
    }

    public TradeInitiating(IEnumerable<Message> predecessorMessages, string fromTokenId, string toTokenId, double amount, TradingPlatform platform, int fromTokenDecimals, string walletAddress, Liquidity liquidityPair, double originalTargetAmount, double expectedAmount, TokenType tokenType) : base(predecessorMessages)
    {
        FromTokenId = fromTokenId;
        ToTokenId = toTokenId;
        Amount = amount;
        Platform = platform;
        FromTokenDecimals = fromTokenDecimals;
        WalletAddress = walletAddress;
        LiquidityPair = liquidityPair;
        OriginalTargetAmount = originalTargetAmount;
        ExpectedAmount = expectedAmount;
        TokenType = tokenType;
    }

    protected override string DataToString()
    {
        return $"{nameof(FromTokenId)}: {FromTokenId}; {nameof(FromTokenDecimals)}: {FromTokenDecimals}; " +
               $"{nameof(ToTokenId)}: {ToTokenId}; {nameof(Amount)}; {nameof(Platform)}: {Platform}; " +
               $"{nameof(WalletAddress)}: {WalletAddress}; {nameof(LiquidityPair)}: {LiquidityPair}; " +
               $"{nameof(OriginalTargetAmount)}: {OriginalTargetAmount}, " +
               $"{nameof(ExpectedAmount)}: {ExpectedAmount}, " +
               $"{nameof(TokenType)}: {TokenType}";
    }
}

public enum TradingPlatform
{
    PancakeSwap,
    TraderJoe
}