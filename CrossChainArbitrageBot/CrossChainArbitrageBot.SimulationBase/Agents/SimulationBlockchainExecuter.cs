using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Numerics;
using System.Threading;
using Agents.Net;
using CrossChainArbitrageBot.Base.Messages;
using CrossChainArbitrageBot.Base.Models;
using CrossChainArbitrageBot.SimulationBase.Messages;
using CrossChainArbitrageBot.SimulationBase.Model;
using Nethereum.Web3;

namespace CrossChainArbitrageBot.SimulationBase.Agents;

[Consumes((typeof(TransactionExecuting)))]
[Consumes((typeof(GasEstimated)))]
[Consumes((typeof(DataUpdated)))]
[Consumes((typeof(TransactionsUntilErrorChanged)))]
public class SimulationBlockchainExecuter : Agent
{
    private readonly int bscTradeDuration;
    private readonly int avalancheTradeDuration;
    private readonly int celerBridgeDuration;
    private readonly string pancakeSwapRouterAddress;
    private readonly string traderJoeRouterAddress;
    private readonly string bscCelerBridgeAddress;
    private readonly string avalancheCelerBridgeAddress;
    private readonly double liquidityProviderFee;

    private DataUpdated? latestUpdate;
    private int transactionsUntilError = 0;

    private readonly MessageCollector<TransactionExecuting, GasEstimated> collector;

    public SimulationBlockchainExecuter(IMessageBoard messageBoard) : base(messageBoard)
    {
        pancakeSwapRouterAddress = ConfigurationManager.AppSettings["PancakeswapRouterAddress"] ?? throw new ConfigurationErrorsException("PancakeswapRouterAddress not configured.");
        traderJoeRouterAddress = ConfigurationManager.AppSettings["TraderJoeRouterAddress"] ?? throw new ConfigurationErrorsException("TraderJoeRouterAddress not configured.");
        bscCelerBridgeAddress = ConfigurationManager.AppSettings["BscCelerBridgeAddress"] ?? throw new ConfigurationErrorsException("BscCelerBridgeAddress not configured.");
        avalancheCelerBridgeAddress = ConfigurationManager.AppSettings["AvalancheCelerBridgeAddress"] ?? throw new ConfigurationErrorsException("AvalancheCelerBridgeAddress not configured.");
        liquidityProviderFee = double.Parse(ConfigurationManager.AppSettings["LiquidityProviderFee"] ?? throw new ConfigurationErrorsException("LiquidityProviderFee not found."));

        NameValueCollection simulationConfiguration = ConfigurationManager.GetSection("SimulationConfiguration") as NameValueCollection ?? throw new ConfigurationErrorsException("SimulationConfiguration not found.");
        bscTradeDuration = int.Parse(simulationConfiguration["BscTradeDuration"] ?? throw new ConfigurationErrorsException("BscTradeDuration not found."));
        avalancheTradeDuration = int.Parse(simulationConfiguration["AvalancheTradeDuration"] ?? throw new ConfigurationErrorsException("AvalancheTradeDuration not found."));
        celerBridgeDuration = int.Parse(simulationConfiguration["CelerBridgeDuration"] ?? throw new ConfigurationErrorsException("CelerBridgeDuration not found."));

        collector = new MessageCollector<TransactionExecuting, GasEstimated>(OnMessagesCollected);
    }

    private void OnMessagesCollected(MessageCollection<TransactionExecuting, GasEstimated> set)
    {
        set.MarkAsConsumed(set.Message1);
        HandleTransaction(set);
    }

