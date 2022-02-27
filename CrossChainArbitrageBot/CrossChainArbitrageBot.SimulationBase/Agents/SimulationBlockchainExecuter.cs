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
    private readonly MessageCollector<TransactionExecuting, DataUpdated> collector;

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

        collector = new MessageCollector<TransactionExecuting, DataUpdated>(OnMessagesCollected);
    }

    private void OnMessagesCollected(MessageCollection<TransactionExecuting, DataUpdated> set)
    {
        set.MarkAsConsumed(set.Message1);

        DataUpdate data = set.Message2.Updates.First(u => u.BlockchainName == set.Message1.BlockchainName);
        if (set.Message1.ContractAddress.Equals(pancakeSwapRouterAddress, StringComparison.OrdinalIgnoreCase) ||
            set.Message1.ContractAddress.Equals(traderJoeRouterAddress, StringComparison.OrdinalIgnoreCase))
        {
            switch (set.Message1.FunctionName)
            {
                case "swapExactTokensForTokens":
                    TradeTokenToToken(data, set.Message1.Parameters, set);
                    break;
                case "swapExactTokensForETH":
                    TradeTokenToNative(data, set.Message1.Parameters, set);
                    break;
                default:
                    throw new InvalidOperationException("Not implemented.");
            }
        }
        else
        {
            throw new InvalidOperationException("Not implemented.");
        }
    }

    private void TradeTokenToNative(DataUpdate data, object[] parameters, MessageCollection<TransactionExecuting, DataUpdated> set)
    {
        throw new NotImplementedException();
    }

    private void TradeTokenToToken(DataUpdate data, object[] parameters, MessageCollection<TransactionExecuting, DataUpdated> set)
    {
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
        if (isStable)
        {
            double amount = (double)Web3.Convert.FromWei((BigInteger)parameters[0], data.StableDecimals);
            double tokensReceived = data.Liquidity.TokenAmount- data.Liquidity.TokenAmount * data.Liquidity.UsdPaired /
                                    (data.Liquidity.UsdPaired + amount);
            stableUpdate = new(data.BlockchainName,
                               TokenType.Stable,
                               data.StableAmount - amount);
            unstableUpdate = new WalletBalanceUpdate(data.BlockchainName,
                                                     TokenType.Unstable,
                                                     data.UnstableAmount + tokensReceived);
        }
        else
        {
            throw new InvalidOperationException("Not implemented.");
        }
        Thread.Sleep(data.BlockchainName switch
        {
            BlockchainName.Bsc => bscTradeDuration,
            BlockchainName.Avalanche => avalancheTradeDuration,
            _ => throw new ArgumentOutOfRangeException()
        });
        //TODO Offset of liquidity
        //TODO check negative balances
        //TODO Important Notice
        //TODO Check latest data update after sleep (no collector)
        OnMessage(new WalletBalanceUpdated(set, nativeUpdate, stableUpdate, unstableUpdate));
        OnMessage(new TradeCompleted(set, true));
    }

    protected override void ExecuteCore(Message messageData)
    {
        collector.Push(messageData);
    }
}