using System.Configuration;
using Agents.Net;
using ConcurrentCollections;
using CrossChainArbitrageBot.Base.Messages;
using CrossChainArbitrageBot.Base.Models;

namespace CrossChainArbitrageBot.Base.Agents;

[Consumes(typeof(TransactionStarted))]
[Consumes(typeof(DataUpdated))]
[Consumes(typeof(TradeCompleted))]
[Consumes(typeof(TokenBridged))]
[Produces(typeof(TradeInitiating))]
[Produces(typeof(TransactionFinished))]
internal class TransactionGateway : Agent
{
    private readonly MessageCollector<DataUpdated, TransactionStarted> collector;
    private DataUpdated? latestUpdate;
    private readonly int timeout;

    public TransactionGateway(IMessageBoard messageBoard) : base(messageBoard)
    {
        collector = new MessageCollector<DataUpdated,TransactionStarted>(OnCollected);
        timeout = int.Parse(ConfigurationManager.AppSettings["TransactionTimeout"]
                            ?? throw new ConfigurationErrorsException("TransactionTimeout not configured."));
    }

    private void OnCollected(MessageCollection<DataUpdated, TransactionStarted> set)
    {
        set.MarkAsConsumed(set.Message2);

        if (set.Message2.Type == TransactionType.AutoLoop ||
            set.Message2.Type == TransactionType.SynchronizedTrade ||
            set.Message2.Type == TransactionType.SingleLoop)
        {
            OnMessage(new LoopStarted(set, set.Message2.Type switch
            {
                TransactionType.AutoLoop => LoopKind.Auto,
                TransactionType.SynchronizedTrade => LoopKind.SyncTrade,
                TransactionType.SingleLoop => LoopKind.Single,
                _ => throw new ArgumentOutOfRangeException()
            }));
            return;
        }

        switch (set.Message2.Chain)
        {
            case BlockchainName.Bsc:
                DataUpdate lastUpdate;
                HandleBscTransaction(set);
                break;
            case BlockchainName.Avalanche:
                lastUpdate = set.Message1.Updates.First(u => u.BlockchainName == BlockchainName.Avalanche);
                HandleAvalancheTransaction(set, lastUpdate);
                break;
            default:
                throw new InvalidOperationException("Not Implemented.");
        }
    }

