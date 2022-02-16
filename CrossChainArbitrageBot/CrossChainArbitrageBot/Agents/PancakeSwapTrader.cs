using Agents.Net;
using CrossChainArbitrageBot.Messages;
using CrossChainArbitrageBot.Models;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CrossChainArbitrageBot.Agents
{
    [Consumes(typeof(BlockchainConnected))]
    [Consumes(typeof(PancakeSwapTradeInitiating))]
    internal class PancakeSwapTrader : Agent
    {
        private readonly MessageCollector<BlockchainConnected, PancakeSwapTradeInitiating> collector;

        public PancakeSwapTrader(IMessageBoard messageBoard) : base(messageBoard)
        {
            collector = new MessageCollector<BlockchainConnected, PancakeSwapTradeInitiating>(OnCollected);
        }

        private void OnCollected(MessageCollection<BlockchainConnected, PancakeSwapTradeInitiating> set)
        {
            set.MarkAsConsumed(set.Message2);

            BlockchainConnection connection = set.Message1.Connections.First((c) => c.BlockchainName == Models.BlockchainName.Bsc);
            Contract pancakeContract = connection.Connection.Eth.GetContract(connection.Abis["Pancake"], ConfigurationManager.AppSettings["PancakeswapRouterAddress"]);

            try
            {
                var sellFunction = pancakeContract.GetFunction("swapExactTokensForTokens");

                var gas = new HexBigInteger(300000);
                var amount = new HexBigInteger(Web3.Convert.ToWei(set.Message2.Amount));

                object[] parameters = new object[]
                {
                    amount.Value,
                    0,
                    new string[] { set.Message2.FromTokenId, set.Message2.ToTokenId },
                    connection.Connection.Eth.TransactionManager.Account.Address,
                    (DateTime.UtcNow.Ticks + 10000)
                };
                var buyCall = sellFunction.SendTransactionAsync(connection.Connection.Eth.TransactionManager.Account.Address, 
                                                                gas, amount, parameters);
                buyCall.Wait();
                var reciept = connection.Connection.TransactionManager.TransactionReceiptService.PollForReceiptAsync(buyCall.Result, new CancellationTokenSource(TimeSpan.FromMinutes(2)));
                reciept.Wait();
                Log.Information($"[TRADE] TX ID: {buyCall.Result} Reciept: {reciept}");

                var swapEventList = reciept.Result.DecodeAllEvents<SwapEvent>().Where(t => t.Event != null)
                    .Select(t => t.Event).ToList();
                var swapEvent = swapEventList.FirstOrDefault();
                OnMessage(new PancakeSwapTradeCompleted(set, swapEvent != null));
            }
            catch (Exception e)
            {
                Log.Error($"Error trading {e}", e);

                OnMessage(new PancakeSwapTradeCompleted(set, false));
            }
        }

        protected override void ExecuteCore(Message messageData)
        {
            collector.Push(messageData);
        }
    }
}
