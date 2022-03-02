using System.Configuration;
using System.Diagnostics;
using Agents.Net;
using CrossChainArbitrageBot.Base.Messages;
using CrossChainArbitrageBot.Base.Models;

namespace CrossChainArbitrageBot.Base.Agents;

[Intercepts(typeof(DataUpdated))]
public class DataCalculationEngine : InterceptorAgent
{
    private readonly double liquidityProviderFee;
    private readonly double bridgeFee;
    private readonly double minimalProfit;

    public DataCalculationEngine(IMessageBoard messageBoard) : base(messageBoard)
    {
        liquidityProviderFee = double.Parse(ConfigurationManager.AppSettings["LiquidityProviderFee"] ??
                                            throw new ConfigurationErrorsException("LiquidityProviderFee not found."));
        bridgeFee = double.Parse(ConfigurationManager.AppSettings["BridgeCostsForProfitCalculation"] ??
                                 throw new ConfigurationErrorsException("BridgeCostsForProfitCalculation not found."));
        minimalProfit = double.Parse(ConfigurationManager.AppSettings["MinimalProfit"] ??
                                 throw new ConfigurationErrorsException("MinimalProfit not found."));
    }

    protected override InterceptionAction InterceptCore(Message messageData)
    {
        DataUpdated updated = messageData.Get<DataUpdated>();
        DataUpdate bscUpdate = updated.Updates.First(u => u.BlockchainName == BlockchainName.Bsc);
        DataUpdate avalancheUpdate = updated.Updates.First(u => u.BlockchainName == BlockchainName.Avalanche);
        
        double spread = (avalancheUpdate.UnstablePrice - bscUpdate.UnstablePrice) / bscUpdate.UnstablePrice;
        Liquidity bscLiquidity = bscUpdate.Liquidity;
        Liquidity avalancheLiquidity = avalancheUpdate.Liquidity;
        Liquidity buyLiquidity = spread > 0
                                     ? bscLiquidity
                                     : avalancheLiquidity;
        Liquidity sellLiquidity = spread > 0
                                      ? avalancheLiquidity
                                      : bscLiquidity;

        MinimalSpread optimalTokenAmount = TestForOptimalTokenAmount(bscLiquidity, avalancheLiquidity);
        double optimalBuyVolume = buyLiquidity.Constant / (buyLiquidity.TokenAmount - optimalTokenAmount.TokenAmount) -
                                  buyLiquidity.UsdPaired;
        optimalBuyVolume = Math.Min(optimalBuyVolume, bscUpdate.StableAmount + avalancheUpdate.StableAmount);
        double currentProfit = optimalBuyVolume.CalculateActualProfit(
            bscUpdate.UnstableAmount + avalancheUpdate.UnstableAmount,
            buyLiquidity,
            sellLiquidity,
            liquidityProviderFee, bridgeFee);

        SpreadDataUpdated.Decorate(updated, spread, optimalTokenAmount.Spread, optimalTokenAmount.TokenAmount,
                                   currentProfit);
        return InterceptionAction.Continue;
    }

    private SpreadProfit TestForOptimalProfit(double spread, Liquidity bscLiquidity, Liquidity avalancheLiquidity)
    {
        SpreadProfit optimal = new();
        for (double testSpread = 0; testSpread < Math.Abs(spread); testSpread += 0.0005)
        {
            SpreadProfit profit = CalculateOptimalSpread(bscLiquidity, avalancheLiquidity,
                                                         spread, testSpread);
            if (profit.OptimalProfit > optimal.OptimalProfit)
            {
                optimal = profit;
            }
        }

        return optimal;
    }