    private void TradeTokenToToken(MessageCollection<TransactionExecuting, GasEstimated> set)
    {
        if (latestUpdate == null)
        {
            OnMessage(new TransactionExecuted(set, false));
            return;
        }
        
        Thread.Sleep(set.Message1.BlockchainName switch
        {
            BlockchainName.Bsc => bscTradeDuration,
            BlockchainName.Avalanche => avalancheTradeDuration,
            _ => throw new ArgumentOutOfRangeException()
        });

        DataUpdate data = latestUpdate.Updates.First(d => d.BlockchainName == set.Message1.BlockchainName);
        object[] parameters = set.Message1.Parameters;
        string[] path = (string[])parameters[2];
        bool isStable = path.First().Equals(data.BlockchainName switch
        {
            BlockchainName.Bsc => ConfigurationManager.AppSettings["BscStableCoinId"],
            BlockchainName.Avalanche => ConfigurationManager.AppSettings["AvalancheStableCoinId"],
            _ => throw new ArgumentOutOfRangeException()
        }, StringComparison.OrdinalIgnoreCase);
        double gasCosts = data.BlockchainName switch
        {
            BlockchainName.Bsc => set.Message2.GasEstimation.BnbTradeAmount,
            BlockchainName.Avalanche => set.Message2.GasEstimation.AvaxTradeAmount,
            _ => throw new ArgumentOutOfRangeException()
        };
        WalletBalanceUpdate nativeUpdate = new(data.BlockchainName,
                                               TokenType.Native,
                                               data.AccountBalance - gasCosts);
        WalletBalanceUpdate stableUpdate;
        WalletBalanceUpdate unstableUpdate;
        LiquidityOffset liquidityOffset;
        double amount;
        double received;
        if (isStable)
        {
            amount = (double)Web3.Convert.FromWei((BigInteger)parameters[0], data.StableDecimals);
            received = data.Liquidity.TokenAmount- data.Liquidity.TokenAmount * data.Liquidity.UsdPaired /
                                    (data.Liquidity.UsdPaired + amount*(1-liquidityProviderFee));
            stableUpdate = new(data.BlockchainName,
                               TokenType.Stable,
                               data.StableAmount - amount);
            unstableUpdate = new WalletBalanceUpdate(data.BlockchainName,
                                                     TokenType.Unstable,
                                                     data.UnstableAmount + received);
            liquidityOffset = new LiquidityOffset(received * -1, amount);
        }
        else
        {
            amount = (double)Web3.Convert.FromWei((BigInteger)parameters[0], data.UnstableDecimals);
            received = data.Liquidity.UsdPaired - data.Liquidity.TokenAmount * data.Liquidity.UsdPaired /
                                 (data.Liquidity.TokenAmount + amount*(1-liquidityProviderFee));
            unstableUpdate = new(data.BlockchainName,
                               TokenType.Unstable,
                               data.UnstableAmount - amount);
            stableUpdate = new(data.BlockchainName,
                               TokenType.Stable,
                               data.StableAmount + received);
            liquidityOffset = new LiquidityOffset(amount, received * -1);
        }

        if (nativeUpdate.NewBalance < 0 ||
            stableUpdate.NewBalance < 0 ||
            unstableUpdate.NewBalance < 0)
        {
            OnMessage(new ImportantNotice(set, $"Transaction failed because at least one balance would be negative: Stable - {stableUpdate}; Unstable - {unstableUpdate}; Native - {nativeUpdate}"));
            OnMessage(new TransactionExecuted(set, false));
            return;
        }
        
        OnMessage(new ImportantNotice(
                      set.Message1,
                      $"Simulated trade completed. {amount} {(isStable ? data.StableSymbol : data.UnstableSymbol)} -> {received} {(!isStable ? data.StableSymbol : data.UnstableSymbol)}"));
        OnMessage(new WalletBalanceUpdated(set, nativeUpdate, stableUpdate, unstableUpdate));
        OnMessage(new LiquidityOffsetUpdated(set, liquidityOffset, data.BlockchainName));
        OnMessage(new TransactionExecuted(set, true));
    }