    private void HandleAvalancheTransaction(MessageCollection<DataUpdated, TransactionStarted> set, DataUpdate lastUpdate)
    {
        switch (set.Message2.Type)
        {
            case TransactionType.StableToUnstable:
                OnMessage(new ImportantNotice(
                              set,
                              $"Trading {lastUpdate.StableAmount * set.Message2.TransactionAmount:F2} {lastUpdate.StableSymbol} for {lastUpdate.UnstableSymbol} on Avalanche"));
                OnMessage(new TradeInitiating(set, lastUpdate.StableId, lastUpdate.UnstableId,
                                              lastUpdate.StableAmount * set.Message2.TransactionAmount,
                                              TradingPlatform.TraderJoe, lastUpdate.StableDecimals,
                                              lastUpdate.WalletAddress,
                                              lastUpdate.Liquidity,
                                              lastUpdate.UnstableAmount,
                                              lastUpdate.StableAmount * set.Message2.TransactionAmount /
                                              lastUpdate.UnstablePrice,
                                              TokenType.Unstable));
                break;
            case TransactionType.UnstableToStable:
                OnMessage(new ImportantNotice(
                              set,
                              $"Trading {lastUpdate.UnstableAmount * set.Message2.TransactionAmount:F2} {lastUpdate.UnstableSymbol} for {lastUpdate.StableSymbol} on Avalanche"));
                OnMessage(new TradeInitiating(set, lastUpdate.UnstableId, lastUpdate.StableId,
                                              lastUpdate.UnstableAmount * set.Message2.TransactionAmount,
                                              TradingPlatform.TraderJoe, lastUpdate.UnstableDecimals,
                                              lastUpdate.WalletAddress,
                                              lastUpdate.Liquidity,
                                              lastUpdate.StableAmount,
                                              lastUpdate.UnstableAmount * set.Message2.TransactionAmount *
                                              lastUpdate.UnstablePrice,
                                              TokenType.Stable));
                break;
            case TransactionType.BridgeStable:
                OnMessage(new ImportantNotice(
                              set,
                              $"Bridging {lastUpdate.StableAmount * set.Message2.TransactionAmount} {lastUpdate.StableSymbol} to BSC"));
                OnMessage(new TokenBridging(set, BlockchainName.Avalanche,
                                            lastUpdate.StableAmount * set.Message2.TransactionAmount,
                                            lastUpdate.WalletAddress, lastUpdate.StableDecimals,
                                            GetBridgeSourceToken(TokenType.Stable, BlockchainName.Avalanche),
                                            TokenType.Stable));
                break;
            case TransactionType.BridgeUnstable:
                OnMessage(new ImportantNotice(
                              set,
                              $"Bridging {lastUpdate.UnstableAmount * set.Message2.TransactionAmount} {lastUpdate.UnstableSymbol} to BSC"));
                OnMessage(new TokenBridging(set, BlockchainName.Avalanche,
                                            lastUpdate.UnstableAmount * set.Message2.TransactionAmount,
                                            lastUpdate.WalletAddress, lastUpdate.UnstableDecimals,
                                            GetBridgeSourceToken(TokenType.Unstable, BlockchainName.Avalanche),
                                            TokenType.Unstable));
                break;
            case TransactionType.StableToNative:
                OnMessage(new ImportantNotice(set, $"Trading 10 {lastUpdate.StableSymbol} for AVAX on Avalanche"));
                OnMessage(new TradeInitiating(set, lastUpdate.StableId,
                                              ConfigurationManager.AppSettings["AvalancheNativeCoinId"]
                                              ?? throw new ConfigurationErrorsException(
                                                  "AvalancheNativeCoinId not configured"),
                                              Math.Min(10, lastUpdate.StableAmount),
                                              TradingPlatform.TraderJoe, lastUpdate.StableDecimals,
                                              lastUpdate.WalletAddress,
                                              lastUpdate.Liquidity,
                                              lastUpdate.AccountBalance,
                                              Math.Min(10, lastUpdate.StableAmount) /
                                              lastUpdate.NativePrice,
                                              TokenType.Native));
                break;
            case TransactionType.UnstableToNative:
                double unstableAmount = 10 / lastUpdate.UnstablePrice;
                OnMessage(new ImportantNotice(
                              set, $"Trading {unstableAmount} {lastUpdate.UnstableSymbol} for AVAX on Avalanche"));
                OnMessage(new TradeInitiating(set, lastUpdate.UnstableId,
                                              ConfigurationManager.AppSettings["AvalancheNativeCoinId"]
                                              ?? throw new ConfigurationErrorsException(
                                                  "AvalancheNativeCoinId not configured"),
                                              Math.Min(unstableAmount, lastUpdate.UnstableAmount),
                                              TradingPlatform.TraderJoe, lastUpdate.UnstableDecimals,
                                              lastUpdate.WalletAddress,
                                              lastUpdate.Liquidity,
                                              lastUpdate.AccountBalance,
                                              Math.Min(unstableAmount, lastUpdate.UnstableAmount)*lastUpdate.UnstablePrice /
                                              lastUpdate.NativePrice,
                                              TokenType.Native));
                break;
            default:
                throw new InvalidOperationException("Not Implemented.");
        }
    }

