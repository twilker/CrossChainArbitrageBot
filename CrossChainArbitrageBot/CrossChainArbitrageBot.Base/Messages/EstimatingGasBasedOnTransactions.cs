using Agents.Net;
using CrossChainArbitrageBot.Base.Models;

namespace CrossChainArbitrageBot.Base.Messages;

public class EstimatingGasBasedOnTransactions : Message
{
    public EstimatingGasBasedOnTransactions(Message predecessorMessage, GasType gasType, BlockchainName blockchainName) : base(predecessorMessage)
    {
        GasType = gasType;
        BlockchainName = blockchainName;
    }

    public EstimatingGasBasedOnTransactions(IEnumerable<Message> predecessorMessages, GasType gasType, BlockchainName blockchainName) : base(predecessorMessages)
    {
        GasType = gasType;
        BlockchainName = blockchainName;
    }
    
    public GasType GasType { get; }
    
    public BlockchainName BlockchainName { get; }

    protected override string DataToString()
    {
        return $"{nameof(GasType)}: {GasType}; {nameof(BlockchainName)}: {BlockchainName}";
    }
}

public enum GasType
{
    Trade,
    Bridge,
    BridgeFee
}