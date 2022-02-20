using System.Configuration;
using System.Numerics;
using Agents.Net;
using DataLogger.Messages;
using DataLogger.Models;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;

namespace DataLogger.Agents
{
    [Consumes(typeof(BlockchainConnected))]
    [Consumes(typeof(TradeInitiating))]
    internal class PancakeSwapTrader : Agent
    {
        private readonly MessageCollector<BlockchainConnected, TradeInitiating> collector;

        public PancakeSwapTrader(IMessageBoard messageBoard) : base(messageBoard)
        {
            collector = new MessageCollector<BlockchainConnected, TradeInitiating>(OnCollected);
        }

        private void OnCollected(MessageCollection<BlockchainConnected, TradeInitiating> set)
        {
            set.MarkAsConsumed(set.Message2);
            if (set.Message2.Platform != TradingPlatform.PancakeSwap)
            {
                return;
            }

            try
            {
                BlockchainConnection connection = set.Message1.Connections.First(c => c.BlockchainName == BlockchainName.Bsc);
                Contract pancakeContract = connection.Connection.Eth.GetContract(connection.Abis["Pancake"], ConfigurationManager.AppSettings["PancakeswapRouterAddress"]);

                Function? sellFunction = pancakeContract.GetFunction("swapExactTokensForTokens");

                HexBigInteger gas = new(300000);
                BigInteger amount = Web3.Convert.ToWei(Math.Floor(set.Message2.Amount * Math.Pow(10, 17))/ Math.Pow(10, 17));

                object[] parameters = {
                    amount,
                    0,
                    new[] { set.Message2.FromTokenId, set.Message2.ToTokenId },
                    connection.Connection.Eth.TransactionManager.Account.Address,
                    (DateTime.UtcNow.Ticks + 10000)
                };
                Task<string>? buyCall = sellFunction.SendTransactionAsync(connection.Connection.Eth.TransactionManager.Account.Address, 
                                                                          gas, new HexBigInteger(0), parameters);
                buyCall.Wait();
                Task<TransactionReceipt>? receipt = connection.Connection.TransactionManager.TransactionReceiptService.PollForReceiptAsync(buyCall.Result, new CancellationTokenSource(TimeSpan.FromMinutes(2)));
                receipt.Wait();
                OnMessage(new ImportantNotice(set, $"[TRADE] TX ID: {buyCall.Result} receipt: {receipt.Result}"));

                List<SwapEvent> swapEventList = receipt.Result.DecodeAllEvents<SwapEvent>().Where(t => t.Event != null)
                                                       .Select(t => t.Event).ToList();
                SwapEvent? swapEvent = swapEventList.FirstOrDefault();
                OnMessage(new TradeCompleted(set, swapEvent != null));
            }
            catch (Exception e)
            {
                OnMessage(new ImportantNotice(set, $"Error trading {e}"));

                OnMessage(new TradeCompleted(set, false));
            }
        }

        protected override void ExecuteCore(Message messageData)
        {
            collector.Push(messageData);
        }
    }
}