    private void HandleBscTransaction(MessageCollection<DataUpdated, TransactionStarted> set)
    {
        DataUpdate lastUpdate = set.Message1.Updates.First(u => u.BlockchainName == BlockchainName.Bsc);
        switch (set.Message2.Type)
        {
            case TransactionType.StableToUnstable:
                OnMessage(new ImportantNotice(
                              set,
                              $"Trading {lastUpdate.StableAmount * set.Message2.TransactionAmount:F2} {lastUpdate.StableSymbol} for {lastUpdate.UnstableSymbol} on BSC"));
                OnMessage(new TradeInitiating(set, lastUpdate.StableId, lastUpdate.UnstableId,
                                              lastUpdate.StableAmount * set.Message2.TransactionAmount,
                                              TradingPlatform.PancakeSwap, lastUpdate.StableDecimals,
                                              lastUpdate.WalletAddress,
                                              lastUpdate.Liquidity,
                                              lastUpdate.UnstableAmount,
                                              lastUpdate.StableAmount * set.Message2.TransactionAmount /
                                              lastUpdate.UnstablePrice,
                                              TokenType.Unstable));
                break;
            case TransactionType.UnstableToStable:
                OnMessage(new ImportantNotice(
                              set,
                              $"Trading {lastUpdate.UnstableAmount * set.Message2.TransactionAmount:F2} {lastUpdate.UnstableSymbol} for {lastUpdate.StableSymbol} on BSC"));
                OnMessage(new TradeInitiating(set, lastUpdate.UnstableId, lastUpdate.StableId,
                                              lastUpdate.UnstableAmount * set.Message2.TransactionAmount,
                                              TradingPlatform.PancakeSwap, lastUpdate.UnstableDecimals,
                                              lastUpdate.WalletAddress,
                                              lastUpdate.Liquidity,
                                              lastUpdate.StableAmount,
                                              lastUpdate.UnstableAmount * set.Message2.TransactionAmount *
                                              lastUpdate.UnstablePrice,
                                              TokenType.Stable));
                break;
            case TransactionType.BridgeStable:
                OnMessage(new ImportantNotice(
                              set,
                              $"Bridging {lastUpdate.StableAmount * set.Message2.TransactionAmount} {lastUpdate.StableSymbol} to Avalanche"));
                OnMessage(new TokenBridging(set, BlockchainName.Bsc,
                                            lastUpdate.StableAmount * set.Message2.TransactionAmount,
                                            lastUpdate.WalletAddress, lastUpdate.StableDecimals,
                                            GetBridgeSourceToken(TokenType.Stable, BlockchainName.Bsc),
                                            TokenType.Stable));
                break;
            case TransactionType.BridgeUnstable:
                OnMessage(new ImportantNotice(
                              set,
                              $"Bridging {lastUpdate.UnstableAmount * set.Message2.TransactionAmount} {lastUpdate.UnstableSymbol} to Avalanche"));
                OnMessage(new TokenBridging(set, BlockchainName.Bsc,
                                            lastUpdate.UnstableAmount * set.Message2.TransactionAmount,
                                            lastUpdate.WalletAddress, lastUpdate.UnstableDecimals,
                                            GetBridgeSourceToken(TokenType.Unstable, BlockchainName.Bsc),
                                            TokenType.Unstable));
                break;
            case TransactionType.StableToNative:
                OnMessage(new ImportantNotice(set, $"Trading 10 {lastUpdate.StableSymbol} for BNB on BSC"));
                OnMessage(new TradeInitiating(set, lastUpdate.StableId,
                                              ConfigurationManager.AppSettings["BscNativeCoinId"]
                                              ?? throw new ConfigurationErrorsException("BscNativeCoinId not configured"),
                                              Math.Min(10, lastUpdate.StableAmount),
                                              TradingPlatform.PancakeSwap, lastUpdate.StableDecimals,
                                              lastUpdate.WalletAddress,
                                              lastUpdate.Liquidity,
                                              lastUpdate.AccountBalance,
                                              Math.Min(10, lastUpdate.StableAmount) /
                                              lastUpdate.NativePrice,
                                              TokenType.Native));
                break;
            case TransactionType.UnstableToNative:
                double unstableAmount = 10 / lastUpdate.UnstablePrice;
                OnMessage(new ImportantNotice(set, $"Trading {unstableAmount} {lastUpdate.UnstableSymbol} for BNB on BSC"));
                OnMessage(new TradeInitiating(set, lastUpdate.UnstableId,
                                              ConfigurationManager.AppSettings["BscNativeCoinId"]
                                              ?? throw new ConfigurationErrorsException("BscNativeCoinId not configured"),
                                              Math.Min(unstableAmount, lastUpdate.UnstableAmount),
                                              TradingPlatform.PancakeSwap, lastUpdate.UnstableDecimals,
                                              lastUpdate.WalletAddress,
                                              lastUpdate.Liquidity,
                                              lastUpdate.AccountBalance,
                                              Math.Min(unstableAmount, lastUpdate.UnstableAmount)*lastUpdate.UnstablePrice /
                                              lastUpdate.NativePrice,
                                              TokenType.Native));
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

    private record AwaitingAmount(BlockchainName BlockchainName, double OriginalAmount,
                                                  double ExpectedAmount, Message CompletedMessage, TokenType TokenType)
    {
        private readonly int timeout = -1;

        public bool IsTimeoutReached { get; private set; } 

        public int Timeout
        {
            get => timeout;
            init
            {
                timeout = value;
                if (timeout > 0)
                {
                    Task.Factory.StartNew(WaitForTimeout);
                }
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)BlockchainName;
                hashCode = (hashCode * 397) ^ OriginalAmount.GetHashCode();
                hashCode = (hashCode * 397) ^ ExpectedAmount.GetHashCode();
                hashCode = (hashCode * 397) ^ CompletedMessage.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)TokenType;
                return hashCode;
            }
        }

        private void WaitForTimeout()
        {
            Thread.Sleep(timeout);
            IsTimeoutReached = true;
        }
    };

