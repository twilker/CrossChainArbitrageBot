using Agents.Net;
using CrossChainArbitrageBot.Base.Messages;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;

namespace CrossChainArbitrageBot.Base.Agents;

[Consumes(typeof(TransactionExecuting))]
[Consumes(typeof(BlockchainConnected))]
public class BlockchainExecuter : Agent
{
    private readonly MessageCollector<BlockchainConnected, TransactionExecuting> collector;
    public BlockchainExecuter(IMessageBoard messageBoard) : base(messageBoard)
    {
        collector = new MessageCollector<BlockchainConnected, TransactionExecuting>(OnMessage);
    }

    private void OnMessage(MessageCollection<BlockchainConnected, TransactionExecuting> set)
    {
        for (int i = 0; i < 3; i++)
        {
            try
            {
                BlockchainConnection connection =
                    set.Message1.Connections.First(c => c.BlockchainName == set.Message2.BlockchainName);
                Contract anySwapContract =
                    connection.Connection.Eth.GetContract(connection.Abis[set.Message2.AbiName], set.Message2.ContractAddress);

                Function bridgeFunction = anySwapContract.GetFunction(set.Message2.FunctionName);

                HexBigInteger gas = new(300000);

                Task<string>? transactionCall = bridgeFunction.SendTransactionAsync(
                    connection.Connection.Eth.TransactionManager.Account.Address,
                    gas, set.Message2.Amount, set.Message2.Parameters);
                transactionCall.Wait();
                Task<TransactionReceipt>? receipt =
                    connection.Connection.TransactionManager.TransactionReceiptService.PollForReceiptAsync(transactionCall.Result);
                receipt.Wait();
                OnMessage(new ImportantNotice(
                              set,
                              $"[{set.Message2.FunctionName}] TX ID: {transactionCall.Result} Success: {receipt.Result.Succeeded()}"));
                if (receipt.Result.Succeeded())
                {
                    OnMessage(new TransactionExecuted(set, true));
                    return;
                }
            }
            catch (Exception e)
            {
                OnMessage(new ImportantNotice(set, $"Error executing {set.Message2.FunctionName} {e}"));
            }
        }
        OnMessage(new TransactionExecuted(set, false));
    }

    protected override void ExecuteCore(Message messageData)
    {
        collector.Push(messageData);
    }
}