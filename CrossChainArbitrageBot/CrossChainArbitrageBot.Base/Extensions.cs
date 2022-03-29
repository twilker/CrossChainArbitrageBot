using CrossChainArbitrageBot.Base.Messages;

namespace CrossChainArbitrageBot.Base;

public static class Extensions
{
    public const string CovalentTransactionApi =
        "https://api.covalenthq.com/v1/{0}/address/{1}/transactions_v2/?quote-currency=USD&format=JSON&block-signed-at-asc=false&no-logs=false&page-number={2}&page-size={3}&key={4}";
    
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

    public static double CalculateActualProfit(this double buyVolume, double sellAmount, Liquidity buyLiquidity,
                                               Liquidity sellLiquidity,
                                               double liquidityProviderFee, double bridgeFee)
    {
        (double buyTokenAmount, double buyUsdPaired, _, _) = buyLiquidity;
        (double sellTokenAmount, double sellUsdPaired, _, _) = sellLiquidity;
            
        //simulate buy
        double newUsd = buyUsdPaired + buyVolume*(1-liquidityProviderFee);
        double newToken = buyTokenAmount * buyUsdPaired / newUsd;
        double tokenReceived = buyTokenAmount - newToken;
        tokenReceived -= bridgeFee/(newUsd/newToken);
            
        //simulate sell
        newToken = sellTokenAmount + sellAmount*(1-liquidityProviderFee);
        newUsd = sellTokenAmount * sellUsdPaired / newToken;
        double soldValue = sellUsdPaired - newUsd - bridgeFee;
        double boughtValue = tokenReceived * newUsd / newToken;

        return boughtValue + soldValue - buyVolume - sellAmount * sellLiquidity.Price;
    }
    
    public static double CalculateProfit(this double volume, Liquidity buyLiquidity, Liquidity sellLiquidity, 
                                         double liquidityProviderFee, double bridgeFee)
    {
        (double buyTokenAmount, double buyUsdPaired, _, _) = buyLiquidity;
        (double sellTokenAmount, double sellUsdPaired, _, _) = sellLiquidity;
        double tokenAmount = volume*(1-liquidityProviderFee) / (sellUsdPaired / sellTokenAmount);
            
        //simulate buy
        double newUsd = buyUsdPaired + volume*(1-liquidityProviderFee);
        double newToken = buyTokenAmount * buyUsdPaired / newUsd;
        double tokenReceived = buyTokenAmount - newToken;
        tokenReceived -= bridgeFee/(newUsd/newToken);
            
        //simulate sell
        newToken = sellTokenAmount + tokenAmount;
        newUsd = sellTokenAmount * sellUsdPaired / newToken;
        double soldValue = sellUsdPaired - newUsd - bridgeFee;
        double boughtValue = tokenReceived * newUsd / newToken;

        return boughtValue + soldValue - volume*2;
    }
}