using System.Collections.Generic;
using Agents.Net;

namespace CrossChainArbitrageBot.Messages;

internal class TradeInitiating : Message
{
    public string FromTokenId { get; }
    public string ToTokenId { get; }
    public double Amount { get; }
    public int FromTokenDecimals { get; }
    public TradingPlatform Platform { get; }
    public string WalletAddress { get; }
    public Liquidity LiquidityPair { get; }

    public TradeInitiating(Message predecessorMessage, string fromTokenId, string toTokenId, double amount, TradingPlatform platform, int fromTokenDecimals, string walletAddress, Liquidity liquidityPair) : base(predecessorMessage)
    {
        FromTokenId = fromTokenId;
        ToTokenId = toTokenId;
        Amount = amount;
        Platform = platform;
        FromTokenDecimals = fromTokenDecimals;
        WalletAddress = walletAddress;
        LiquidityPair = liquidityPair;
    }

    public TradeInitiating(IEnumerable<Message> predecessorMessages, string fromTokenId, string toTokenId, double amount, TradingPlatform platform, int fromTokenDecimals, string walletAddress, Liquidity liquidityPair) : base(predecessorMessages)
    {
        FromTokenId = fromTokenId;
        ToTokenId = toTokenId;
        Amount = amount;
        Platform = platform;
        FromTokenDecimals = fromTokenDecimals;
        WalletAddress = walletAddress;
        LiquidityPair = liquidityPair;
    }

    protected override string DataToString()
    {
        return $"{nameof(FromTokenId)}: {FromTokenId}; {nameof(FromTokenDecimals)}: {FromTokenDecimals}; " +
               $"{nameof(ToTokenId)}: {ToTokenId}; {nameof(Amount)}; {nameof(Platform)}: {Platform}; " +
               $"{nameof(WalletAddress)}: {WalletAddress}; {nameof(LiquidityPair)}: {LiquidityPair}";
    }
}

public enum TradingPlatform
{
    PancakeSwap,
    TraderJoe
}