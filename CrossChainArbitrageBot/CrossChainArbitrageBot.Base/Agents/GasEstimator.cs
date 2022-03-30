using System.Configuration;
using System.Globalization;
using Agents.Net;
using CrossChainArbitrageBot.Base.Messages;
using CrossChainArbitrageBot.Base.Models;
using HtmlAgilityPack;
using Nethereum.Util;
using Nethereum.Web3;

namespace CrossChainArbitrageBot.Base.Agents;

[Consumes(typeof(InitializeMessage))]
[Consumes(typeof(DataUpdated))]
public class GasEstimator : Agent
{
    private const double DefaultAvaxGasCosts = 60;
    private const double DefaultBscGasCosts = 5;
    private const int CrawlerSleep = 2000;

    private double? bnbPrice;
    private double? avaxPrice;
    private DataUpdate? buySide;
    private DataUpdate? sellSide;
    private Estimator estimator = new();
    
    public GasEstimator(IMessageBoard messageBoard) : base(messageBoard)
    {
    }

    protected override void ExecuteCore(Message messageData)
    {
        if (messageData.Is<InitializeMessage>())
        {
            StartGasCrawlerCycle(messageData);
        }
        else if (messageData.TryGet(out DataUpdated updated))
        {
            if (estimator.IsDefault)
            {
                CompleteEstimator(updated);
            }
            bnbPrice = updated.Updates.Bsc().NativePrice;
            avaxPrice = updated.Updates.Avalanche().NativePrice;
            buySide = updated.Updates.BuySide();
            sellSide = updated.Updates.SellSide();
        }
    }

    private void CompleteEstimator(DataUpdated updated)
    {
        DataUpdate bscUpdate = updated.Updates.Bsc();
        bool bscLongTrade = bscUpdate.Liquidity.TradePath(bscUpdate.StableId, bscUpdate.UnstableId).Length > 2;
        int bscTradeGas = bscLongTrade
                              ? int.Parse(ConfigurationManager.AppSettings["BscLongTradeGasCosts"] ??
                                          throw new ConfigurationErrorsException("BscLongTradeGasCosts not defined."))
                              : int.Parse(ConfigurationManager.AppSettings["BscSimpleTradeGasCosts"] ??
                                          throw new ConfigurationErrorsException("BscSimpleTradeGasCosts not defined."));
        
        DataUpdate avaxUpdate = updated.Updates.Avalanche();
        bool avaxLongTrade = avaxUpdate.Liquidity.TradePath(avaxUpdate.StableId, avaxUpdate.UnstableId).Length > 2;
        int avaxTradeGas = avaxLongTrade
                              ? int.Parse(ConfigurationManager.AppSettings["AvalancheLongTradeGasCosts"] ??
                                          throw new ConfigurationErrorsException("AvalancheLongTradeGasCosts not defined."))
                              : int.Parse(ConfigurationManager.AppSettings["AvalancheSimpleTradeGasCosts"] ??
                                          throw new ConfigurationErrorsException("AvalancheSimpleTradeGasCosts not defined."));

        int bscBridgeGas = int.Parse(ConfigurationManager.AppSettings["CelerBridgeBscGasCosts"] ??
                                     throw new ConfigurationErrorsException("CelerBridgeBscGasCosts not defined."));
        int avaxBridgeGas = int.Parse(ConfigurationManager.AppSettings["CelerBridgeAvalancheGasCosts"] ??
                                     throw new ConfigurationErrorsException("CelerBridgeAvalancheGasCosts not defined."));
        double bscBridgeCosts = double.Parse(ConfigurationManager.AppSettings["CelerBscBridgeCosts"] ??
                                         throw new ConfigurationErrorsException("CelerBscBridgeCosts not defined."));
        double avaxBridgeCosts = double.Parse(ConfigurationManager.AppSettings["CelerAvalancheBridgeCosts"] ??
                                         throw new ConfigurationErrorsException("CelerAvalancheBridgeCosts not defined."));
        double bscGasPriority = double.Parse(ConfigurationManager.AppSettings["BscGasPricePriority"] ??
                                         throw new ConfigurationErrorsException("BscGasPricePriority not defined."));
        double avaxGasPriority = double.Parse(ConfigurationManager.AppSettings["AvalancheGasPricePriority"] ??
                                              throw new ConfigurationErrorsException("AvalancheGasPricePriority not defined."));

        estimator = new Estimator(bscTradeGas, avaxTradeGas, bscBridgeGas, avaxBridgeGas, bscBridgeCosts,
                                  avaxBridgeCosts, bscGasPriority, avaxGasPriority);
    }