    private readonly ConcurrentHashSet<AwaitingAmount> awaitingAmounts = new();

    protected override void ExecuteCore(Message messageData)
    {
        if(messageData.TryGet(out TradeCompleted tradeCompleted))
        {
            ProcessTradeCompleted();
            return;
        }

        if(messageData.TryGet(out TokenBridged tokenBridged))
        {
            ProcessTokenBridged();
            return;
        }

        if(messageData.TryGet(out DataUpdated dataUpdated))
        {
            ProcessDataUpdate();
        }
        collector.Push(messageData);

        void ProcessDataUpdate()
        {
            latestUpdate = dataUpdated;
            foreach (AwaitingAmount awaitingAmount in awaitingAmounts)
            {
                DataUpdate update = dataUpdated.Updates.First(u => u.BlockchainName == awaitingAmount.BlockchainName);
                double amount = awaitingAmount.TokenType switch
                {
                    TokenType.Stable => update.StableAmount,
                    TokenType.Unstable => update.UnstableAmount,
                    TokenType.Native => update.AccountBalance,
                    _ => throw new ArgumentOutOfRangeException()
                };
                if (amount > awaitingAmount.OriginalAmount + awaitingAmount.ExpectedAmount * 0.5 &&
                    awaitingAmounts.TryRemove(awaitingAmount))
                {
                    OnMessage(new TransactionFinished(awaitingAmount.CompletedMessage, TransactionResult.Success));
                    return;
                }

                if (awaitingAmount.IsTimeoutReached && awaitingAmounts.TryRemove(awaitingAmount))
                {
                    OnMessage(new TransactionFinished(awaitingAmount.CompletedMessage, TransactionResult.Timeout));
                }
            }
        }

        void ProcessTokenBridged()
        {
            if (tokenBridged.Success)
            {
                double currentAmount = tokenBridged.TokenType == TokenType.Unstable
                                           ? latestUpdate!.Updates
                                                         .First(u => u.BlockchainName ==
                                                                     tokenBridged.TargetChain)
                                                         .UnstableAmount
                                           : latestUpdate!.Updates
                                                         .First(u => u.BlockchainName ==
                                                                     tokenBridged.TargetChain)
                                                         .StableAmount;
                                                   
                awaitingAmounts.Add(new AwaitingAmount(tokenBridged.TargetChain, currentAmount,
                                                       tokenBridged.AmountSend, tokenBridged, tokenBridged.TokenType)
                {
                    Timeout = timeout
                });
            }
            else
            {
                OnMessage(new TransactionFinished(messageData, TransactionResult.Failed));
            }
        }

        void ProcessTradeCompleted()
        {
            if (tradeCompleted.Success)
            {
                awaitingAmounts.Add(new AwaitingAmount(tradeCompleted.BlockchainName, tradeCompleted.OriginalAmount,
                                                       tradeCompleted.AmountExpected, tradeCompleted, tradeCompleted.TokenType)
                {
                    Timeout = timeout
                });
            }
            else
            {
                OnMessage(new TransactionFinished(messageData, TransactionResult.Failed));
            }
        }
    }
}