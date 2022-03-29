using Agents.Net;
using CrossChainArbitrageBot.Base.Models;

namespace CrossChainArbitrageBot.Base.Messages;

public class GasEstimatedBasedOnTransactions : Message
{
    public GasEstimatedBasedOnTransactions(Message predecessorMessage, double value, GasType gasType, BlockchainName blockchainName) : base(predecessorMessage)
    {
        Value = value;
        GasType = gasType;
        BlockchainName = blockchainName;
    }

    public GasEstimatedBasedOnTransactions(IEnumerable<Message> predecessorMessages, double value, GasType gasType, BlockchainName blockchainName) : base(predecessorMessages)
    {
        Value = value;
        GasType = gasType;
        BlockchainName = blockchainName;
    }
    
    public GasType GasType { get; }
    
    public BlockchainName BlockchainName { get; }
    
    public double Value { get; }

    protected override string DataToString()
    {
        return $"{nameof(Value)}: {Value}; {nameof(GasType)}: {GasType}; {nameof(BlockchainName)}: {BlockchainName}";
    }
}