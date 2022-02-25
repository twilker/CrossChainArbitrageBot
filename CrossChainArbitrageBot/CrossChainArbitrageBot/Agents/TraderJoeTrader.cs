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
public class TraderJoeTrader : Agent
{
    public TraderJoeTrader(IMessageBoard messageBoard) : base(messageBoard)
    {
    }

    protected override void ExecuteCore(Message messageData)
    {
        if (messageData.TryGet(out TradeInitiating trade) &&
            trade.Platform == TradingPlatform.TraderJoe)
        {
            if (trade.ToTokenId.Equals(ConfigurationManager.AppSettings["AvalancheNativeCoinId"], StringComparison.OrdinalIgnoreCase))
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
                 trade.Platform == TradingPlatform.TraderJoe)
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

        string contractAddress = ConfigurationManager.AppSettings["TraderJoeRouterAddress"]
                                 ?? throw new ConfigurationErrorsException(
                                     "TraderJoeRouterAddress not configured.");

        MessageDomain.CreateNewDomainsFor(messageData);
        OnMessage(new TransactionExecuting(messageData, BlockchainName.Avalanche,
                                           "TraderJoe",
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

        string contractAddress = ConfigurationManager.AppSettings["TraderJoeRouterAddress"]
                                 ?? throw new ConfigurationErrorsException(
                                     "TraderJoeRouterAddress not configured.");

        MessageDomain.CreateNewDomainsFor(messageData);
        OnMessage(new TransactionExecuting(messageData, BlockchainName.Avalanche,
                                           "TraderJoe",
                                           contractAddress,
                                           "swapExactTokensForETH",
                                           new HexBigInteger(0),
                                           parameters));
    }
}