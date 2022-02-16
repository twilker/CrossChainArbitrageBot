﻿using Agents.Net;
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

            try
            {
                BlockchainConnection connection = set.Message1.Connections.First((c) => c.BlockchainName == Models.BlockchainName.Bsc);
                Contract pancakeContract = connection.Connection.Eth.GetContract(connection.Abis["Pancake"], ConfigurationManager.AppSettings["PancakeswapRouterAddress"]);

                var sellFunction = pancakeContract.GetFunction("swapExactTokensForTokens");

                var gas = new HexBigInteger(300000);
                BigInteger amount = Web3.Convert.ToWei(Math.Floor(set.Message2.Amount * Math.Pow(10, 17))/ Math.Pow(10, 17));

                object[] parameters = new object[]
                {
                    amount,
                    0,
                    new string[] { set.Message2.FromTokenId, set.Message2.ToTokenId },
                    connection.Connection.Eth.TransactionManager.Account.Address,
                    (DateTime.UtcNow.Ticks + 10000)
                };
                var buyCall = sellFunction.SendTransactionAsync(connection.Connection.Eth.TransactionManager.Account.Address, 
                                                                gas, new HexBigInteger(0), parameters);
                buyCall.Wait();
                var reciept = connection.Connection.TransactionManager.TransactionReceiptService.PollForReceiptAsync(buyCall.Result, new CancellationTokenSource(TimeSpan.FromMinutes(2)));
                reciept.Wait();
                OnMessage(new ImportantNotice(set, $"[TRADE] TX ID: {buyCall.Result} Reciept: {reciept.Result}"));

                var swapEventList = reciept.Result.DecodeAllEvents<SwapEvent>().Where(t => t.Event != null)
                    .Select(t => t.Event).ToList();
                var swapEvent = swapEventList.FirstOrDefault();
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
