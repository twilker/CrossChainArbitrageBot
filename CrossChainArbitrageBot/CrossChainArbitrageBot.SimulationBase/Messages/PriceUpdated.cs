using System.Collections.Generic;
using Agents.Net;
using CrossChainArbitrageBot.Base.Models;

namespace CrossChainArbitrageBot.SimulationBase.Messages;

public class PriceUpdated : Message
{
    public PriceUpdated(Message predecessorMessage, double? priceOverride, BlockchainName blockchainName) : base(predecessorMessage)
    {
        PriceOverride = priceOverride;
        BlockchainName = blockchainName;
    }

    public PriceUpdated(IEnumerable<Message> predecessorMessages, double? priceOverride, BlockchainName blockchainName) : base(predecessorMessages)
    {
        PriceOverride = priceOverride;
        BlockchainName = blockchainName;
    }
    
    public double? PriceOverride { get; }
    public BlockchainName BlockchainName { get; }

    protected override string DataToString()
    {
        return $"{nameof(PriceOverride)}: {PriceOverride}; {nameof(BlockchainName)}: {BlockchainName}";
    }
}