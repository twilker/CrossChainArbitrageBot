using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Agents.Net;
using CrossChainArbitrageBot.Base.Messages;
using CrossChainArbitrageBot.Base.Models;
using CrossChainArbitrageBot.SimulationBase.Messages;

namespace CrossChainArbitrageBot.SimulationBase.Agents;

[Intercepts(typeof(DataUpdated))]
public class DataSimulator : InterceptorAgent
{
    private readonly ConcurrentDictionary<BlockchainName, Wallet> wallets = new();
    public DataSimulator(IMessageBoard messageBoard) : base(messageBoard)
    {
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