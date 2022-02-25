using System.Collections.Generic;
using Agents.Net;
using CrossChainArbitrageBot.Models;

namespace CrossChainArbitrageBot.Messages;

public class DataUpdated : Message
{
    public DataUpdated(Message predecessorMessage, DataUpdate[] updates) : base(predecessorMessage)
    {
        Updates = updates;
    }

    public DataUpdated(IEnumerable<Message> predecessorMessages, DataUpdate[] updates) : base(predecessorMessages)
    {
        Updates = updates;
    }
    
    public DataUpdate[] Updates { get; }

    protected override string DataToString()
    {
        return $"{nameof(Updates)}: {string.Join(", ", Updates)}";
    }
}

public readonly record struct DataUpdate(BlockchainName BlockchainName, double UnstablePrice, double UnstableAmount,
                                         string UnstableSymbol, string UnstableId, int UnstableDecimals, 
                                         Liquidity Liquidity, double StableAmount, string StableSymbol, 
                                         string StableId, int StableDecimals, double NativePrice,
                                         double AccountBalance, string WalletAddress);
                                         
public readonly record struct Liquidity(double TokenAmount, double UsdPaired, string PairedTokenId, string UnstableTokenId);