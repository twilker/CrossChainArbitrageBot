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
[Consumes((typeof(DataUpdated)))]
public class SimulationBlockchainExecuter : Agent
{
    private readonly int bscTradeDuration;
    private readonly double bscSimpleTradeGasCosts;
    private readonly double bscLongTradeGasCosts;
    private readonly int avalancheTradeDuration;
    private readonly double avalancheSimpleTradeGasCosts;
    private readonly double avalancheLongTradeGasCosts;
    private readonly int celerBridgeDuration;
    private readonly double celerBridgeCosts;
    private readonly double celerBridgeBscGasCosts;
    private readonly double celerBridgeAvalancheGasCosts;
    private readonly string pancakeSwapRouterAddress;
    private readonly string traderJoeRouterAddress;
    private readonly string bscCelerBridgeAddress;
    private readonly string avalancheCelerBridgeAddress;

    private DataUpdated? latestUpdate;

    public SimulationBlockchainExecuter(IMessageBoard messageBoard) : base(messageBoard)
    {
        pancakeSwapRouterAddress = ConfigurationManager.AppSettings["PancakeswapRouterAddress"] ?? throw new ConfigurationErrorsException("PancakeswapRouterAddress not configured.");
        traderJoeRouterAddress = ConfigurationManager.AppSettings["TraderJoeRouterAddress"] ?? throw new ConfigurationErrorsException("TraderJoeRouterAddress not configured.");
        bscCelerBridgeAddress = ConfigurationManager.AppSettings["BscCelerBridgeAddress"] ?? throw new ConfigurationErrorsException("BscCelerBridgeAddress not configured.");
        avalancheCelerBridgeAddress = ConfigurationManager.AppSettings["AvalancheCelerBridgeAddress"] ?? throw new ConfigurationErrorsException("AvalancheCelerBridgeAddress not configured.");

        NameValueCollection simulationConfiguration = ConfigurationManager.GetSection("SimulationConfiguration") as NameValueCollection ?? throw new ConfigurationErrorsException("SimulationConfiguration not found.");
        bscTradeDuration = int.Parse(simulationConfiguration["BscTradeDuration"] ?? throw new ConfigurationErrorsException("BscTradeDuration not found."));
        bscSimpleTradeGasCosts = double.Parse(simulationConfiguration["BscSimpleTradeGasCosts"] ?? throw new ConfigurationErrorsException("BscSimpleTradeGasCosts not found."));
        bscLongTradeGasCosts = double.Parse(simulationConfiguration["BscLongTradeGasCosts"] ?? throw new ConfigurationErrorsException("BscLongTradeGasCosts not found."));
        avalancheTradeDuration = int.Parse(simulationConfiguration["AvalancheTradeDuration"] ?? throw new ConfigurationErrorsException("AvalancheTradeDuration not found."));
        avalancheSimpleTradeGasCosts = double.Parse(simulationConfiguration["AvalancheSimpleTradeGasCosts"] ?? throw new ConfigurationErrorsException("AvalancheSimpleTradeGasCosts not found."));
        avalancheLongTradeGasCosts = double.Parse(simulationConfiguration["AvalancheLongTradeGasCosts"] ?? throw new ConfigurationErrorsException("AvalancheLongTradeGasCosts not found."));
        celerBridgeDuration = int.Parse(simulationConfiguration["CelerBridgeDuration"] ?? throw new ConfigurationErrorsException("CelerBridgeDuration not found."));
        celerBridgeCosts = double.Parse(simulationConfiguration["CelerBridgeCosts"] ?? throw new ConfigurationErrorsException("CelerBridgeCosts not found."));
        celerBridgeBscGasCosts = double.Parse(simulationConfiguration["CelerBridgeBscGasCosts"] ?? throw new ConfigurationErrorsException("CelerBridgeBscGasCosts not found."));
        celerBridgeAvalancheGasCosts = double.Parse(simulationConfiguration["CelerBridgeAvalancheGasCosts"] ?? throw new ConfigurationErrorsException("CelerBridgeAvalancheGasCosts not found."));
    }

