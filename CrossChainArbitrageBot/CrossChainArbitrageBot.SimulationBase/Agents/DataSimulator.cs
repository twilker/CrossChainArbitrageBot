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
public class DataSimulator : InterceptorAgent
{
    private readonly ConcurrentDictionary<BlockchainName, Wallet> wallets = new();
    public DataSimulator(IMessageBoard messageBoard) : base(messageBoard)
    {
    }

    protected override void ExecuteCore(Message messageData)
    {
        WalletBalanceUpdated updated = messageData.Get<WalletBalanceUpdated>();
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
                        throw new ArgumentOutOfRangeException(nameof(update.Type),update.Type,"Not implemented.");
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
            if (!wallets.ContainsKey(dataUpdate.BlockchainName))
            {
                wallets.TryAdd(dataUpdate.BlockchainName,
                               new Wallet(dataUpdate.UnstableAmount, dataUpdate.StableAmount,
                                          dataUpdate.AccountBalance));
            }

            if (!wallets.TryGetValue(dataUpdate.BlockchainName, out Wallet currentState))
            {
                throw new InvalidOperationException("Simulated wallet was not configured.");
            }

            updates.Add(new DataUpdate(dataUpdate.BlockchainName,
                                       dataUpdate.UnstablePrice,
                                       currentState.UnstableAmount,
                                       dataUpdate.UnstableSymbol,
                                       dataUpdate.UnstableId,
                                       dataUpdate.UnstableDecimals,
                                       dataUpdate.Liquidity,
                                       currentState.StableAmount,
                                       dataUpdate.StableSymbol,
                                       dataUpdate.StableId,
                                       dataUpdate.StableDecimals,
                                       dataUpdate.NativePrice,
                                       currentState.NativeAmount,
                                       dataUpdate.WalletAddress));
        }

        OnMessage(SimulatedDataUpdated.Decorate(new DataUpdated(messageData, updates.ToArray())));
        return InterceptionAction.DoNotPublish;
    }

    private readonly record struct Wallet(double UnstableAmount, double StableAmount, double NativeAmount);
}