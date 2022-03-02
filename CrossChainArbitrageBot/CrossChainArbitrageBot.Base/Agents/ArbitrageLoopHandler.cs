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
    private readonly double liquidityProviderFee;
    private readonly double bridgeFee;

    private const double MinimalGasPreLoop = 3; 

    public ArbitrageLoopHandler(IMessageBoard messageBoard) : base(messageBoard)
    {
        loopState = new InternalLoopState(LoopState.Stopped, LoopKind.None);
        minimalProfit = double.Parse(ConfigurationManager.AppSettings["MinimalProfit"] ??
                                     throw new ConfigurationErrorsException("MinimalProfit not configured."));
        liquidityProviderFee = double.Parse(ConfigurationManager.AppSettings["LiquidityProviderFee"] ??
                                            throw new ConfigurationErrorsException("LiquidityProviderFee not found."));
        bridgeFee = double.Parse(ConfigurationManager.AppSettings["BridgeCostsForProfitCalculation"] ??
                                 throw new ConfigurationErrorsException("BridgeCostsForProfitCalculation not found."));
    }

    protected override void ExecuteCore(Message messageData)
    {
        if (messageData.TryGet(out DataUpdated updated))
        {
            latestData = updated;
            latestSpreadData = messageData.Get<SpreadDataUpdated>();
            avalancheUpdate = latestData.Updates.First(u => u.BlockchainName == BlockchainName.Avalanche);
            bscUpdate = latestData.Updates.First(u => u.BlockchainName == BlockchainName.Bsc);
            CheckIdleAutoLoop(messageData);
            return;
        }
        if (messageData.TryGet(out TransactionFinished finished))
        {
            if (!finished.MessageDomain.Root.TryGet(out TransactionStarted started) ||
                !waitingTransactions.TryRemove(started))
            {
                return;
            }

            MessageDomain.TerminateDomainsOf(finished);
            if (waitingTransactions.IsEmpty)
            {
                loopState = loopState.PopAction(out Action<Message>? nextAction);
                nextAction?.Invoke(messageData);
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
                              message =>
                              {
                                  ChangeLoopState(new InternalLoopState(LoopState.Stopped, LoopKind.None), message);
                              });
                }

                break;
            case LoopKind.Single:
                if (CanLoop())
                {
                    ChangeLoopState(new InternalLoopState(LoopState.Running, LoopKind.Single), messageData);
                    Loop(messageData,
                         message =>
                         {
                             ChangeLoopState(new InternalLoopState(LoopState.Stopped, LoopKind.None), message);
                         });
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
        if (loopState.Kind == LoopKind.Auto)
        {
            loopState = loopState.RequestAutoLoopCancel();
            return;
        }
        
        ChangeLoopState(new InternalLoopState(LoopState.Idle, LoopKind.Auto), messageData);
        CheckIdleAutoLoop(messageData);
    }

    private void CheckIdleAutoLoop(Message messageData)
    {
        if (loopState.State != LoopState.Idle || loopState.Kind != LoopKind.Auto)
        {
            return;
        }
        
        if (loopState.AutoLoopCancelRequested)
        {
            ChangeLoopState(new InternalLoopState(LoopState.Stopped, LoopKind.None), messageData);
            return;
        }

        if (MinimalProfitPossible())
        {
            ChangeLoopState(new InternalLoopState(LoopState.Running, LoopKind.Auto), messageData);
            Loop(messageData, m =>
            {
                ChangeLoopState(new InternalLoopState(LoopState.Idle, LoopKind.Auto), messageData);
                CheckIdleAutoLoop(m);
            });
        }
    }

    private void Loop(Message messageData, Action<Message> nextAction)
    {
        if (PrepareLoop(messageData, nextAction))
        {
            return;
        }

        BlockchainName buySide = BuySide.BlockchainName;
        BlockchainName sellSide = SellSide.BlockchainName;
        double totalNetWorth = latestData!.Updates.Sum(dataUpdate => dataUpdate.StableAmount +
                                                                    dataUpdate.UnstableAmount *
                                                                    dataUpdate.UnstablePrice);
        SyncTrade(messageData, m =>
        {
            TransactionStarted[] bridges = {
                new(m, 1,
                    buySide,
                    TransactionType.BridgeUnstable),
                new(m, 1,
                    sellSide,
                    TransactionType.BridgeStable),
            };
            
            loopState = loopState.PushNextAction(futureMessage =>
            {
                double afterLoopNetWorth = latestData!.Updates.Sum(dataUpdate => dataUpdate.StableAmount +
                                                                             dataUpdate.UnstableAmount *
                                                                             dataUpdate.UnstablePrice);
                OnMessage(new ImportantNotice(futureMessage, $"Loop executed successfully. Profit - " +
                                                             $"{afterLoopNetWorth - totalNetWorth:F2}$ - " +
                                                             $"{(afterLoopNetWorth - totalNetWorth) / totalNetWorth * 100:F2}%"));
                OnMessage(new LoopCompleted(futureMessage,
                                            afterLoopNetWorth,
                                            afterLoopNetWorth +
                                            latestData!.Updates.Sum(u => u.NativePrice * u.AccountBalance),
                                            latestData!.Updates.Sum(u => u.StableAmount),
                                            latestData!.Updates.Sum(u => u.UnstableAmount),
                                            latestData!.Updates.First(u => u.BlockchainName == BlockchainName.Bsc)
                                                       .AccountBalance,
                                            latestData!.Updates.First(u => u.BlockchainName == BlockchainName.Avalanche)
                                                       .AccountBalance));
                nextAction(futureMessage);
            });
            foreach (TransactionStarted bridge in bridges)
            {
                waitingTransactions.Add(bridge);
            }

            OnMessages(bridges);
        });
    }

    private bool PrepareLoop(Message messageData, Action<Message> nextAction)
    {
        OnMessage(new ImportantNotice(messageData, "Preparing loop (check native balance + bridge)"));
        List<TransactionStarted> preparationTransactions = new();
        if (BuySide.AccountBalance * BuySide.NativePrice < MinimalGasPreLoop)
        {
            preparationTransactions.Add(new TransactionStarted(messageData, 0,
                                                               BuySide.BlockchainName,
                                                               BuySide.StableAmount >
                                                               BuySide.UnstableAmount *
                                                               BuySide.UnstablePrice
                                                                   ? TransactionType.StableToNative
                                                                   : TransactionType.UnstableToNative));
        }

        if (SellSide.AccountBalance * SellSide.NativePrice < MinimalGasPreLoop)
        {
            preparationTransactions.Add(new TransactionStarted(messageData, 0,
                                                               SellSide.BlockchainName,
                                                               SellSide.StableAmount >
                                                               SellSide.UnstableAmount *
                                                               SellSide.UnstablePrice
                                                                   ? TransactionType.StableToNative
                                                                   : TransactionType.UnstableToNative));
        }

        if (preparationTransactions.Any())
        {
            SendPreparationTransactions();
            return true;
        }

        if (BuySide.StableAmount < OptimalBuyVolume &&
            SellSide.StableAmount > bridgeFee * 10)
        {
            preparationTransactions.Add(new TransactionStarted(messageData, 1,
                                                               SellSide.BlockchainName,
                                                               TransactionType.BridgeStable));
        }

        if (BuySide.UnstableAmount * BuySide.UnstablePrice > bridgeFee*10)
        {
            preparationTransactions.Add(new TransactionStarted(messageData, 1,
                                                               BuySide.BlockchainName,
                                                               TransactionType.BridgeUnstable));
        }

        if (preparationTransactions.Any())
        {
            SendPreparationTransactions();
            return true;
        }

        return false;
        
        void SendPreparationTransactions()
        {
            loopState = loopState.PushNextAction(m => Loop(m, nextAction));
            foreach (TransactionStarted preparationTransaction in preparationTransactions)
            {
                waitingTransactions.Add(preparationTransaction);
            }

            OnMessages(preparationTransactions);
        }
    }

    private void SyncTrade(Message messageData, Action<Message> nextAction)
    {
        TransactionStarted buy = new(messageData,
                                     BuyVolume / BuySide.StableAmount,
                                     BuySide.BlockchainName,
                                     TransactionType.StableToUnstable);
        TransactionStarted sell = new(messageData,
                                      1,
                                      SellSide.BlockchainName,
                                      TransactionType.UnstableToStable);
        waitingTransactions.Add(buy);
        waitingTransactions.Add(sell);
        loopState = loopState.PushNextAction(nextAction);

        OnMessage(new ImportantNotice(messageData,
                                      $"Synchronized trade of {BuyVolume:F2} {BuySide.StableSymbol} and " +
                                      $"{SellSide.UnstableAmount:F2} {SellSide.UnstableSymbol}. " +
                                      $"Sell on {SellSide.BlockchainName}. Buy on {BuySide.BlockchainName}"));
        OnMessages(new []{buy, sell});
    }

    private bool CanAutoLoop()
    {
        return loopState.Kind == LoopKind.Auto ||
               loopState.State == LoopState.Stopped &&
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
        return latestSpreadData!.CurrentProfit >= minimalProfit;
    }

    private double BuyVolume => Math.Min(OptimalBuyVolume, BuySide.StableAmount);

    private double OptimalBuyVolume
    {
        get
        {
            double buyVolume =
                BuySide.Liquidity.Constant / (BuySide.Liquidity.TokenAmount - latestSpreadData!.OptimalTokenAmount) -
                BuySide.Liquidity.UsdPaired;
            return buyVolume;
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

    private readonly record struct InternalLoopState(LoopState State, LoopKind Kind, Action<Message>? NextAction=null, bool AutoLoopCancelRequested = false)
    {
        public InternalLoopState PushNextAction(Action<Message> nextAction)
        {
            return new InternalLoopState(State, Kind, nextAction, AutoLoopCancelRequested);
        }

        public InternalLoopState PopAction(out Action<Message>? nextAction)
        {
            nextAction = NextAction;
            return new InternalLoopState(State, Kind, AutoLoopCancelRequested:AutoLoopCancelRequested);
        }

        public InternalLoopState RequestAutoLoopCancel()
        {
            return new InternalLoopState(State, Kind, NextAction, true);
        }
    }
}