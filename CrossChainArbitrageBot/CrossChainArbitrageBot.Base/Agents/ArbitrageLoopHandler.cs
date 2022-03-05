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
                loopState = loopState.PopAction(out Action<Message, bool>? nextAction);
                nextAction?.Invoke(messageData, finished.Result == TransactionResult.Success && loopState.Success);
            }
            else if (finished.Result != TransactionResult.Success)
            {
                loopState = loopState.SetSuccess(false);
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
                              (message, _) =>
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
                         (message, _) =>
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

    private int splitAssets = 0; 

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

        if (FundsInSingleAsset(out bool fundsInStable, out DataUpdate side))
        {
            if (Interlocked.Exchange(ref splitAssets, 1) != 0)
            {
                return;
            }
            double amount = fundsInStable
                                ? OptimalBuyVolume / side.StableAmount
                                : OptimalBuyVolume / (side.UnstableAmount * side.UnstablePrice);
            TransactionStarted started = new(messageData,
                                             amount, side.BlockchainName,
                                             fundsInStable
                                                 ? TransactionType.StableToUnstable
                                                 : TransactionType.UnstableToStable);
            waitingTransactions.Add(started);
            loopState = loopState.PushNextAction((m, _) =>
            {
                splitAssets = 0;
                CheckIdleAutoLoop(m);
            });
            OnMessages(new []{started});
            return;
        }

        if (!MinimalProfitPossible())
        {
            return;
        }

        ChangeLoopState(new InternalLoopState(LoopState.Running, LoopKind.Auto), messageData);
        Loop(messageData, (m, success) =>
        {
            if (success)
            {
                ChangeLoopState(new InternalLoopState(LoopState.Idle, LoopKind.Auto), messageData);
                CheckIdleAutoLoop(m);
            }
            else
            {
                ChangeLoopState(new InternalLoopState(LoopState.Stopped, LoopKind.None), messageData);
                OnMessage(new ImportantNotice(messageData,
                                              "Auto loop stopped, because a transaction failed. Restart after 1 minute.",
                                              NoticeSeverity.Error));
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(60000);
                    ChangeLoopState(new InternalLoopState(LoopState.Idle, LoopKind.Auto), messageData);
                });
            }
        });
    }

    private bool FundsInSingleAsset(out bool isStable, out DataUpdate side)
    {
        if (BuySide.UnstableAmount * BuySide.UnstablePrice < 10 &&
            SellSide.UnstableAmount * SellSide.UnstablePrice < 10)
        {
            isStable = true;
            if (BuySide.StableAmount > OptimalBuyVolume)
            {
                side = BuySide;
                return true;
            }
            if (SellSide.StableAmount > OptimalBuyVolume)
            {
                side = SellSide;
                return true;
            }
        }
        if (BuySide.StableAmount < 10 &&
            SellSide.StableAmount < 10)
        {
            isStable = false;
            if (BuySide.UnstableAmount * BuySide.UnstablePrice > OptimalBuyVolume)
            {
                side = BuySide;
                return true;
            }
            if (SellSide.UnstableAmount * SellSide.UnstablePrice > OptimalBuyVolume)
            {
                side = SellSide;
                return true;
            }
        }

        isStable = default;
        side = default;
        return false;
    }

    private void Loop(Message messageData, Action<Message, bool> nextAction)
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
        SyncTrade(messageData, (m, result) =>
        {
            if (!result)
            {
                nextAction(m, result);
            }
            TransactionStarted[] bridges = {
                new(m, 1,
                    buySide,
                    TransactionType.BridgeUnstable),
                new(m, 1,
                    sellSide,
                    TransactionType.BridgeStable),
            };
            
            loopState = loopState.PushNextAction((futureMessage, success) =>
            {
                if (!success)
                {
                    nextAction(m, success);
                    return;
                }
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
                nextAction(futureMessage, true);
            });
            foreach (TransactionStarted bridge in bridges)
            {
                waitingTransactions.Add(bridge);
            }

            OnMessages(bridges);
        });
    }

    private bool PrepareLoop(Message messageData, Action<Message, bool> nextAction)
    {
        OnMessage(new ImportantNotice(messageData, "Preparing loop (check native balance + bridge)"));
        List<TransactionStarted> preparationTransactions = new();

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
            SendPreparationTransactions(nextAction, preparationTransactions);
            return true;
        }
        
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
            SendPreparationTransactions(nextAction, preparationTransactions);
            return true;
        }

        return false;
    }

    private void SendPreparationTransactions(Action<Message, bool> nextAction, List<TransactionStarted> preparationTransactions)
    {
        loopState = loopState.PushNextAction((m, result) =>
        {
            if (result)
            {
                Loop(m, nextAction);
            }
            else
            {
                nextAction(m, result);
            }
        });
        foreach (TransactionStarted preparationTransaction in preparationTransactions)
        {
            waitingTransactions.Add(preparationTransaction);
        }

        OnMessages(preparationTransactions);
    }

    private void SyncTrade(Message messageData, Action<Message, bool> nextAction)
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
               loopState.State == LoopState.Stopped;
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

    private readonly record struct InternalLoopState(LoopState State, LoopKind Kind, Action<Message, bool>? NextAction=null, 
                                                     bool AutoLoopCancelRequested = false, bool Success = true)
    {
        public InternalLoopState PushNextAction(Action<Message, bool> nextAction)
        {
            return new InternalLoopState(State, Kind, nextAction, AutoLoopCancelRequested, Success);
        }

        public InternalLoopState PopAction(out Action<Message, bool>? nextAction)
        {
            nextAction = NextAction;
            return new InternalLoopState(State, Kind, AutoLoopCancelRequested:AutoLoopCancelRequested, Success:Success);
        }

        public InternalLoopState RequestAutoLoopCancel()
        {
            return new InternalLoopState(State, Kind, NextAction, true, Success);
        }

        public InternalLoopState SetSuccess(bool success)
        {
            return new InternalLoopState(State, Kind, NextAction, AutoLoopCancelRequested, success);
        }
    }
}