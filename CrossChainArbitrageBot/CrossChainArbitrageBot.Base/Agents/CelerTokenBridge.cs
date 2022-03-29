using System.Configuration;
using System.Numerics;
using System.Runtime.ExceptionServices;
using Agents.Net;
using CrossChainArbitrageBot.Base.Messages;
using CrossChainArbitrageBot.Base.Models;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Newtonsoft.Json.Linq;

namespace CrossChainArbitrageBot.Base.Agents;

[Consumes(typeof(TokenBridging))]
[Consumes(typeof(TransactionExecuted))]
[Consumes(typeof(EstimatingGasBasedOnTransactions))]
public class CelerTokenBridge : Agent
{
    private long lastKnownNonceValue = 1646285611413;

    private readonly Random random = new();

    public CelerTokenBridge(IMessageBoard messageBoard, string name = null) : base(messageBoard, name)
    {
    }

    protected override void ExecuteCore(Message messageData)
    {
        if (messageData.TryGet(out EstimatingGasBasedOnTransactions estimatingGas))
        {
            TryEstimateGas(estimatingGas);
            return;
        }
        if(messageData.TryGet(out TokenBridging bridging))
        {
            string contractAddress = bridging.SourceChain switch
            {
                BlockchainName.Bsc => ConfigurationManager.AppSettings["BscCelerBridgeAddress"] ?? throw new ConfigurationErrorsException("BscCelerBridgeAddress not defined."),
                BlockchainName.Avalanche => ConfigurationManager.AppSettings["AvalancheCelerBridgeAddress"] ?? throw new ConfigurationErrorsException("AvalancheCelerBridgeAddress not defined."),
                _ => throw new InvalidOperationException("Not implemented.")
            };
            string token = bridging.BridgedSourceToken;
            string sourceChainId = bridging.SourceChain switch
            {
                BlockchainName.Bsc => ConfigurationManager.AppSettings["BscId"] ?? throw new ConfigurationErrorsException("BscId not defined."),
                BlockchainName.Avalanche => ConfigurationManager.AppSettings["AvalancheId"] ?? throw new ConfigurationErrorsException("AvalancheId not defined."),
                _ => throw new InvalidOperationException("Not implemented.")
            };
            BigInteger targetChainId = bridging.SourceChain switch
            {
                BlockchainName.Bsc => int.Parse(ConfigurationManager.AppSettings["AvalancheId"] ?? throw new ConfigurationErrorsException("AvalancheId not defined.")),
                BlockchainName.Avalanche => int.Parse(ConfigurationManager.AppSettings["BscId"] ?? throw new ConfigurationErrorsException("BscId not defined.")),
                _ => throw new InvalidOperationException("Not implemented.")
            };

            long lastNonce = lastKnownNonceValue;
            try
            {
                lastNonce = GetLastUsedNonce(sourceChainId, contractAddress);
            }
            catch (Exception e)
            {
                OnMessage(new ExceptionMessage(ExceptionDispatchInfo.Capture(e), messageData, this));
            }
            long nextNonce = lastNonce + random.NextInt64(500000, 2000000);
            
            BigInteger amount = Web3.Convert.ToWei(bridging.Amount.RoundedAmount(), bridging.Decimals);

            //last is maxSlippage - roughly 1%
            object[] parameters = {
                bridging.TargetWallet,
                token,
                amount,
                targetChainId,
                nextNonce,
                93702
            };

            MessageDomain.CreateNewDomainsFor(messageData);
            OnMessage(new TransactionExecuting(messageData, bridging.SourceChain,
                                               "Celer", contractAddress, 
                                               "send",
                                               new HexBigInteger(0), parameters));
        }
        else if (messageData.TryGet(out TransactionExecuted executed) &&
                 messageData.MessageDomain.Root.TryGet(out bridging))
        {
            MessageDomain.TerminateDomainsOf(messageData);
            OnMessage(new TokenBridged(messageData, executed.Success, 
                                       bridging.Amount,
                                       bridging.SourceChain switch
                                       {
                                           BlockchainName.Bsc => BlockchainName.Avalanche,
                                           BlockchainName.Avalanche => BlockchainName.Bsc,
                                           _ => throw new InvalidOperationException("Not implemented.")
                                       }, bridging.BridgedSourceToken,
                          bridging.TokenType));
        }
    }

