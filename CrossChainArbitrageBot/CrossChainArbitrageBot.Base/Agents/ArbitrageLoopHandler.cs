using System.Configuration;
using Agents.Net;
using ConcurrentCollections;
using CrossChainArbitrageBot.Base.Messages;
using CrossChainArbitrageBot.Base.Models;

namespace CrossChainArbitrageBot.Base.Agents;

[Consumes(typeof(LoopStarted))]
[Consumes(typeof(DataUpdated))]
[Consumes(typeof(TransactionFinished))]
public class ArbitrageLoopHandler : Agent
{
    private DataUpdated? latestData;
    private SpreadDataUpdated? latestSpreadData;
    private DataUpdate? bscUpdate;
    private DataUpdate? avalancheUpdate;
    private InternalLoopState loopState;
    private readonly double minimalProfit;
    private readonly ConcurrentHashSet<TransactionStarted> waitingTransactions = new();

    public ArbitrageLoopHandler(IMessageBoard messageBoard) : base(messageBoard)
    {
        loopState = new InternalLoopState(LoopState.Stopped, LoopKind.None);
        minimalProfit = double.Parse(ConfigurationManager.AppSettings["MinimalProfit"] ??
                                     throw new ConfigurationErrorsException("MinimalProfit not configured."));
    }

    protected override void ExecuteCore(Message messageData)
    {
        if (messageData.TryGet(out DataUpdated updated))
        {
            latestData = updated;
            latestSpreadData = messageData.Get<SpreadDataUpdated>();
            avalancheUpdate = latestData.Updates.First(u => u.BlockchainName == BlockchainName.Avalanche);
            bscUpdate = latestData.Updates.First(u => u.BlockchainName == BlockchainName.Bsc);
            return;
        }
        if (messageData.TryGet(out TransactionExecuted executed))
        {
            if (!executed.MessageDomain.Root.TryGet(out TransactionStarted started) ||
                !waitingTransactions.TryRemove(started))
            {
                return;
            }

            MessageDomain.TerminateDomainsOf(executed);
            if (waitingTransactions.IsEmpty)
            {
                loopState = loopState.PopAction(out Action? nextAction);
                nextAction?.Invoke();
            }
            return;
        }
        
        LoopKind kind = messageData.Get<LoopStarted>().Kind;
        ExecuteLoop(messageData, kind);
    }

    private void ExecuteLoop(Message messageData, LoopKind kind)
    {
        switch (kind)
        {
            case LoopKind.SyncTrade:
                if (CanSyncTrade())
                {
                    ChangeLoopState(new InternalLoopState(LoopState.Running, LoopKind.SyncTrade), messageData);
                    SyncTrade(messageData,
                              () =>
                              {
                                  ChangeLoopState(new InternalLoopState(LoopState.Stopped, LoopKind.None), messageData);
                              });
                }

                break;
            case LoopKind.Single:
                if (CanLoop())
                {
                    ChangeLoopState(new InternalLoopState(LoopState.Running, LoopKind.Single), messageData);
                    Loop(messageData);
                }

                break;
            case LoopKind.Auto:
                if (CanAutoLoop())
                {
                    AutoLoop(messageData);
                }

                break;
            case LoopKind.None:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void AutoLoop(Message messageData)
    {
        throw new NotImplementedException();
    }

    private void Loop(Message messageData)
    {
        throw new NotImplementedException();
    }

    private void SyncTrade(Message messageData, Action nextAction)
    {
        TransactionStarted buy = new(messageData,
                                     OptimalVolume / BuySide.StableAmount,
                                     BuySide.BlockchainName,
                                     TransactionType.StableToUnstable);
        TransactionStarted sell = new(messageData,
                                      OptimalVolume / SellSide.UnstablePrice / SellSide.UnstableAmount,
                                      SellSide.BlockchainName,
                                      TransactionType.UnstableToStable);
        MessageDomain.CreateNewDomainsFor(new []{buy, sell});
        waitingTransactions.Add(buy);
        waitingTransactions.Add(sell);
        loopState = loopState.PushNextAction(nextAction);
        
        OnMessage(new ImportantNotice(messageData, $""));
        OnMessage(buy);
        OnMessage(sell);
    }

    private bool CanAutoLoop()
    {
        return loopState.State != LoopState.Stopped ||
               LoopFundsFound();
    }

    private bool CanLoop()
    {
        return loopState.State == LoopState.Stopped &&
               LoopFundsFound() &&
               MinimalProfitPossible();
    }

    private bool CanSyncTrade()
    {
        return loopState.State == LoopState.Stopped &&
               FundsOnRightChain() &&
               MinimalProfitPossible();
        
        bool FundsOnRightChain()
        {
            return latestSpreadData!.Spread > 0
                       ? avalancheUpdate!.Value.UnstableAmount * avalancheUpdate!.Value.UnstablePrice > 100 &&
                         bscUpdate!.Value.StableAmount > 100
                       : bscUpdate!.Value.UnstableAmount * bscUpdate!.Value.UnstablePrice > 100 &&
                         avalancheUpdate!.Value.StableAmount > 100;
        }
    }

    private bool MinimalProfitPossible()
    {
        return OptimalVolume.CalculateProfit(bscUpdate!.Value.Liquidity,
                                             avalancheUpdate!.Value.Liquidity,
                                             BuySide.BlockchainName == BlockchainName.Bsc)
               >= minimalProfit;
    }

    private double OptimalVolume
    {
        get
        {
            double stableValue = latestData!.Updates.Max(u => u.StableAmount);
            double unstableValue = latestData!.Updates.Max(u => u.UnstableAmount * SellSide.UnstablePrice);
            double volume = new[] { stableValue, unstableValue, latestSpreadData!.MaximumVolumeToTargetSpread / 2 }.Min();
            return volume;
        }
    }

    private DataUpdate BuySide => latestSpreadData!.Spread > 0 ? bscUpdate!.Value : avalancheUpdate!.Value;
    private DataUpdate SellSide => latestSpreadData!.Spread > 0 ? avalancheUpdate!.Value : bscUpdate!.Value;

    private bool LoopFundsFound()
    {
        return latestData!.Updates.Any(u => u.StableAmount >= 100) &&
               latestData!.Updates.Any(u => u.UnstableAmount * u.UnstablePrice >= 100);
    }

    private void ChangeLoopState(InternalLoopState newState, Message message)
    {
        loopState = newState;
        OnMessage(new LoopStateChanged(message, newState.Kind == LoopKind.Auto, newState.State));
    }

    private readonly record struct InternalLoopState(LoopState State, LoopKind Kind, Action? NextAction=null)
    {
        public InternalLoopState PushNextAction(Action nextAction)
        {
            return new InternalLoopState(State, Kind, nextAction);
        }

        public InternalLoopState PopAction(out Action? nextAction)
        {
            nextAction = NextAction;
            return new InternalLoopState(State, Kind);
        }
    }
}