    private void StartGasCrawlerCycle(Message messageData)
    {
        Task.Run(async () =>
        {
            if (bnbPrice != null &&
                avaxPrice != null &&
                !estimator.IsDefault)
            {
                (int avaxBridgeOperations, int bscBridgeOperations) = CountBridgeOperations();
                GasPrices prices = await CrawlGasCosts();
                GasEstimation gasEstimate = estimator.CalculateGasCosts(prices.Bnb, prices.Avax, bnbPrice.Value,
                                                                        avaxPrice.Value, avaxBridgeOperations,
                                                                        bscBridgeOperations);
                OnMessage(new GasEstimated(messageData, gasEstimate));
            }
            Thread.Sleep(CrawlerSleep);
            StartGasCrawlerCycle(messageData);
        });
    }

    private BridgeOperations CountBridgeOperations()
    {
        if (buySide == null ||
            sellSide == null)
        {
            return new BridgeOperations(1, 1);
        }
        int bscBridgeOperations = 1;
        int avaxBridgeOperations = 1;
        if (buySide.Value.StableAmount < 100 && sellSide.Value.StableAmount > 100)
        {
            if (sellSide.Value.BlockchainName == BlockchainName.Bsc)
            {
                bscBridgeOperations++;
            }
            else
            {
                avaxBridgeOperations++;
            }
        }
        if (sellSide.Value.UnstableAmount*sellSide.Value.UnstablePrice < 100 && 
            buySide.Value.UnstableAmount*sellSide.Value.UnstablePrice > 100)
        {
            if (buySide.Value.BlockchainName == BlockchainName.Bsc)
            {
                bscBridgeOperations++;
            }
            else
            {
                avaxBridgeOperations++;
            }
        }

        return new BridgeOperations(avaxBridgeOperations, bscBridgeOperations);
    }

    private readonly record struct BridgeOperations(int AvaxBridgeOperations, int BscBridgeOperations);

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

    private readonly record struct Estimator(int BscTradeGas,
                                             int AvaxTradeGas,
                                             int BscBridgeGas,
                                             int AvaxBridgeGas,
                                             double BscBridgeCosts,
                                             double AvaxBridgeCosts, 
                                             double BscGasPriority, 
                                             double AvaxGasPriority)
    {
        public bool IsDefault => this == default;
        public GasEstimation CalculateGasCosts(double bscGasCost, double avaxGasCost, double bnbPrice, double avaxPrice,
                                               int avaxBridgeOperations, int bscBridgeOperations)
        {
            double bnbTradeAmount = (double)Web3.Convert.FromWei(Web3.Convert.ToWei(
                                                                     new BigDecimal(BscTradeGas * bscGasCost * BscGasPriority),
                                                                     UnitConversion.EthUnit.Gwei));
            double bscTradeCost = bnbTradeAmount * bnbPrice;
            double bnbBridgeAmount = (double)Web3.Convert.FromWei(Web3.Convert.ToWei(
                                                                      new BigDecimal(BscBridgeGas * bscGasCost * BscGasPriority),
                                                                      UnitConversion.EthUnit.Gwei));
            double bscBridgeCost = bnbBridgeAmount * bnbPrice;
            double avalancheTradeAmount = (double)Web3.Convert.FromWei(Web3.Convert.ToWei(
                                                                            new BigDecimal(
                                                                                AvaxTradeGas * avaxGasCost * AvaxGasPriority),
                                                                            UnitConversion.EthUnit.Gwei));
            double avaxTradeCost = avalancheTradeAmount * avaxPrice;
            double avalancheBridgeAmount = (double)Web3.Convert.FromWei(Web3.Convert.ToWei(
                                                                            new BigDecimal(AvaxBridgeGas * avaxGasCost * AvaxGasPriority),
                                                                            UnitConversion.EthUnit.Gwei));
            double avaxBridgeCost = avalancheBridgeAmount * avaxPrice;
            double gasCost = bscTradeCost +
                             avaxTradeCost +
                             avaxBridgeOperations * (avaxBridgeCost + AvaxBridgeCosts) +
                             bscBridgeOperations * (bscBridgeCost + BscBridgeCosts);
            return new GasEstimation(gasCost, bnbTradeAmount, bnbBridgeAmount,
                                     avalancheTradeAmount, avalancheBridgeAmount,
                                     BscBridgeCosts, AvaxBridgeCosts, bscGasCost * BscGasPriority,
                                     avaxGasCost * AvaxGasPriority);
        }
    };
}