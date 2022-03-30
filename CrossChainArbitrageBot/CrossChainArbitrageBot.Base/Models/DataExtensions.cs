using CrossChainArbitrageBot.Base.Messages;

namespace CrossChainArbitrageBot.Base.Models;

public static class DataExtensions
{
    public static DataUpdate Bsc(this DataUpdate[] updates)
    {
        return updates.First(u => u.BlockchainName == BlockchainName.Bsc);
    }
    
    public static DataUpdate Avalanche(this DataUpdate[] updates)
    {
        return updates.First(u => u.BlockchainName == BlockchainName.Avalanche);
    }
    
    public static DataUpdate BuySide(this DataUpdate[] updates)
    {
        return updates.OrderBy(u => u.UnstablePrice).First();
    }
    
    public static DataUpdate SellSide(this DataUpdate[] updates)
    {
        return updates.OrderByDescending(u => u.UnstablePrice).First();
    }

    public static string[] TradePath(this Liquidity liquidity, string from, string to)
    {
        string liquidityGoalAddress =
            from.Equals(liquidity.UnstableTokenId, StringComparison.OrdinalIgnoreCase)
                ? liquidity.PairedTokenId
                : liquidity.UnstableTokenId;
        string liquidityStartAddress =
            to.Equals(liquidity.UnstableTokenId, StringComparison.OrdinalIgnoreCase)
                ? liquidity.PairedTokenId
                : liquidity.UnstableTokenId;
        List<string> path = new() { from };
        if (!liquidityStartAddress.Equals(from, StringComparison.OrdinalIgnoreCase))
        {
            path.Add(liquidityStartAddress);
        }
        if (!liquidityGoalAddress.Equals(to, StringComparison.OrdinalIgnoreCase))
        {
            path.Add(liquidityGoalAddress);
        }
        path.Add(to);
        return path.ToArray();
    }
}