using Agents.Net;
using CrossChainArbitrageBot.Base.Models;
using Nethereum.Hex.HexTypes;

namespace CrossChainArbitrageBot.Base.Messages;

public class TransactionExecuting : Message
{
    public TransactionExecuting(Message predecessorMessage, BlockchainName blockchainName, string abiName, string contractAddress, string functionName, HexBigInteger amount, object[] parameters) : base(predecessorMessage)
    {
        BlockchainName = blockchainName;
        AbiName = abiName;
        ContractAddress = contractAddress;
        FunctionName = functionName;
        Amount = amount;
        Parameters = parameters;
    }

    public TransactionExecuting(IEnumerable<Message> predecessorMessages, BlockchainName blockchainName, string abiName, string contractAddress, string functionName, HexBigInteger amount, object[] parameters) : base(predecessorMessages)
    {
        BlockchainName = blockchainName;
        AbiName = abiName;
        ContractAddress = contractAddress;
        FunctionName = functionName;
        Amount = amount;
        Parameters = parameters;
    }

    public BlockchainName BlockchainName { get; }
    public string AbiName { get; }
    public string ContractAddress { get; }
    public string FunctionName { get; }
    public HexBigInteger Amount { get; }
    public object[] Parameters { get; }

    protected override string DataToString()
    {
        return $"{nameof(BlockchainName)}: {BlockchainName}, {nameof(AbiName)}: {AbiName}, {nameof(ContractAddress)}: {ContractAddress}, {nameof(FunctionName)}: {FunctionName}, {nameof(Amount)}: {Amount}, {nameof(Parameters)}: {Parameters}";
    }
}