    private void TradeTokenToNative(MessageCollection<TransactionExecuting, GasEstimated> set)
    {
        if (latestUpdate == null)
        {
            OnMessage(new TransactionExecuted(set, false));
            return;
        }
        
        Thread.Sleep(set.Message1.BlockchainName switch
        {
            BlockchainName.Bsc => bscTradeDuration,
            BlockchainName.Avalanche => avalancheTradeDuration,
            _ => throw new ArgumentOutOfRangeException()
        });

        DataUpdate data = latestUpdate.Updates.First(d => d.BlockchainName == set.Message1.BlockchainName);
        object[] parameters = set.Message1.Parameters;
        string[] path = (string[])parameters[2];
        bool isStable = path.First().Equals(data.BlockchainName switch
        {
            BlockchainName.Bsc => ConfigurationManager.AppSettings["BscStableCoinId"],
            BlockchainName.Avalanche => ConfigurationManager.AppSettings["AvalancheStableCoinId"],
            _ => throw new ArgumentOutOfRangeException()
        }, StringComparison.OrdinalIgnoreCase);
        double gasCosts = data.BlockchainName switch
        {
            BlockchainName.Bsc => set.Message2.GasEstimation.BnbTradeAmount,
            BlockchainName.Avalanche => set.Message2.GasEstimation.AvaxTradeAmount,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        WalletBalanceUpdate nativeUpdate;
        WalletBalanceUpdate tokenUpdate;
        double amount;
        double received;
        if (isStable)
        {
            amount = (double)Web3.Convert.FromWei((BigInteger)parameters[0], data.StableDecimals);
            received = amount*(1-liquidityProviderFee) / data.NativePrice;
            tokenUpdate = new WalletBalanceUpdate(data.BlockchainName,
                                                  TokenType.Stable,
                                                  data.StableAmount - amount);
            nativeUpdate = new WalletBalanceUpdate(data.BlockchainName,
                                                   TokenType.Native,
                                                   data.AccountBalance + received - gasCosts);
        }
        else
        {
            amount = (double)Web3.Convert.FromWei((BigInteger)parameters[0], data.UnstableDecimals);
            received = amount*(1-liquidityProviderFee) * data.UnstablePrice / data.NativePrice;
            tokenUpdate = new WalletBalanceUpdate(data.BlockchainName,
                                                  TokenType.Unstable,
                                                  data.UnstableAmount - amount);
            nativeUpdate = new WalletBalanceUpdate(data.BlockchainName,
                                                   TokenType.Native,
                                                   data.AccountBalance + received - gasCosts / data.NativePrice);
        }

        if (nativeUpdate.NewBalance < 0 ||
            tokenUpdate.NewBalance < 0)
        {
            OnMessage(new ImportantNotice(set, $"Transaction failed because at least one balance would be negative: Token - {tokenUpdate}; Native - {nativeUpdate}"));
            OnMessage(new TransactionExecuted(set, false));
            return;
        }
        
        OnMessage(new ImportantNotice(
                      set,
                      $"Simulated trade completed. {amount} {(isStable ? data.StableSymbol : data.UnstableSymbol)} -> {received} {(data.BlockchainName == BlockchainName.Bsc ? "BNB" : "AVAX")}"));
        OnMessage(new WalletBalanceUpdated(set, nativeUpdate, tokenUpdate));
        OnMessage(new TransactionExecuted(set, true));
    }

    private void BridgeToken(MessageCollection<TransactionExecuting, GasEstimated> set)
    {
        if (latestUpdate == null)
        {
            OnMessage(new TransactionExecuted(set, false));
            return;
        }
        
        DataUpdate data = latestUpdate.Updates.First(d => d.BlockchainName == set.Message1.BlockchainName);
        DataUpdate targetData = latestUpdate.Updates.First(d => d.BlockchainName != set.Message1.BlockchainName);
        object[] parameters = set.Message1.Parameters;
        bool isStable = ((string)parameters[1]).Equals(data.BlockchainName switch
        {
            BlockchainName.Bsc => ConfigurationManager.AppSettings["BscStableCoinId"],
            BlockchainName.Avalanche => ConfigurationManager.AppSettings["AvalancheStableCoinId"],
            _ => throw new ArgumentOutOfRangeException()
        }, StringComparison.OrdinalIgnoreCase);
        double gasCosts = data.BlockchainName switch
        {
            BlockchainName.Bsc => set.Message2.GasEstimation.BnbSingleBridgeAmount,
            BlockchainName.Avalanche => set.Message2.GasEstimation.AvaxSingleBridgeAmount,
            _ => throw new ArgumentOutOfRangeException()
        };
        double celerBridgeCosts = data.BlockchainName switch
        {
            BlockchainName.Bsc => set.Message2.GasEstimation.BscBridgeFee,
            BlockchainName.Avalanche => set.Message2.GasEstimation.AvalancheBridgeFee,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        WalletBalanceUpdate nativeUpdate;
        WalletBalanceUpdate sourceTokenUpdate;
        WalletBalanceUpdate targetTokenUpdate;
        double amount;
        if (isStable)
        {
            nativeUpdate = new WalletBalanceUpdate(data.BlockchainName,
                                                   TokenType.Native,
                                                   data.AccountBalance - gasCosts);
            amount = (double)Web3.Convert.FromWei((BigInteger)parameters[2], data.StableDecimals);
            sourceTokenUpdate = new WalletBalanceUpdate(data.BlockchainName,
                                                        TokenType.Stable,
                                                        data.StableAmount - amount);
            targetTokenUpdate = new WalletBalanceUpdate(targetData.BlockchainName,
                                                        TokenType.Stable,
                                                        targetData.StableAmount + amount-celerBridgeCosts);
        }
        else
        {
            nativeUpdate = new WalletBalanceUpdate(data.BlockchainName,
                                                   TokenType.Native,
                                                   data.AccountBalance - gasCosts);
            amount = (double)Web3.Convert.FromWei((BigInteger)parameters[2], data.UnstableDecimals);
            sourceTokenUpdate = new WalletBalanceUpdate(data.BlockchainName,
                                                        TokenType.Unstable,
                                                        data.UnstableAmount - amount);
            targetTokenUpdate = new WalletBalanceUpdate(targetData.BlockchainName,
                                                        TokenType.Unstable,
                                                        targetData.UnstableAmount + amount -
                                                        celerBridgeCosts / targetData.UnstablePrice);
        }

        if (nativeUpdate.NewBalance < 0 ||
            sourceTokenUpdate.NewBalance < 0 ||
            targetTokenUpdate.NewBalance < 0)
        {
            OnMessage(new ImportantNotice(set, $"Transaction failed because at least one balance would be negative: Source Token - {sourceTokenUpdate}; Native - {nativeUpdate}; Target Token - {targetTokenUpdate}"));
            OnMessage(new TransactionExecuted(set, false));
            return;
        }

        OnMessage(new ImportantNotice(set, $"Bridging token. ETA: {DateTime.Now+new TimeSpan(0,0,0,0,celerBridgeDuration):HH:mm:ss}"));
        OnMessage(new WalletBalanceUpdated(set, nativeUpdate, sourceTokenUpdate));
        OnMessage(new TransactionExecuted(set, true));
        Thread.Sleep(set.Message1.BlockchainName switch
        {
            BlockchainName.Bsc => celerBridgeDuration,
            BlockchainName.Avalanche => celerBridgeDuration,
            _ => throw new ArgumentOutOfRangeException()
        });

        OnMessage(new ImportantNotice(
                      latestUpdate,
                      $"Simulated bridge completed. {amount} {(isStable ? data.StableSymbol : data.UnstableSymbol)} -> {targetData.BlockchainName}"));
        OnMessage(new WalletBalanceUpdated(latestUpdate, targetTokenUpdate));
    }

    protected override void ExecuteCore(Message messageData)
    {
        if (messageData.TryGet(out DataUpdated updated))
        {
            latestUpdate = updated;
            return;
        }
        if (messageData.TryGet(out TransactionsUntilErrorChanged untilErrorChanged))
        {
            transactionsUntilError = untilErrorChanged.TransactionsUntilError;
            return;
        }

        collector.Push(messageData);
    }

    private void HandleTransaction(MessageCollection<TransactionExecuting, GasEstimated> set)
    {
        int untilError = Interlocked.Decrement(ref transactionsUntilError);
        switch (untilError)
        {
            case < 0:
                transactionsUntilError++;
                break;
            case 0:
                OnMessage(new ImportantNotice(set, "Simulated transaction error."));
                OnMessage(new TransactionExecuted(set, false));
                OnMessage(new TransactionsUntilErrorChanged(set, 0));
                return;
        }

        if (set.Message1.ContractAddress.Equals(pancakeSwapRouterAddress, StringComparison.OrdinalIgnoreCase) ||
            set.Message1.ContractAddress.Equals(traderJoeRouterAddress, StringComparison.OrdinalIgnoreCase))
        {
            switch (set.Message1.FunctionName)
            {
                case "swapExactTokensForTokens":
                    TradeTokenToToken(set);
                    break;
                case "swapExactTokensForETH":
                    TradeTokenToNative(set);
                    break;
                default:
                    throw new InvalidOperationException("Not implemented.");
            }
        }
        else if ((set.Message1.ContractAddress.Equals(bscCelerBridgeAddress, StringComparison.OrdinalIgnoreCase) ||
                  set.Message1.ContractAddress.Equals(avalancheCelerBridgeAddress, StringComparison.OrdinalIgnoreCase)) &&
                 set.Message1.FunctionName == "send")
        {
            BridgeToken(set);
        }
        else
        {
            throw new InvalidOperationException("Not implemented.");
        }
    }
}