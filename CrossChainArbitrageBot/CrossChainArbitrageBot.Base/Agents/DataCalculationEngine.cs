using Agents.Net;
using CrossChainArbitrageBot.Base.Messages;
using CrossChainArbitrageBot.Base.Models;

namespace CrossChainArbitrageBot.Base.Agents;

[Intercepts(typeof(DataUpdated))]
public class DataCalculationEngine : InterceptorAgent
{
    public DataCalculationEngine(IMessageBoard messageBoard) : base(messageBoard)
    {
    }

    protected override InterceptionAction InterceptCore(Message messageData)
    {
        DataUpdated updated = messageData.Get<DataUpdated>();
        DataUpdate bscUpdate = updated.Updates.First(u => u.BlockchainName == BlockchainName.Bsc);
        DataUpdate avalancheUpdate = updated.Updates.First(u => u.BlockchainName == BlockchainName.Avalanche);
        
        double spread = (avalancheUpdate.UnstablePrice - bscUpdate.UnstablePrice) / bscUpdate.UnstablePrice;
        SpreadProfit optimal = new();
        for (double testSpread = 0; testSpread < Math.Abs(spread); testSpread+=0.0005)
        {
            SpreadProfit profit = CalculateOptimalSpread(bscUpdate.Liquidity, avalancheUpdate.Liquidity,
                                                         spread, testSpread);
            if (profit.OptimalProfit > optimal.OptimalProfit)
            {
                optimal = profit;
            }
        }

        double targetSpread = optimal.TargetSpread;
        double maximumVolumeToTargetSpread = optimal.OptimalVolume;
        double profitByMaximumVolume = optimal.OptimalProfit;

        SpreadDataUpdated.Decorate(updated, spread, targetSpread, maximumVolumeToTargetSpread, profitByMaximumVolume);
        return InterceptionAction.Continue;
    }
    
    private readonly record struct SpreadProfit(double OptimalVolume, double OptimalProfit, double TargetSpread);

    private SpreadProfit CalculateOptimalSpread(Liquidity bscLiquidity, Liquidity avalancheLiquidity, double spread, double targetSpread)
    {
        double bscConstant = Math.Sqrt(bscLiquidity.TokenAmount * bscLiquidity.UsdPaired);
        double avalancheConstant = Math.Sqrt(avalancheLiquidity.TokenAmount * avalancheLiquidity.UsdPaired);
        double bscChange = (Math.Abs(spread) - targetSpread) *(avalancheConstant/(avalancheConstant+bscConstant));
        double maximumVolumeToTargetSpread = CalculateVolumeSpreadOptimum(bscLiquidity, bscChange)*2;
        double profitByMaximumVolume = (maximumVolumeToTargetSpread/2).CalculateProfit(bscLiquidity, avalancheLiquidity, spread > 0);
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