using System;
using System.Collections.Generic;
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

namespace CrossChainArbitrageBot.Agents;

[Consumes(typeof(TradeInitiating))]
[Consumes(typeof(TransactionExecuted))]
internal class PancakeSwapTrader : Agent
{
    public PancakeSwapTrader(IMessageBoard messageBoard) : base(messageBoard)
    {
    }

    protected override void ExecuteCore(Message messageData)
    {
        if (messageData.TryGet(out TradeInitiating trade) &&
            trade.Platform == TradingPlatform.PancakeSwap)
        {
            if (trade.ToTokenId.Equals(ConfigurationManager.AppSettings["BscNativeCoinId"], StringComparison.OrdinalIgnoreCase))
            {
                TradeTokenForNative(messageData, trade);
            }
            else
            {
                TradeTokenForToken(messageData, trade);
            }
        }
        else if (messageData.TryGet(out TransactionExecuted executed) &&
                 messageData.MessageDomain.Root.TryGet(out trade) &&
                 trade.Platform == TradingPlatform.PancakeSwap)
        {
            MessageDomain.TerminateDomainsOf(messageData);
            OnMessage(new TradeCompleted(messageData, executed.Success));
        }
    }

    private void TradeTokenForToken(Message messageData, TradeInitiating trade)
    {
        BigInteger amount = Web3.Convert.ToWei(trade.Amount.RoundedAmount(), trade.FromTokenDecimals);

        string liquidityGoalAddress =
            trade.FromTokenId.Equals(trade.LiquidityPair.UnstableTokenId, StringComparison.OrdinalIgnoreCase)
                ? trade.LiquidityPair.PairedTokenId
                : trade.LiquidityPair.UnstableTokenId;
        string[] path = trade.ToTokenId.Equals(liquidityGoalAddress, StringComparison.OrdinalIgnoreCase)
                            ? new[] { trade.FromTokenId, trade.ToTokenId }
                            : new[] { trade.FromTokenId, liquidityGoalAddress, trade.ToTokenId };

        object[] parameters =
        {
            amount,
            0,
            path,
            trade.WalletAddress,
            (DateTime.UtcNow.Ticks + 10000)
        };

        string contractAddress = ConfigurationManager.AppSettings["PancakeswapRouterAddress"]
                                 ?? throw new ConfigurationErrorsException(
                                     "PancakeswapRouterAddress not configured.");

        MessageDomain.CreateNewDomainsFor(messageData);
        OnMessage(new TransactionExecuting(messageData, BlockchainName.Bsc,
                                           "Pancake",
                                           contractAddress,
                                           "swapExactTokensForTokens",
                                           new HexBigInteger(0),
                                           parameters));
    }

    private void TradeTokenForNative(Message messageData, TradeInitiating trade)
    {
        BigInteger amount = Web3.Convert.ToWei(trade.Amount.RoundedAmount(), trade.FromTokenDecimals);

        string[] path = trade.FromTokenId.Equals(trade.LiquidityPair.UnstableTokenId, StringComparison.OrdinalIgnoreCase) &&
                        trade.ToTokenId.Equals(trade.LiquidityPair.PairedTokenId,StringComparison.OrdinalIgnoreCase)
                            ? new[] { trade.FromTokenId, trade.ToTokenId }
                            : new[] { trade.FromTokenId, trade.LiquidityPair.PairedTokenId, trade.ToTokenId };

        object[] parameters =
        {
            amount,
            0,
            path,
            trade.WalletAddress,
            (DateTime.UtcNow.Ticks + 10000)
        };

        string contractAddress = ConfigurationManager.AppSettings["PancakeswapRouterAddress"]
                                 ?? throw new ConfigurationErrorsException(
                                     "PancakeswapRouterAddress not configured.");

        MessageDomain.CreateNewDomainsFor(messageData);
        OnMessage(new TransactionExecuting(messageData, BlockchainName.Bsc,
                                           "Pancake",
                                           contractAddress,
                                           "swapExactTokensForETH",
                                           new HexBigInteger(0),
                                           parameters));
    }
}