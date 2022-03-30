using Agents.Net;
using CrossChainArbitrageBot.Base.Messages;
using CrossChainArbitrageBot.Base.Models;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.Web3;

namespace CrossChainArbitrageBot.Base.Agents;

[Consumes(typeof(TransactionExecuting))]
[Consumes(typeof(BlockchainConnected))]
[Consumes(typeof(GasEstimated))]
public class BlockchainExecuter : Agent
{
    private readonly MessageCollector<BlockchainConnected, TransactionExecuting, GasEstimated> collector;
    public BlockchainExecuter(IMessageBoard messageBoard) : base(messageBoard)
    {
        collector = new MessageCollector<BlockchainConnected, TransactionExecuting, GasEstimated>(OnMessage);
    }

    private void OnMessage(MessageCollection<BlockchainConnected, TransactionExecuting, GasEstimated> set)
    {
        set.MarkAsConsumed(set.Message2);
        for (int i = 0; i < 3; i++)
        {
            try
            {
                BlockchainConnection connection =
                    set.Message1.Connections.First(c => c.BlockchainName == set.Message2.BlockchainName);
                Contract contract =
                    connection.Connection.Eth.GetContract(connection.Abis[set.Message2.AbiName], set.Message2.ContractAddress);
                double priorityGasPrice = set.Message2.BlockchainName switch
                {
                    BlockchainName.Bsc => set.Message3.GasEstimation.BnbPriorityGasPrice,
                    BlockchainName.Avalanche => set.Message3.GasEstimation.AvaxPriorityGasPrice,
                    _ => throw new ArgumentOutOfRangeException()
                };

                Function function = contract.GetFunction(set.Message2.FunctionName);

                HexBigInteger gas = new(300000);
                HexBigInteger gasPrice = new(Web3.Convert.ToWei(priorityGasPrice, fromUnit: UnitConversion.EthUnit.Gwei));

                Task<string>? transactionCall = function.SendTransactionAsync(
                    connection.Connection.Eth.TransactionManager.Account.Address,
                    gas, gasPrice, set.Message2.Amount, set.Message2.Parameters);
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