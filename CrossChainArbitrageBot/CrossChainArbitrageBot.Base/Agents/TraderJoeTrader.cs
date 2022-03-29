using System.Configuration;
using System.Numerics;
using Agents.Net;
using CrossChainArbitrageBot.Base.Messages;
using CrossChainArbitrageBot.Base.Models;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Newtonsoft.Json.Linq;

namespace CrossChainArbitrageBot.Base.Agents;

[Consumes(typeof(TradeInitiating))]
[Consumes(typeof(TransactionExecuted))]
[Consumes(typeof(EstimatingGasBasedOnTransactions))]
public class TraderJoeTrader : Agent
{
    public TraderJoeTrader(IMessageBoard messageBoard) : base(messageBoard)
    {
    }

    protected override void ExecuteCore(Message messageData)
    {
        if (messageData.TryGet(out EstimatingGasBasedOnTransactions estimatingGas))
        {
            TryEstimateGas(estimatingGas);
            return;
        }
        if (messageData.TryGet(out TradeInitiating trade) &&
            trade.Platform == TradingPlatform.TraderJoe)
        {
            if (trade.ToTokenId.Equals(ConfigurationManager.AppSettings["AvalancheNativeCoinId"], StringComparison.OrdinalIgnoreCase))
            {
                TradeTokenForNative(messageData, trade);
            }
            else
            {
                TradeTokenForToken(messageData, trade);
            }
        }
        else if (messageData.TryGet(out TransactionExecuted executed) &&
                 messageData.MessageDomain.Root.TryGet(out trade) &&
                 trade.Platform == TradingPlatform.TraderJoe)
        {
            MessageDomain.TerminateDomainsOf(messageData);
            OnMessage(new TradeCompleted(messageData, executed.Success,
                                         BlockchainName.Avalanche,
                                         trade.OriginalTargetAmount,
                                         trade.ExpectedAmount,
                                         trade.TokenType));
        }
    }

    private void TryEstimateGas(EstimatingGasBasedOnTransactions estimatingGas)
    {
        if (estimatingGas.GasType != GasType.Trade || estimatingGas.BlockchainName != BlockchainName.Avalanche)
        {
            return;
        }

        string sourceChainId = ConfigurationManager.AppSettings["AvalancheId"]
                               ?? throw new ConfigurationErrorsException("AvalancheId not configured");
        string sourceContractAddress = ConfigurationManager.AppSettings["TraderJoeRouterAddress"]
                                      ?? throw new ConfigurationErrorsException("TraderJoeRouterAddress not configured");
        int gas = EstimateGas(sourceChainId, sourceContractAddress);
        OnMessage(new GasEstimatedBasedOnTransactions(estimatingGas, gas, GasType.Trade, BlockchainName.Avalanche));
    }

    private static int EstimateGas(string sourceChainId, string sourceContractAddress)
    {
        using HttpClient client = new HttpClient();
        int gasSpent = int.MinValue;
        int page = 0;
        while (gasSpent < 0)
        {
            string url = string.Format(Extensions.CovalentTransactionApi, sourceChainId, sourceContractAddress,
                                       page, 5, ConfigurationManager.AppSettings["CovalentApiKey"]);
            page++;
            if (page > 100)
            {
                throw new InvalidOperationException(
                    "No Send transaction in the last 500 transaction -> Something is off.");
            }

            Task<HttpResponseMessage>? getCall = null;
            for (int i = 0; i < 3 && getCall?.Result.IsSuccessStatusCode != true; i++)
            {
                getCall = client.GetAsync(url);
                getCall.Wait();
                Thread.Sleep(1000);
            }
            
            if (getCall?.Result.IsSuccessStatusCode != true)
            {
                throw new InvalidOperationException(
                    $"Status Code {getCall.Result.StatusCode} does not suggest success.");
            }

            Task<string> contentCall = getCall.Result.Content.ReadAsStringAsync();
            contentCall.Wait();
            JObject jObject = JObject.Parse(contentCall.Result);
            JObject? lastSendLog = jObject["data"]?["items"]?.Children<JObject>()
                                                             .Where(e => e["log_events"]?.Any() == true)
                                                             .Select(e => e["log_events"]?[0]?["decoded"] as JObject)
                                                             .FirstOrDefault(
                                                                  e => e?["name"]?.Value<string>() == "swapExactTokensForTokens" &&
                                                                       e?["gas_offered"]?.Value<int>() >
                                                                       e?["gas_spent"]?.Value<int>());
            if (lastSendLog == null)
            {
                continue;
            }

            gasSpent = lastSendLog["gas_spent"]!.Value<int>();
        }

        return gasSpent;
    }