    private void TradeTokenToToken(TransactionExecuting executing)
    {
        if (latestUpdate == null)
        {
            OnMessage(new TransactionExecuted(executing, false));
            return;
        }
        
        Thread.Sleep(executing.BlockchainName switch
        {
            BlockchainName.Bsc => bscTradeDuration,
            BlockchainName.Avalanche => avalancheTradeDuration,
            _ => throw new ArgumentOutOfRangeException()
        });

        DataUpdate data = latestUpdate.Updates.First(d => d.BlockchainName == executing.BlockchainName);
        object[] parameters = executing.Parameters;
        string[] path = (string[])parameters[2];
        bool isStable = path.First().Equals(data.BlockchainName switch
        {
            BlockchainName.Bsc => ConfigurationManager.AppSettings["BscStableCoinId"],
            BlockchainName.Avalanche => ConfigurationManager.AppSettings["AvalancheStableCoinId"],
            _ => throw new ArgumentOutOfRangeException()
        }, StringComparison.OrdinalIgnoreCase);
        double gasCosts = data.BlockchainName switch
        {
            BlockchainName.Bsc => path.Length == 2 ? bscSimpleTradeGasCosts : bscLongTradeGasCosts,
            BlockchainName.Avalanche => path.Length == 2 ? avalancheSimpleTradeGasCosts : avalancheLongTradeGasCosts,
            _ => throw new ArgumentOutOfRangeException()
        };
        WalletBalanceUpdate nativeUpdate = new(data.BlockchainName,
                                               TokenType.Native,
                                               data.AccountBalance - gasCosts / data.NativePrice);
        WalletBalanceUpdate stableUpdate;
        WalletBalanceUpdate unstableUpdate;
        LiquidityOffset liquidityOffset;
        double amount;
        double received;
        if (isStable)
        {
            amount = (double)Web3.Convert.FromWei((BigInteger)parameters[0], data.StableDecimals);
            received = data.Liquidity.TokenAmount- data.Liquidity.TokenAmount * data.Liquidity.UsdPaired /
                                    (data.Liquidity.UsdPaired + amount);
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
                                 (data.Liquidity.TokenAmount + amount);
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
            OnMessage(new ImportantNotice(executing, $"Transaction failed because at least one balance would be negative: Stable - {stableUpdate}; Unstable - {unstableUpdate}; Native - {nativeUpdate}"));
            OnMessage(new TransactionExecuted(executing, false));
            return;
        }
        
        OnMessage(new ImportantNotice(
                      executing,
                      $"Simulated trade completed. {amount} {(isStable ? data.StableSymbol : data.UnstableSymbol)} -> {received} {(!isStable ? data.StableSymbol : data.UnstableSymbol)}"));
        OnMessage(new WalletBalanceUpdated(executing, nativeUpdate, stableUpdate, unstableUpdate));
        OnMessage(new LiquidityOffsetUpdated(executing, liquidityOffset, data.BlockchainName));
        OnMessage(new TransactionExecuted(executing, true));
    }