    private MinimalSpread TestForOptimalTokenAmount(Liquidity originalBscLiquidity, Liquidity originalAvalancheLiquidity)
    {
        MinimalSpread? bscPositive = null, bscNegative = null;
        for (double testSpread = 0.0005; testSpread < 0.1; testSpread+=0.0005)
        {
            TestSpread(testSpread);
        }

        if (!bscPositive.HasValue ||
            !bscNegative.HasValue)
        {
            throw new InvalidOperationException("Could not find minimal spread in 10% region.");
        }

        return bscPositive.Value.TokenAmount < bscNegative.Value.TokenAmount
                   ? bscPositive.Value
                   : bscNegative.Value;

        void TestSpread(double testSpread)
        {
            if (!bscPositive.HasValue)
            {
                Liquidity bscLiquidity = new Liquidity(
                    Math.Sqrt(originalBscLiquidity.Constant / (originalBscLiquidity.Price * (1 + testSpread))),
                    Math.Sqrt(originalBscLiquidity.Constant * originalBscLiquidity.Price * (1 + testSpread)),
                    originalBscLiquidity.PairedTokenId, originalBscLiquidity.UnstableTokenId);
                Liquidity avalancheLiquidity = new Liquidity(
                    Math.Sqrt(originalAvalancheLiquidity.Constant / originalBscLiquidity.Price),
                    Math.Sqrt(originalAvalancheLiquidity.Constant * originalBscLiquidity.Price),
                    originalBscLiquidity.PairedTokenId, originalBscLiquidity.UnstableTokenId);
                SpreadProfit profit = TestForOptimalProfit(-testSpread, bscLiquidity, avalancheLiquidity);
                if (profit.OptimalProfit > minimalProfit)
                {
                    double tokenAmount = profit.OptimalVolume * 0.5 / bscLiquidity.Price;
                    bscPositive = new MinimalSpread(testSpread, tokenAmount, profit);
                }
            }

            if (!bscNegative.HasValue)
            {
                Liquidity bscLiquidity = new Liquidity(
                    Math.Sqrt(originalBscLiquidity.Constant / (originalBscLiquidity.Price * (1 - testSpread))),
                    Math.Sqrt(originalBscLiquidity.Constant * originalBscLiquidity.Price * (1 - testSpread)),
                    originalBscLiquidity.PairedTokenId, originalBscLiquidity.UnstableTokenId);
                Liquidity avalancheLiquidity = new Liquidity(
                    Math.Sqrt(originalAvalancheLiquidity.Constant / originalBscLiquidity.Price),
                    Math.Sqrt(originalAvalancheLiquidity.Constant * originalBscLiquidity.Price),
                    originalBscLiquidity.PairedTokenId, originalBscLiquidity.UnstableTokenId);
                SpreadProfit profit = TestForOptimalProfit(testSpread, bscLiquidity, avalancheLiquidity);
                if (profit.OptimalProfit > minimalProfit)
                {
                    double tokenAmount = profit.OptimalVolume * 0.5 / avalancheLiquidity.Price;
                    bscNegative = new MinimalSpread(testSpread, tokenAmount, profit);
                }
            }
        }
    }

    private readonly record struct SpreadProfit(double OptimalVolume, double OptimalProfit, double TargetSpread);
    private readonly record struct MinimalSpread(double Spread, double TokenAmount, SpreadProfit Profit);

    private SpreadProfit CalculateOptimalSpread(Liquidity bscLiquidity, Liquidity avalancheLiquidity, double spread, double targetSpread)
    {
        double bscConstant = Math.Sqrt(bscLiquidity.TokenAmount * bscLiquidity.UsdPaired);
        double avalancheConstant = Math.Sqrt(avalancheLiquidity.TokenAmount * avalancheLiquidity.UsdPaired);
        double bscChange = (Math.Abs(spread) - targetSpread) *(avalancheConstant/(avalancheConstant+bscConstant));
        double maximumVolumeToTargetSpread = CalculateVolumeSpreadOptimum(bscLiquidity, bscChange)*2;
        double profitByMaximumVolume = (maximumVolumeToTargetSpread / 2).CalculateProfit(spread > 0
                ? bscLiquidity
                : avalancheLiquidity,
            spread > 0
                ? avalancheLiquidity
                : bscLiquidity,
            liquidityProviderFee,
            bridgeFee);
        return new SpreadProfit(maximumVolumeToTargetSpread, profitByMaximumVolume, targetSpread);
    }

    private double CalculateVolumeSpreadOptimum(Liquidity liquidity, double targetSpreadChange)
    {
        double constant = liquidity.TokenAmount * liquidity.UsdPaired;
        double currentPrice = liquidity.UsdPaired / liquidity.TokenAmount;
        double targetPrice = currentPrice * (1 + targetSpreadChange);
        return Math.Sqrt(targetPrice * constant) - liquidity.UsdPaired;
    }
}