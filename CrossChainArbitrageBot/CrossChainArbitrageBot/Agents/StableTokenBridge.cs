using System;
using System.Configuration;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Agents.Net;
using CrossChainArbitrageBot.Messages;
using CrossChainArbitrageBot.Models;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;

namespace CrossChainArbitrageBot.Agents
{
    [Consumes(typeof(StableTokenBridging))]
    [Consumes(typeof(BlockchainConnected))]
    internal class StableTokenBridge : Agent
    {
        private readonly MessageCollector<BlockchainConnected, StableTokenBridging> collector;

        public StableTokenBridge(IMessageBoard messageBoard) : base(messageBoard)
        {
            collector = new MessageCollector<BlockchainConnected, StableTokenBridging>(OnMessage);
        }

        private void OnMessage(MessageCollection<BlockchainConnected, StableTokenBridging> set)
        {
            set.MarkAsConsumed(set.Message2);

            try
            {
                BlockchainConnection connection = set.Message1.Connections.First(c => c.BlockchainName == set.Message2.SourceChain);
                string contractAdress = set.Message2.SourceChain switch
                {
                    BlockchainName.Bsc => ConfigurationManager.AppSettings["BscAnySwapRouterAddress"] ?? throw new ConfigurationErrorsException("BscAnySwapRouterAddress not defined."),
                    BlockchainName.Avalanche => ConfigurationManager.AppSettings["AvalancheAnySwapRouterAddress"] ?? throw new ConfigurationErrorsException("BscAnySwapRouterAddress not defined."),
                    _ => throw new InvalidOperationException("Not implemented.")
                };
                string token = set.Message2.SourceChain switch
                {
                    BlockchainName.Bsc => ConfigurationManager.AppSettings["BscAnySwapStableToken"] ?? throw new ConfigurationErrorsException("BscAnySwapRouterAddress not defined."),
                    BlockchainName.Avalanche => ConfigurationManager.AppSettings["AvalancheAnySwapStableToken"] ?? throw new ConfigurationErrorsException("BscAnySwapRouterAddress not defined."),
                    _ => throw new InvalidOperationException("Not implemented.")
                };
                BigInteger targetChainId = set.Message2.SourceChain switch
                {
                    BlockchainName.Bsc => 43114,
                    BlockchainName.Avalanche => 56,
                    _ => throw new InvalidOperationException("Not implemented.")
                };
                Contract anySwapContract = connection.Connection.Eth.GetContract(connection.Abis["AnySwap"], contractAdress);

                Function bridgeFunction = anySwapContract.GetFunction("anySwapOutUnderlying");

                HexBigInteger gas = new(300000);
                BigInteger amount = Web3.Convert.ToWei(Math.Floor(set.Message2.Amount * Math.Pow(10, 12)) / Math.Pow(10, 12));

                object[] parameters = {
                token,
                connection.Connection.TransactionManager.Account.Address,
                amount,
                targetChainId
                };

                Task<string>? bridgeCall = bridgeFunction.SendTransactionAsync(connection.Connection.Eth.TransactionManager.Account.Address,
                                                                               gas, new HexBigInteger(0), parameters);
                bridgeCall.Wait();
                Task<TransactionReceipt>? receipt = connection.Connection.TransactionManager.TransactionReceiptService.PollForReceiptAsync(bridgeCall.Result, new CancellationTokenSource(TimeSpan.FromMinutes(2)));
                receipt.Wait();
                OnMessage(new ImportantNotice(set, $"[BRIDGE] TX ID: {bridgeCall.Result} receipt: {receipt.Result}"));
                OnMessage(new StableTokenBridged(set, receipt.Result.Status.Value == 1, set.Message2.Amount, set.Message2.OriginalTargetAmount,
                                                 set.Message2.SourceChain switch
                                                 {
                                                     BlockchainName.Bsc => BlockchainName.Avalanche,
                                                     BlockchainName.Avalanche => BlockchainName.Bsc,
                                                     _ => throw new InvalidOperationException("Not implemented.")
                                                 }));
            }
            catch (Exception e)
            {
                OnMessage(new ImportantNotice(set, $"Error bridging {e}"));

                OnMessage(new StableTokenBridged(set, false, set.Message2.Amount, set.Message2.OriginalTargetAmount,
                                                 set.Message2.SourceChain switch
                                                 {
                                                     BlockchainName.Bsc => BlockchainName.Avalanche,
                                                     BlockchainName.Avalanche => BlockchainName.Bsc,
                                                     _ => throw new InvalidOperationException("Not implemented.")
                                                 }));
            }
        }

        protected override void ExecuteCore(Message messageData)
        {
            collector.Push(messageData);
        }
    }
}