    private void TradeTokenToNative(TransactionExecuting executing)
    {
        if (latestUpdate == null)
        {
            OnMessage(new TransactionExecuted(executing, false));
            return;
        }
        
        Thread.Sleep(executing.BlockchainName switch
        {
            BlockchainName.Bsc => bscTradeDuration,
            BlockchainName.Avalanche => avalancheTradeDuration,
            _ => throw new ArgumentOutOfRangeException()
        });

        DataUpdate data = latestUpdate.Updates.First(d => d.BlockchainName == executing.BlockchainName);
        object[] parameters = executing.Parameters;
        string[] path = (string[])parameters[2];
        bool isStable = path.First().Equals(data.BlockchainName switch
        {
            BlockchainName.Bsc => ConfigurationManager.AppSettings["BscStableCoinId"],
            BlockchainName.Avalanche => ConfigurationManager.AppSettings["AvalancheStableCoinId"],
            _ => throw new ArgumentOutOfRangeException()
        }, StringComparison.OrdinalIgnoreCase);
        double gasCosts = data.BlockchainName switch
        {
            BlockchainName.Bsc => path.Length == 2 ? bscSimpleTradeGasCosts : bscLongTradeGasCosts,
            BlockchainName.Avalanche => path.Length == 2 ? avalancheSimpleTradeGasCosts : avalancheLongTradeGasCosts,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        WalletBalanceUpdate nativeUpdate;
        WalletBalanceUpdate tokenUpdate;
        double amount;
        double received;
        if (isStable)
        {
            amount = (double)Web3.Convert.FromWei((BigInteger)parameters[0], data.StableDecimals);
            received = amount / data.NativePrice;
            tokenUpdate = new WalletBalanceUpdate(data.BlockchainName,
                                                  TokenType.Stable,
                                                  data.StableAmount - amount);
            nativeUpdate = new WalletBalanceUpdate(data.BlockchainName,
                                                   TokenType.Native,
                                                   data.AccountBalance + received - gasCosts / data.NativePrice);
        }
        else
        {
            amount = (double)Web3.Convert.FromWei((BigInteger)parameters[0], data.UnstableDecimals);
            received = amount * data.UnstablePrice / data.NativePrice;
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
            OnMessage(new ImportantNotice(executing, $"Transaction failed because at least one balance would be negative: Token - {tokenUpdate}; Native - {nativeUpdate}"));
            OnMessage(new TransactionExecuted(executing, false));
            return;
        }
        
        OnMessage(new ImportantNotice(
                      executing,
                      $"Simulated trade completed. {amount} {(isStable ? data.StableSymbol : data.UnstableSymbol)} -> {received} {(data.BlockchainName == BlockchainName.Bsc ? "BNB" : "AVAX")}"));
        OnMessage(new WalletBalanceUpdated(executing, nativeUpdate, tokenUpdate));
        OnMessage(new TransactionExecuted(executing, true));
    }

    private void BridgeToken(TransactionExecuting executing)
    {
        if (latestUpdate == null)
        {
            OnMessage(new TransactionExecuted(executing, false));
            return;
        }
        
        DataUpdate data = latestUpdate.Updates.First(d => d.BlockchainName == executing.BlockchainName);
        DataUpdate targetData = latestUpdate.Updates.First(d => d.BlockchainName != executing.BlockchainName);
        object[] parameters = executing.Parameters;
        bool isStable = ((string)parameters[1]).Equals(data.BlockchainName switch
        {
            BlockchainName.Bsc => ConfigurationManager.AppSettings["BscStableCoinId"],
            BlockchainName.Avalanche => ConfigurationManager.AppSettings["AvalancheStableCoinId"],
            _ => throw new ArgumentOutOfRangeException()
        }, StringComparison.OrdinalIgnoreCase);
        double gasCosts = data.BlockchainName switch
        {
            BlockchainName.Bsc => celerBridgeBscGasCosts,
            BlockchainName.Avalanche => celerBridgeAvalancheGasCosts,
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
                                                   data.AccountBalance - gasCosts / data.NativePrice);
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
                                                   data.AccountBalance - gasCosts / data.NativePrice);
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
            OnMessage(new ImportantNotice(executing, $"Transaction failed because at least one balance would be negative: Source Token - {sourceTokenUpdate}; Native - {nativeUpdate}; Target Token - {targetTokenUpdate}"));
            OnMessage(new TransactionExecuted(executing, false));
            return;
        }

        OnMessage(new ImportantNotice(executing, $"Bridging token. ETA: {DateTime.Now+new TimeSpan(0,0,0,0,celerBridgeDuration):HH:mm:ss}"));
        OnMessage(new WalletBalanceUpdated(executing, nativeUpdate, sourceTokenUpdate));
        Thread.Sleep(executing.BlockchainName switch
        {
            BlockchainName.Bsc => celerBridgeDuration,
            BlockchainName.Avalanche => celerBridgeDuration,
            _ => throw new ArgumentOutOfRangeException()
        });

        OnMessage(new ImportantNotice(
                      executing,
                      $"Simulated bridge completed. {amount} {(isStable ? data.StableSymbol : data.UnstableSymbol)} -> {targetData.BlockchainName}"));
        OnMessage(new WalletBalanceUpdated(executing, targetTokenUpdate));
        OnMessage(new TransactionExecuted(executing, true));
    }

    protected override void ExecuteCore(Message messageData)
    {
        if (messageData.TryGet(out DataUpdated updated))
        {
            latestUpdate = updated;
            return;
        }

        TransactionExecuting executing = messageData.Get<TransactionExecuting>();
        if (executing.ContractAddress.Equals(pancakeSwapRouterAddress, StringComparison.OrdinalIgnoreCase) ||
            executing.ContractAddress.Equals(traderJoeRouterAddress, StringComparison.OrdinalIgnoreCase))
        {
            switch (executing.FunctionName)
            {
                case "swapExactTokensForTokens":
                    TradeTokenToToken(executing);
                    break;
                case "swapExactTokensForETH":
                    TradeTokenToNative(executing);
                    break;
                default:
                    throw new InvalidOperationException("Not implemented.");
            }
        }
        else if ((executing.ContractAddress.Equals(bscCelerBridgeAddress, StringComparison.OrdinalIgnoreCase) ||
                 executing.ContractAddress.Equals(avalancheCelerBridgeAddress, StringComparison.OrdinalIgnoreCase)) &&
                 executing.FunctionName == "send")
        {
            BridgeToken(executing);
        }
        else
        {
            throw new InvalidOperationException("Not implemented.");
        }
    }
}