    private void TradeTokenForToken(Message messageData, TradeInitiating trade)
    {
        BigInteger amount = Web3.Convert.ToWei(trade.Amount.RoundedAmount(), trade.FromTokenDecimals);

        string liquidityGoalAddress =
            trade.FromTokenId.Equals(trade.LiquidityPair.UnstableTokenId, StringComparison.OrdinalIgnoreCase)
                ? trade.LiquidityPair.PairedTokenId
                : trade.LiquidityPair.UnstableTokenId;
        string liquidityStartAddress =
            trade.ToTokenId.Equals(trade.LiquidityPair.UnstableTokenId, StringComparison.OrdinalIgnoreCase)
                ? trade.LiquidityPair.PairedTokenId
                : trade.LiquidityPair.UnstableTokenId;
        List<string> path = new() { trade.FromTokenId };
        if (!liquidityStartAddress.Equals(trade.FromTokenId, StringComparison.OrdinalIgnoreCase))
        {
            path.Add(liquidityStartAddress);
        }
        if (!liquidityGoalAddress.Equals(trade.ToTokenId, StringComparison.OrdinalIgnoreCase))
        {
            path.Add(liquidityGoalAddress);
        }
        path.Add(trade.ToTokenId);
        
        object[] parameters =
        {
            amount,
            0,
            path.ToArray(),
            trade.WalletAddress,
            (DateTime.UtcNow.Ticks + 10000)
        };

        string contractAddress = ConfigurationManager.AppSettings["TraderJoeRouterAddress"]
                                 ?? throw new ConfigurationErrorsException(
                                     "TraderJoeRouterAddress not configured.");

        MessageDomain.CreateNewDomainsFor(messageData);
        OnMessage(new TransactionExecuting(messageData, BlockchainName.Avalanche,
                                           "TraderJoe",
                                           contractAddress,
                                           "swapExactTokensForTokens",
                                           new HexBigInteger(0),
                                           parameters));
    }

    private void TradeTokenForNative(Message messageData, TradeInitiating trade)
    {
        BigInteger amount = Web3.Convert.ToWei(trade.Amount.RoundedAmount(), trade.FromTokenDecimals);

        string[] path = trade.FromTokenId.Equals(trade.LiquidityPair.UnstableTokenId, StringComparison.OrdinalIgnoreCase) &&
                        trade.ToTokenId.Equals(trade.LiquidityPair.PairedTokenId,StringComparison.OrdinalIgnoreCase)
                            ? new[] { trade.FromTokenId, trade.ToTokenId }
                            : new[] { trade.FromTokenId, trade.LiquidityPair.PairedTokenId, trade.ToTokenId };

        object[] parameters =
        {
            amount,
            0,
            path,
            trade.WalletAddress,
            (DateTime.UtcNow.Ticks + 10000)
        };

        string contractAddress = ConfigurationManager.AppSettings["TraderJoeRouterAddress"]
                                 ?? throw new ConfigurationErrorsException(
                                     "TraderJoeRouterAddress not configured.");

        MessageDomain.CreateNewDomainsFor(messageData);
        OnMessage(new TransactionExecuting(messageData, BlockchainName.Avalanche,
                                           "TraderJoe",
                                           contractAddress,
                                           "swapExactTokensForETH",
                                           new HexBigInteger(0),
                                           parameters));
    }
}