    private void TryEstimateGas(EstimatingGasBasedOnTransactions estimatingGas)
    {
        switch (estimatingGas.GasType)
        {
            case GasType.Trade:
                return;
            case GasType.Bridge:
                string sourceChainId = estimatingGas.BlockchainName == BlockchainName.Bsc
                                           ? ConfigurationManager.AppSettings["BscId"]
                                             ?? throw new ConfigurationErrorsException("BscId not configured")
                                           : ConfigurationManager.AppSettings["AvalancheId"]
                                             ?? throw new ConfigurationErrorsException("AvalancheId not configured");
                string destinationId = estimatingGas.BlockchainName == BlockchainName.Bsc
                                           ? ConfigurationManager.AppSettings["AvalancheId"]
                                             ?? throw new ConfigurationErrorsException("AvalancheId not configured")
                                           : ConfigurationManager.AppSettings["BscId"]
                                             ?? throw new ConfigurationErrorsException("BscId not configured");
                string sourceContractAddress = estimatingGas.BlockchainName == BlockchainName.Bsc
                                                   ? ConfigurationManager.AppSettings["BscCelerBridgeAddress"]
                                                     ?? throw new ConfigurationErrorsException("BscCelerBridgeAddress not configured")
                                                   : ConfigurationManager.AppSettings["AvalancheCelerBridgeAddress"]
                                                     ?? throw new ConfigurationErrorsException("AvalancheCelerBridgeAddress not configured");
                int gasSpent = EstimateGas(sourceChainId, sourceContractAddress, destinationId);
                OnMessage(new GasEstimatedBasedOnTransactions(estimatingGas, gasSpent, GasType.Bridge,
                                                              estimatingGas.BlockchainName));
                break;
            case GasType.BridgeFee:
                OnMessage(new GasEstimatedBasedOnTransactions(estimatingGas, 1, GasType.BridgeFee,
                                                              estimatingGas.BlockchainName));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static int EstimateGas(string sourceChainId, string sourceContractAddress, string destinationId)
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
                throw new InvalidOperationException("No Send transaction in the last 500 transaction -> Something is off.");
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
                throw new InvalidOperationException($"Status Code {getCall.Result.StatusCode} does not suggest success.");
            }

            Task<string> contentCall = getCall.Result.Content.ReadAsStringAsync();
            contentCall.Wait();
            JObject jObject = JObject.Parse(contentCall.Result);
            JObject? lastSendLog = jObject["data"]?["items"]?.Children<JObject>()
                                                             .Where(e => e["log_events"]?.Any() == true)
                                                             .Select(e => e["log_events"]?[0]?["decoded"] as JObject)
                                                             .FirstOrDefault(e => e?["name"]?.Value<string>() == "Send" &&
                                                                                 e?["dstChainId"]?.Value<string>() ==
                                                                                 destinationId &&
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

    private static long GetLastUsedNonce(string sourceChainId, string sourceContractAddress)
    {
        using HttpClient client = new HttpClient();
        long result = long.MinValue;
        int page = 0;
        while (result < 0)
        {
            string url = string.Format(Extensions.CovalentTransactionApi, sourceChainId, sourceContractAddress,
                                       page, 5, ConfigurationManager.AppSettings["CovalentApiKey"]);
            page++;
            if (page > 100)
            {
                throw new InvalidOperationException("No Send transaction in the last 500 transaction -> Something is off.");
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
                throw new InvalidOperationException($"Status Code {getCall.Result.StatusCode} does not suggest success.");
            }

            Task<string> contentCall = getCall.Result.Content.ReadAsStringAsync();
            contentCall.Wait();
            JObject jObject = JObject.Parse(contentCall.Result);
            JObject? lastSendLog = jObject["data"]?["items"]?.Children<JObject>()
                                                             .Where(e => e["log_events"]?.Any() == true)
                                                             .Select(e => e["log_events"]?[0]?["decoded"] as JObject)
                                                             .FirstOrDefault(e => e?["name"]?.Value<string>() == "Send");
            if (lastSendLog == null)
            {
                continue;
            }
            string? lastNonce = lastSendLog?["params"]?.Children<JObject>()
                                                       .FirstOrDefault(e => e["name"]?.Value<string>() == "nonce")
                                                        ?["value"]?.Value<string>();
            if (!long.TryParse(lastNonce, out result))
            {
                throw new InvalidOperationException($"Could not find any Send command in {Environment.NewLine}{contentCall.Result}");
            }
        }

        return result;
    }
}