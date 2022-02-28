using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Agents.Net;
using CrossChainArbitrageBot.Base.Messages;
using CrossChainArbitrageBot.Base.Models;
using CrossChainArbitrageBot.SimulationBase.Messages;
using CrossChainArbitrageBot.SimulationBase.Model;

namespace CrossChainArbitrageBot.SimulationBase.Agents;

[Intercepts(typeof(DataUpdated))]
[Consumes(typeof(WalletBalanceUpdated))]
[Consumes(typeof(PriceUpdated))]
[Consumes(typeof(LiquidityOffsetUpdated))]
public class DataSimulator : InterceptorAgent
{
    private readonly ConcurrentDictionary<BlockchainName, Wallet> wallets = new();
    private readonly ConcurrentDictionary<BlockchainName, double> prices = new();
    private readonly ConcurrentDictionary<BlockchainName, LiquidityOffset> offsets = new();
    public DataSimulator(IMessageBoard messageBoard) : base(messageBoard)
    {
    }

    protected override void ExecuteCore(Message messageData)
    {
        if (messageData.TryGet(out PriceUpdated priceUpdated))
        {
            ProcessPriceUpdate(priceUpdated);
            return;
        }

        if (messageData.TryGet(out LiquidityOffsetUpdated liquidityOffsetUpdated))
        {
            offsets.AddOrUpdate(liquidityOffsetUpdated.BlockchainName,
                                _ => liquidityOffsetUpdated.Offset,
                                (_, offset) => new LiquidityOffset(
                                    liquidityOffsetUpdated.Offset.TokenOffset + offset.TokenOffset,
                                    liquidityOffsetUpdated.Offset.UsdOffset + offset.UsdOffset));
            return;
        }
        WalletBalanceUpdated updated = messageData.Get<WalletBalanceUpdated>();
        ProcessWalletUpdate(updated);
    }

    private void ProcessPriceUpdate(PriceUpdated priceUpdated)
    {
        if (!priceUpdated.PriceOverride.HasValue)
        {
            prices.TryRemove(priceUpdated.BlockchainName, out _);
        }
        else
        {
            prices.AddOrUpdate(priceUpdated.BlockchainName,
                               _ => priceUpdated.PriceOverride.Value,
                               (_, _) => priceUpdated.PriceOverride.Value);
            offsets.AddOrUpdate(priceUpdated.BlockchainName,
                                _ => new LiquidityOffset(0, 0),
                                (_, _) => new LiquidityOffset(0, 0));
        }
    }

    private void ProcessWalletUpdate(WalletBalanceUpdated updated)
    {
        foreach (WalletBalanceUpdate update in updated.Updates)
        {
            if (wallets.TryGetValue(update.Chain, out Wallet changing))
            {
                switch (update.Type)
                {
                    case TokenType.Stable:
                        wallets.TryUpdate(update.Chain,
                                          new Wallet(changing.UnstableAmount,
                                                     update.NewBalance,
                                                     changing.NativeAmount),
                                          changing);
                        break;
                    case TokenType.Unstable:
                        wallets.TryUpdate(update.Chain,
                                          new Wallet(update.NewBalance,
                                                     changing.StableAmount,
                                                     changing.NativeAmount),
                                          changing);
                        break;
                    case TokenType.Native:
                        wallets.TryUpdate(update.Chain,
                                          new Wallet(changing.UnstableAmount,
                                                     changing.StableAmount,
                                                     update.NewBalance),
                                          changing);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(update.Type), update.Type, "Not implemented.");
                }
            }
        }
    }

    protected override InterceptionAction InterceptCore(Message messageData)
    {
        if (messageData.Is<SimulatedDataUpdated>())
        {
            return InterceptionAction.Continue;
        }
        DataUpdated originalMessage = messageData.Get<DataUpdated>();
        List<DataUpdate> updates = new();
        foreach (DataUpdate dataUpdate in originalMessage.Updates)
        {
            ManipulateUpdate(dataUpdate);
        }

        SimulatedDataUpdated message = SimulatedDataUpdated.Decorate(new DataUpdated(messageData, updates.ToArray()));
        OnMessage(message);
        return InterceptionAction.DoNotPublish;

        void ManipulateUpdate(DataUpdate dataUpdate)
        {
            (double unstableAmount, double stableAmount, double nativeAmount) = wallets.GetOrAdd(
                dataUpdate.BlockchainName, new Wallet(
                    dataUpdate.UnstableAmount, dataUpdate.StableAmount,
                    dataUpdate.AccountBalance));

            Liquidity liquidity;
            double unstablePrice;
            if (prices.ContainsKey(dataUpdate.BlockchainName) &&
                prices.TryGetValue(dataUpdate.BlockchainName, out double price))
            {
                unstablePrice = price;
                double constant = dataUpdate.Liquidity.TokenAmount * dataUpdate.Liquidity.UsdPaired;
                liquidity = new Liquidity(Math.Sqrt(constant / unstablePrice),
                                          Math.Sqrt(constant * unstablePrice),
                                          dataUpdate.Liquidity.PairedTokenId,
                                          dataUpdate.Liquidity.UnstableTokenId);
            }
            else
            {
                liquidity = dataUpdate.Liquidity;
            }

            (double tokenOffset, double usdOffset) = offsets.GetOrAdd(dataUpdate.BlockchainName, _ => new LiquidityOffset(0, 0));
            liquidity = new Liquidity(liquidity.TokenAmount + tokenOffset,
                                      liquidity.UsdPaired + usdOffset,
                                      liquidity.PairedTokenId,
                                      liquidity.UnstableTokenId);
            unstablePrice = liquidity.UsdPaired / liquidity.TokenAmount;

            updates.Add(new DataUpdate(dataUpdate.BlockchainName,
                                       unstablePrice,
                                       unstableAmount,
                                       dataUpdate.UnstableSymbol,
                                       dataUpdate.UnstableId,
                                       dataUpdate.UnstableDecimals,
                                       liquidity,
                                       stableAmount,
                                       dataUpdate.StableSymbol,
                                       dataUpdate.StableId,
                                       dataUpdate.StableDecimals,
                                       dataUpdate.NativePrice,
                                       nativeAmount,
                                       dataUpdate.WalletAddress));
        }
    }

    private readonly record struct Wallet(double UnstableAmount, double StableAmount, double NativeAmount);
}