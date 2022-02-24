using System.Globalization;
using Agents.Net;
using CsvHelper;
using DataLogger.Messages;
using DataLogger.Models;

namespace DataLogger.Agents;

[Consumes(typeof(DataUpdated))]
public class DataLogger : Agent
{
    private const double SingleBrideStaticCost = 1;
    private const double BscGasCostsForPriorityTrade = 0.3833333;
    private const double AvalancheGasCostsForPriorityTrade = 0.3771429;
    private const double BridgingGasCosts = 0.25;
    private const double MaximumTotalVolume = 2000;
    private readonly CsvWriter writer;
    public DataLogger(IMessageBoard messageBoard) : base(messageBoard)
    {
        if (File.Exists("data.csv"))
        {
            File.Delete("data.csv");
        }
        StreamWriter streamWriter = new("data.csv");
        writer = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);
        AddDisposable(streamWriter);
        AddDisposable(writer);
        writer.WriteHeader<DataLoggerData>();
        writer.NextRecord();
    }

    protected override void ExecuteCore(Message messageData)
    {
        DataUpdated updated = messageData.Get<DataUpdated>();
        lock (writer)
        {
            DataLoggerData log = new()
            {
                Timestamp = DateTime.Now,
                BscPrice = updated.Updates.First(u => u.BlockchainName == BlockchainName.Bsc)
                                  .UnstablePrice,
                AvalanchePrice = updated.Updates.First(u => u.BlockchainName == BlockchainName.Avalanche)
                                        .UnstablePrice,
            };
            Liquidity bscLiquidity = updated.Updates.First(u => u.BlockchainName == BlockchainName.Bsc)
                                            .Liquidity;
            Liquidity avalancheLiquidity = updated.Updates.First(u => u.BlockchainName == BlockchainName.Avalanche)
                                            .Liquidity;
            log.Spread = (log.AvalanchePrice - log.BscPrice) / log.BscPrice * 100;
            CalculateOptimalSpread(bscLiquidity, avalancheLiquidity, log);
            writer.WriteRecord(log);
            writer.NextRecord();
            writer.Flush();
        }
    }

    private void CalculateOptimalSpread(Liquidity bscLiquidity, Liquidity avalancheLiquidity, DataLoggerData log)
    {
        double bscConstant = Math.Sqrt(bscLiquidity.TokenAmount * bscLiquidity.UsdPaired);
        double avalancheConstant = Math.Sqrt(avalancheLiquidity.TokenAmount * avalancheLiquidity.UsdPaired);
        double targetSpread = Math.Abs(log.Spread / 200);
        double bscChange = (Math.Abs(log.Spread) / 100 - targetSpread) *(avalancheConstant/(avalancheConstant+bscConstant));
        log.TotalVolume = Math.Min(CalculateVolumeSpreadOptimum(bscLiquidity, bscChange)*2, MaximumTotalVolume);
        log.SimulatedProfit = SimulateOptimalSellAndBuy(bscLiquidity, avalancheLiquidity, log.TotalVolume/2, log.Spread > 0);
        log.SimulatedProfitGasConsidered = log.SimulatedProfit - (2 * SingleBrideStaticCost +
                                                                  BscGasCostsForPriorityTrade +
                                                                  AvalancheGasCostsForPriorityTrade +
                                                                  2 * BridgingGasCosts);
    }

    private double CalculateVolumeSpreadOptimum(Liquidity liquidity, double targetSpreadChange)
    {
        double constant = liquidity.TokenAmount * liquidity.UsdPaired;
        double currentPrice = liquidity.UsdPaired / liquidity.TokenAmount;
        double targetPrice = currentPrice * (1 + targetSpreadChange);
        return Math.Sqrt(targetPrice * constant) - liquidity.UsdPaired;
    }

    private static double SimulateOptimalSellAndBuy(Liquidity bscLiquidity, Liquidity avalancheLiquidity, double volume, bool buyOnBsc)
    {
        (double buyTokenAmount, double buyUsdPaired, _) = buyOnBsc ? bscLiquidity : avalancheLiquidity;
        (double sellTokenAmount, double sellUsdPaired, _) = buyOnBsc ? avalancheLiquidity : bscLiquidity;
        double tokenAmount = volume / (buyUsdPaired / buyTokenAmount);
            
        //simulate buy
        double newUsd = buyUsdPaired + volume;
        double newToken = buyTokenAmount * buyUsdPaired / newUsd;
        double tokenReceived = buyTokenAmount - newToken;
            
        //simulate sell
        newToken = sellTokenAmount + tokenAmount;
        newUsd = sellTokenAmount * sellUsdPaired / newToken;
        double soldValue = sellUsdPaired - newUsd;
        double boughtValue = tokenReceived * newUsd / newToken;

        return boughtValue + soldValue - volume - tokenAmount * sellUsdPaired / sellTokenAmount;
    }
}