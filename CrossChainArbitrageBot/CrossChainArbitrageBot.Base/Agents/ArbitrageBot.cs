using System.Configuration;
using Agents.Net;
using CrossChainArbitrageBot.Base.Messages;
using CrossChainArbitrageBot.Base.Models;

namespace CrossChainArbitrageBot.Base.Agents;

[Consumes(typeof(TransactionStarted))]
[Consumes(typeof(DataUpdated))]
[Consumes(typeof(TradeCompleted))]
[Consumes(typeof(TokenBridged))]
[Produces(typeof(TradeInitiating))]
[Produces(typeof(TransactionFinished))]
internal class ArbitrageBot : Agent
{
    private readonly MessageCollector<DataUpdated, TransactionStarted> collector;
    private int ongoingTransaction;

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
            case BlockchainName.Bsc:
                DataUpdate lastUpdate = set.Message1.Updates.First(u => u.BlockchainName == BlockchainName.Bsc);
                switch (set.Message2.Type)
                {
                    case TransactionType.StableToUnstable:
                        OnMessage(new ImportantNotice(set, $"Trading {lastUpdate.StableAmount * set.Message2.TransactionAmount:F2} {lastUpdate.StableSymbol} for {lastUpdate.UnstableSymbol} on BSC"));
                        OnMessage(new TradeInitiating(set, lastUpdate.StableId, lastUpdate.UnstableId,
                                                      lastUpdate.StableAmount * set.Message2.TransactionAmount,
                                                      TradingPlatform.PancakeSwap, lastUpdate.StableDecimals,
                                                      lastUpdate.WalletAddress,
                                                      lastUpdate.Liquidity));
                        break;
                    case TransactionType.UnstableToStable:
                        OnMessage(new ImportantNotice(set, $"Trading {lastUpdate.UnstableAmount * set.Message2.TransactionAmount:F2} {lastUpdate.UnstableSymbol} for {lastUpdate.StableSymbol} on BSC"));
                        OnMessage(new TradeInitiating(set, lastUpdate.UnstableId, lastUpdate.StableId,
                                                      lastUpdate.UnstableAmount * set.Message2.TransactionAmount,
                                                      TradingPlatform.PancakeSwap, lastUpdate.UnstableDecimals,
                                                      lastUpdate.WalletAddress,
                                                      lastUpdate.Liquidity));
                        break;
                    case TransactionType.BridgeStable:
                        DataUpdate targetUpdate = set.Message1.Updates.First(u => u.BlockchainName == BlockchainName.Avalanche);
                        OnMessage(new ImportantNotice(set, $"Bridging {lastUpdate.StableAmount * set.Message2.TransactionAmount} {lastUpdate.StableSymbol} to Avalanche"));
                        OnMessage(new TokenBridging(set, BlockchainName.Bsc,
                                                    lastUpdate.StableAmount * set.Message2.TransactionAmount,
                                                    targetUpdate.StableAmount, 
                                                    lastUpdate.WalletAddress, lastUpdate.StableDecimals,
                                                    GetBridgeSourceToken(TokenType.Stable, BlockchainName.Bsc)));
                        break;
                    case TransactionType.BridgeUnstable:
                        targetUpdate = set.Message1.Updates.First(u => u.BlockchainName == BlockchainName.Avalanche);
                        OnMessage(new ImportantNotice(set, $"Bridging {lastUpdate.UnstableAmount * set.Message2.TransactionAmount} {lastUpdate.UnstableSymbol} to Avalanche"));
                        OnMessage(new TokenBridging(set, BlockchainName.Bsc,
                                                    lastUpdate.UnstableAmount * set.Message2.TransactionAmount,
                                                    targetUpdate.UnstableAmount, 
                                                    lastUpdate.WalletAddress, lastUpdate.UnstableDecimals,
                                                    GetBridgeSourceToken(TokenType.Unstable, BlockchainName.Bsc)));
                        break;
                    case TransactionType.StableToNative:
                        OnMessage(new ImportantNotice(set, $"Trading 10 {lastUpdate.StableSymbol} for BNB on BSC"));
                        OnMessage(new TradeInitiating(set, lastUpdate.StableId, 
                                                      ConfigurationManager.AppSettings["BscNativeCoinId"]
                                                     ?? throw new ConfigurationErrorsException("BscNativeCoinId not configured"),
                                                      Math.Min(10, lastUpdate.StableAmount),
                                                      TradingPlatform.PancakeSwap, lastUpdate.StableDecimals,
                                                      lastUpdate.WalletAddress,
                                                      lastUpdate.Liquidity));
                        break;
                    case TransactionType.UnstableToNative:
                        double unstableAmount = 10 / lastUpdate.UnstablePrice;
                        OnMessage(new ImportantNotice(set, $"Trading {unstableAmount} {lastUpdate.UnstableSymbol} for BNB on BSC"));
                        OnMessage(new TradeInitiating(set, lastUpdate.UnstableId, 
                                                      ConfigurationManager.AppSettings["BscNativeCoinId"]
                                                     ?? throw new ConfigurationErrorsException("BscNativeCoinId not configured"),
                                                      Math.Min(unstableAmount, lastUpdate.UnstableAmount),
                                                      TradingPlatform.PancakeSwap, lastUpdate.StableDecimals,
                                                      lastUpdate.WalletAddress,
                                                      lastUpdate.Liquidity));
                        break;
                    default:
                        throw new InvalidOperationException("Not Implemented.");
                }
                break;
            case BlockchainName.Avalanche:
                lastUpdate = set.Message1.Updates.First(u => u.BlockchainName == BlockchainName.Avalanche);
                switch (set.Message2.Type)
                {
                    case TransactionType.StableToUnstable:
                        OnMessage(new ImportantNotice(set, $"Trading {lastUpdate.StableAmount * set.Message2.TransactionAmount:F2} {lastUpdate.StableSymbol} for {lastUpdate.UnstableSymbol} on Avalanche"));
                        OnMessage(new TradeInitiating(set, lastUpdate.StableId, lastUpdate.UnstableId,
                                                      lastUpdate.StableAmount * set.Message2.TransactionAmount,
                                                      TradingPlatform.TraderJoe, lastUpdate.StableDecimals,
                                                      lastUpdate.WalletAddress,
                                                      lastUpdate.Liquidity));
                        break;
                    case TransactionType.UnstableToStable:
                        OnMessage(new ImportantNotice(set, $"Trading {lastUpdate.UnstableAmount * set.Message2.TransactionAmount:F2} {lastUpdate.UnstableSymbol} for {lastUpdate.StableSymbol} on Avalanche"));
                        OnMessage(new TradeInitiating(set, lastUpdate.UnstableId, lastUpdate.StableId,
                                                      lastUpdate.UnstableAmount * set.Message2.TransactionAmount,
                                                      TradingPlatform.TraderJoe, lastUpdate.UnstableDecimals,
                                                      lastUpdate.WalletAddress,
                                                      lastUpdate.Liquidity));
                        break;
                    case TransactionType.BridgeStable:
                        DataUpdate targetUpdate = set.Message1.Updates.First(u => u.BlockchainName == BlockchainName.Bsc);
                        OnMessage(new ImportantNotice(set, $"Bridging {lastUpdate.StableAmount * set.Message2.TransactionAmount} {lastUpdate.StableSymbol} to BSC"));
                        OnMessage(new TokenBridging(set, BlockchainName.Avalanche,
                                                    lastUpdate.StableAmount * set.Message2.TransactionAmount,
                                                    targetUpdate.StableAmount, 
                                                    lastUpdate.WalletAddress, lastUpdate.StableDecimals,
                                                    GetBridgeSourceToken(TokenType.Stable, BlockchainName.Avalanche)));
                        break;
                    case TransactionType.BridgeUnstable:
                        targetUpdate = set.Message1.Updates.First(u => u.BlockchainName == BlockchainName.Bsc);
                        OnMessage(new ImportantNotice(set, $"Bridging {lastUpdate.UnstableAmount * set.Message2.TransactionAmount} {lastUpdate.UnstableSymbol} to BSC"));
                        OnMessage(new TokenBridging(set, BlockchainName.Avalanche,
                                                    lastUpdate.UnstableAmount * set.Message2.TransactionAmount,
                                                    targetUpdate.UnstableAmount, 
                                                    lastUpdate.WalletAddress, lastUpdate.UnstableDecimals,
                                                    GetBridgeSourceToken(TokenType.Unstable, BlockchainName.Avalanche)));
                        break;
                    case TransactionType.StableToNative:
                        OnMessage(new ImportantNotice(set, $"Trading 10 {lastUpdate.StableSymbol} for AVAX on Avalanche"));
                        OnMessage(new TradeInitiating(set, lastUpdate.StableId, 
                                                      ConfigurationManager.AppSettings["AvalancheNativeCoinId"]
                                                      ?? throw new ConfigurationErrorsException("AvalancheNativeCoinId not configured"),
                                                      Math.Min(10, lastUpdate.StableAmount),
                                                      TradingPlatform.TraderJoe, lastUpdate.StableDecimals,
                                                      lastUpdate.WalletAddress,
                                                      lastUpdate.Liquidity));
                        break;
                    case TransactionType.UnstableToNative:
                        double unstableAmount = 10 / lastUpdate.UnstablePrice;
                        OnMessage(new ImportantNotice(set, $"Trading {unstableAmount} {lastUpdate.UnstableSymbol} for AVAX on Avalanche"));
                        OnMessage(new TradeInitiating(set, lastUpdate.UnstableId, 
                                                      ConfigurationManager.AppSettings["AvalancheNativeCoinId"]
                                                      ?? throw new ConfigurationErrorsException("AvalancheNativeCoinId not configured"),
                                                      Math.Min(unstableAmount, lastUpdate.UnstableAmount),
                                                      TradingPlatform.TraderJoe, lastUpdate.StableDecimals,
                                                      lastUpdate.WalletAddress,
                                                      lastUpdate.Liquidity));
                        break;
                    default:
                        throw new InvalidOperationException("Not Implemented.");
                }
                break;
            default:
                throw new InvalidOperationException("Not Implemented.");
        }
    }

    private static string GetBridgeSourceToken(TokenType tokenType, BlockchainName sourceChain)
    {
        return tokenType switch
        {
            TokenType.Stable => sourceChain switch
            {
                BlockchainName.Bsc => ConfigurationManager.AppSettings["BscStableCoinId"] ??
                                      throw new ConfigurationErrorsException("BscStableCoinId not found"),
                BlockchainName.Avalanche => ConfigurationManager.AppSettings["AvalancheStableCoinId"] ??
                                            throw new ConfigurationErrorsException("AvalancheStableCoinId not found"),
                _ => throw new ArgumentOutOfRangeException(nameof(sourceChain), sourceChain, null)
            },
            TokenType.Unstable => sourceChain switch
            {
                BlockchainName.Bsc => ConfigurationManager.AppSettings["BscUnstableCoinId"] ??
                                      throw new ConfigurationErrorsException("BscUnstableCoinId not found"),
                BlockchainName.Avalanche => ConfigurationManager.AppSettings["AvalancheUnstableCoinId"] ??
                                            throw new ConfigurationErrorsException("AvalancheUnstableCoinId not found"),
                _ => throw new ArgumentOutOfRangeException(nameof(sourceChain), sourceChain, null)
            },
            TokenType.Native => throw new InvalidOperationException("Cannot bridge native Token."),
            _ => throw new ArgumentOutOfRangeException(nameof(tokenType), tokenType, null)
        };
    }

    public record AwaitingBridgeAmount(BlockchainName TargetChain, double OriginalAmount, double SendAmount, Message OriginalMessage);
    private AwaitingBridgeAmount? awaitingBridgeAmount;

    protected override async void ExecuteCore(Message messageData)
    {
        if(messageData.TryGet(out TradeCompleted tradeCompleted))
        {
            ongoingTransaction = 0;
            OnMessage(new TransactionFinished(messageData, tradeCompleted.Success 
                                                               ? TransactionResult.Success 
                                                               : TransactionResult.Failed));
            return;
        }

        if(messageData.TryGet(out TokenBridged tokenBridged))
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

        if(messageData.TryGet(out DataUpdated dataUpdated) && 
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