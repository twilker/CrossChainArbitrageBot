using System.Collections.Generic;
using Agents.Net;
using CrossChainArbitrageBot.Base.Models;

namespace CrossChainArbitrageBot.SimulationBase.Messages;

public class LiquidityOffsetUpdated : Message
{
    public LiquidityOffsetUpdated(Message predecessorMessage, LiquidityOffset offset, BlockchainName blockchainName) : base(predecessorMessage)
    {
        Offset = offset;
        BlockchainName = blockchainName;
    }

    public LiquidityOffsetUpdated(IEnumerable<Message> predecessorMessages, LiquidityOffset offset, BlockchainName blockchainName) : base(predecessorMessages)
    {
        Offset = offset;
        BlockchainName = blockchainName;
    }
    
    public LiquidityOffset Offset { get; }
    public BlockchainName BlockchainName { get; }

    protected override string DataToString()
    {
        return $"{nameof(Offset)}: {Offset}, {nameof(BlockchainName)}: {BlockchainName}";
    }
}

public readonly record struct LiquidityOffset(double TokenOffset, double UsdOffset);