using System.Globalization;
using Agents.Net;
using CrossChainArbitrageBot.Base.Messages;
using CrossChainArbitrageBot.Base.Models;
using HtmlAgilityPack;
using Nethereum.Util;
using Nethereum.Web3;

namespace CrossChainArbitrageBot.Base.Agents;

[Consumes(typeof(InitializeMessage))]
[Consumes(typeof(GasEstimatedBasedOnTransactions))]
[Consumes(typeof(DataUpdated))]
public class GasEstimator : Agent
{
    private const double DefaultAvaxGasCosts = 60;
    private const double DefaultBscGasCosts = 5;
    private const int CrawlerSleep = 2000;
    private const int EstimatorSleep = 6 * 60 * 60 * 1000;

    private double? bnbPrice;
    private double? avaxPrice;
    private Estimator estimator = new();
    
    public GasEstimator(IMessageBoard messageBoard) : base(messageBoard)
    {
    }

    protected override void ExecuteCore(Message messageData)
    {
        if (messageData.Is<InitializeMessage>())
        {
            StartGasCrawlerCycle(messageData);
            StartGasEstimationCycle(messageData);
        }
        else if (messageData.TryGet(out GasEstimatedBasedOnTransactions gasEstimated))
        {
            estimator = gasEstimated.GasType switch
            {
                GasType.Trade => gasEstimated.BlockchainName == BlockchainName.Bsc
                                     ? estimator with { BscTradeGas = (int)gasEstimated.Value }
                                     : estimator with { AvaxTradeGas = (int)gasEstimated.Value },
                GasType.Bridge => gasEstimated.BlockchainName == BlockchainName.Bsc
                                      ? estimator with { BscBridgeGas = (int)gasEstimated.Value }
                                      : estimator with { AvaxBridgeGas = (int)gasEstimated.Value },
                GasType.BridgeFee => gasEstimated.BlockchainName == BlockchainName.Bsc
                                         ? estimator with { BscBridgeCosts = gasEstimated.Value }
                                         : estimator with { AvaxBridgeCosts = gasEstimated.Value },
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        else if (messageData.TryGet(out DataUpdated updated))
        {
            bnbPrice = updated.Updates.First(u => u.BlockchainName == BlockchainName.Bsc)
                              .NativePrice;
            avaxPrice = updated.Updates.First(u => u.BlockchainName == BlockchainName.Avalanche)
                               .NativePrice;
        }
    }

    private void StartGasEstimationCycle(Message messageData)
    {
        Task.Run(() =>
        {
            OnMessage(new EstimatingGasBasedOnTransactions(messageData, GasType.Trade, BlockchainName.Bsc));
            OnMessage(new EstimatingGasBasedOnTransactions(messageData, GasType.Bridge, BlockchainName.Bsc));
            OnMessage(new EstimatingGasBasedOnTransactions(messageData, GasType.BridgeFee, BlockchainName.Bsc));
            OnMessage(new EstimatingGasBasedOnTransactions(messageData, GasType.Trade, BlockchainName.Avalanche));
            OnMessage(new EstimatingGasBasedOnTransactions(messageData, GasType.Bridge, BlockchainName.Avalanche));
            OnMessage(new EstimatingGasBasedOnTransactions(messageData, GasType.BridgeFee, BlockchainName.Avalanche));
            Thread.Sleep(EstimatorSleep);
            StartGasEstimationCycle(messageData);
        });
    }

    private void StartGasCrawlerCycle(Message messageData)
    {
        Task.Run(async () =>
        {
            if (estimator.IsComplete &&
                bnbPrice != null &&
                avaxPrice != null)
            {
                GasPrices prices = await CrawlGasCosts();
                GasEstimation gasEstimate = estimator.CalculateGasCosts(prices.Bnb, prices.Avax, bnbPrice.Value, 
                                                             avaxPrice.Value, 1,1);
                OnMessage(new GasEstimated(messageData, gasEstimate));
            }
            Thread.Sleep(CrawlerSleep);
            StartGasCrawlerCycle(messageData);
        });
    }

    private readonly record struct GasPrices(double Avax, double Bnb);

    private static async Task<GasPrices> CrawlGasCosts()
    {
        double avaxGas = await CrawlGas("https://snowtrace.io/gastracker", DefaultAvaxGasCosts);
        double bscGas = await CrawlGas("https://bscscan.com/gastracker", DefaultBscGasCosts);

        return new GasPrices(avaxGas, bscGas);

        async Task<double> CrawlGas(string url, double defaultGas)
        {
            double gas = defaultGas;
            try
            {
                using HttpClient client = new HttpClient();
                string html = await client.GetStringAsync(url);
                HtmlDocument document = new HtmlDocument();
                document.LoadHtml(html);
                HtmlNode? value = document.DocumentNode.Descendants("span")
                                          .FirstOrDefault(s => s.GetAttributeValue("id", string.Empty)
                                                                .Equals("rapidgas", StringComparison.OrdinalIgnoreCase));
                if (value != null)
                {
                    gas = double.Parse(value.InnerText.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0],new NumberFormatInfo
                    {
                        NumberDecimalSeparator = ".",
                        CurrencyDecimalSeparator = ".",
                        PercentDecimalSeparator = ".",
                        CurrencyGroupSeparator = ",",
                        NumberGroupSeparator = ",",
                        PercentGroupSeparator = ","
                    });
                }
            }
            catch (Exception)
            {
            }

            return gas;
        }
    }

    private record Estimator(int? BscTradeGas = null,
                             int? AvaxTradeGas = null,
                             int? BscBridgeGas = null,
                             int? AvaxBridgeGas = null,
                             double? BscBridgeCosts = null,
                             double? AvaxBridgeCosts = null)
    {
        public bool IsComplete => BscTradeGas.HasValue &&
                                  AvaxTradeGas.HasValue &&
                                  BscBridgeGas.HasValue &&
                                  AvaxBridgeGas.HasValue &&
                                  BscBridgeCosts.HasValue &&
                                  AvaxBridgeCosts.HasValue;

        public GasEstimation CalculateGasCosts(double bscGasCost, double avaxGasCost, double bnbPrice, double avaxPrice,
                                               int avaxBridgeOperations, int bscBridgeOperations)
        {
            if (!IsComplete)
            {
                return new GasEstimation();
            }

            double bnbTradeAmount = (double)Web3.Convert.FromWei(Web3.Convert.ToWei(
                                                                     new BigDecimal(BscTradeGas!.Value * bscGasCost),
                                                                     UnitConversion.EthUnit.Gwei));
            double bscTradeCost = bnbTradeAmount * bnbPrice;
            double bnbBridgeAmount = (double)Web3.Convert.FromWei(Web3.Convert.ToWei(
                                                                      new BigDecimal(BscBridgeGas!.Value * bscGasCost),
                                                                      UnitConversion.EthUnit.Gwei));
            double bscBridgeCost = bnbBridgeAmount * bnbPrice;
            double avalancheTradeAmount = (double)Web3.Convert.FromWei(Web3.Convert.ToWei(
                                                                            new BigDecimal(
                                                                                AvaxTradeGas!.Value * avaxGasCost),
                                                                            UnitConversion.EthUnit.Gwei));
            double avaxTradeCost = avalancheTradeAmount * avaxPrice;
            double avalancheBridgeAmount = (double)Web3.Convert.FromWei(Web3.Convert.ToWei(
                                                                            new BigDecimal(AvaxBridgeGas!.Value * avaxGasCost),
                                                                            UnitConversion.EthUnit.Gwei));
            double avaxBridgeCost = avalancheBridgeAmount * avaxPrice;
            double gasCost = bscTradeCost +
                             avaxTradeCost +
                             avaxBridgeOperations * (avaxBridgeCost + AvaxBridgeCosts!.Value) +
                             bscBridgeOperations * (bscBridgeCost + BscBridgeCosts!.Value);
            return new GasEstimation(gasCost, bnbTradeAmount, bnbBridgeAmount,
                                     avalancheTradeAmount, avalancheBridgeAmount,
                                     BscBridgeCosts!.Value, AvaxBridgeCosts!.Value);
        }
    };
}