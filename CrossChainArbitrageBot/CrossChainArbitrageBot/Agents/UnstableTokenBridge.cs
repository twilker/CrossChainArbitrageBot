using System;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Agents.Net;
using Newtonsoft.Json.Linq;

namespace CrossChainArbitrageBot.Agents;

public class UnstableTokenBridge : Agent
{
    private const string CovalentTransactionApi =
        "https://api.covalenthq.com/v1/{0}/address/{1}/transactions_v2/?quote-currency=USD&format=JSON&block-signed-at-asc=false&no-logs=false&page-number={2}&page-size={3}&key={4}";

    public UnstableTokenBridge(IMessageBoard messageBoard, string name = null) : base(messageBoard, name)
    {
    }

    protected override void ExecuteCore(Message messageData)
    {
        throw new System.NotImplementedException();
    }

    private static long GetLastUsedNonce(string sourceChainId, string sourceContractAddress)
    {
        using HttpClient client = new HttpClient();
        long result = long.MinValue;
        int page = 0;
        while (result < 0)
        {
            string url = string.Format(CovalentTransactionApi, sourceChainId, sourceContractAddress,
                                       page, 5, ConfigurationManager.AppSettings["CovalentApiKey"]);
            page++;
            if (page > 100)
            {
                throw new InvalidOperationException("No Send transaction in the last 500 transaction -> Something is off.");
            }
            Task<HttpResponseMessage> getCall = client.GetAsync(url);
            getCall.Wait();
            if (!getCall.Result.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Status Code {getCall.Result.StatusCode} does not suggest success.");
            }

            Task<string> contentCall = getCall.Result.Content.ReadAsStringAsync();
            contentCall.Wait();
            JObject jObject = JObject.Parse(contentCall.Result);
            JObject? lastSendLog = jObject["data"]?["items"]?.Children<JObject>()
                                                             .Select(e => (JObject?)e["log_events"]?[0]?["decoded"])
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