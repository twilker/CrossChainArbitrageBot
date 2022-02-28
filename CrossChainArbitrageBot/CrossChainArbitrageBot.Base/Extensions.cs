using CrossChainArbitrageBot.Base.Messages;

namespace CrossChainArbitrageBot.Base;

public static class Extensions
{
    public static double RoundedAmount(this double amount)
    {
        int i;
        for (i = 1; i < 18; i++)
        {
            double value = amount * Math.Pow(10, i);
            double fraction = value - Math.Floor(value);
            if (fraction < 0.000001)
            {
                break;
            }
        }

        return Math.Floor(amount * Math.Pow(10, i-1)) / Math.Pow(10, i-1);
    }
    
    public static double CalculateProfit(this double volume, Liquidity bscLiquidity, Liquidity avalancheLiquidity, bool buyOnBsc)
    {
        (double buyTokenAmount, double buyUsdPaired, _, _) = buyOnBsc ? bscLiquidity : avalancheLiquidity;
        (double sellTokenAmount, double sellUsdPaired, _, _) = buyOnBsc ? avalancheLiquidity : bscLiquidity;
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