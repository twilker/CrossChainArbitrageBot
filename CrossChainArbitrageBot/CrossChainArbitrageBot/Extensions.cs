using System;

namespace CrossChainArbitrageBot;

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
}