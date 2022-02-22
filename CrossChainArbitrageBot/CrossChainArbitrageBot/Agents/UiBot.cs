using System;
using Agents.Net;
using CrossChainArbitrageBot.Messages;
using CrossChainArbitrageBot.Models;
using CrossChainArbitrageBot.ViewModel;

namespace CrossChainArbitrageBot.Agents
{
    [Consumes(typeof(MainWindowCreated))]
    [Consumes(typeof(DataUpdated))]
    [Consumes(typeof(ImportantNotice))]
    internal class UiBot : Agent
    {
        private MainWindow mainWindow;
        private MainWindowCreated mainWindowCreated;

        public UiBot(IMessageBoard messageBoard) : base(messageBoard)
        {
        }

        protected override void ExecuteCore(Message messageData)
        {
            if(messageData.TryGet(out ImportantNotice importantNotice))
            {
                mainWindow.Dispatcher.Invoke(() =>
                {
                    ((WindowViewModel)mainWindow.DataContext).ImportantNotices.Add(importantNotice.Notice);
                });
                return;
            }
            if (messageData.TryGet(out DataUpdated updated))
            {
                mainWindow.Dispatcher.Invoke(() =>
                {
                    UpdateViewModel(updated.Updates);
                });
                return;
            }
            mainWindowCreated = messageData.Get<MainWindowCreated>();
            mainWindow = mainWindowCreated.MainWindow;
            SubscribeToEvents();
        }

        private void UpdateViewModel(DataUpdate[] updatedUpdates)
        {
            WindowViewModel viewModel = (WindowViewModel)mainWindow.DataContext;
            double bscPrice = 0;
            double avalanchePrice = 0;
            Liquidity bscLiquidity = new Liquidity();
            Liquidity avalancheLiquidity = new Liquidity();
            foreach (DataUpdate dataUpdate in updatedUpdates)
            {
                switch (dataUpdate.BlockchainName)
                {
                    case BlockchainName.Bsc:
                        bscPrice = dataUpdate.UnstablePrice;
                        viewModel.BscStableAmount = dataUpdate.StableAmount;
                        viewModel.BscStableToken = dataUpdate.StableSymbol;
                        viewModel.BscUnstableAmount = dataUpdate.UnstableAmount;
                        viewModel.BscUnstablePrice = dataUpdate.UnstablePrice;
                        viewModel.BscUnstableToken = dataUpdate.UnstableSymbol;
                        viewModel.BscAccountBalance = dataUpdate.AccountBalance;
                        bscLiquidity = dataUpdate.Liquidity;
                        break;
                    case BlockchainName.Avalanche:
                        avalanchePrice = dataUpdate.UnstablePrice;
                        viewModel.AvalancheStableAmount = dataUpdate.StableAmount;
                        viewModel.AvalancheStableToken = dataUpdate.StableSymbol;
                        viewModel.AvalancheUnstableAmount = dataUpdate.UnstableAmount;
                        viewModel.AvalancheUnstablePrice = dataUpdate.UnstablePrice;
                        viewModel.AvalancheUnstableToken = dataUpdate.UnstableSymbol;
                        viewModel.AvalancheAccountBalance = dataUpdate.AccountBalance;
                        avalancheLiquidity = dataUpdate.Liquidity;
                        break;
                    default:
                        throw new InvalidOperationException("Not implemented.");
                }

                viewModel.Spread = (avalanchePrice - bscPrice) / bscPrice * 100;
                viewModel.TargetSpread = Math.Abs(0.5 * viewModel.Spread); //dont know why, but this is the optimum
                CalculateOptimalSpread(bscLiquidity, avalancheLiquidity, viewModel);
            }
        }

        private void CalculateOptimalSpread(Liquidity bscLiquidity, Liquidity avalancheLiquidity, WindowViewModel viewModel)
        {
            double bscConstant = Math.Sqrt(bscLiquidity.TokenAmount * bscLiquidity.UsdPaired);
            double avalancheConstant = Math.Sqrt(avalancheLiquidity.TokenAmount * avalancheLiquidity.UsdPaired);
            double targetSpread = viewModel.TargetSpread / 100;
            double bscChange = (Math.Abs(viewModel.Spread) / 100 - targetSpread) *(avalancheConstant/(avalancheConstant+bscConstant));
            viewModel.MaximumVolumeToTargetSpread = CalculateVolumeSpreadOptimum(bscLiquidity, bscChange)*2;
            viewModel.ProfitByMaximumVolume = SimulateOptimalSellAndBuy(bscLiquidity, avalancheLiquidity, viewModel.MaximumVolumeToTargetSpread/2, viewModel.Spread > 0);
        }

        private static double SimulateOptimalSellAndBuy(Liquidity bscLiquidity, Liquidity avalancheLiquidity, double volume, bool buyOnBsc)
        {
            (double buyTokenAmount, double buyUsdPaired, _) = buyOnBsc ? bscLiquidity : avalancheLiquidity;
            (double sellTokenAmount, double sellUsdPaired, _) = buyOnBsc ? avalancheLiquidity : bscLiquidity;
            double tokenAmount = volume / (buyUsdPaired / buyTokenAmount);
            
            //simulate buy
            double newUsd = buyUsdPaired + volume;
            double newToken = buyTokenAmount * buyUsdPaired / newUsd;
            double tokenReceived = buyTokenAmount - newToken;
            
            //simulate sell
            newToken = sellTokenAmount + tokenAmount;
            newUsd = sellTokenAmount * sellUsdPaired / newToken;
            double soldValue = sellUsdPaired - newUsd;
            double boughtValue = tokenReceived * newUsd / newToken;

            return boughtValue + soldValue - volume - tokenAmount * sellUsdPaired / sellTokenAmount;
        }

        private double CalculateVolumeSpreadOptimum(Liquidity liquidity, double targetSpreadChange)
        {
            double constant = liquidity.TokenAmount * liquidity.UsdPaired;
            double currentPrice = liquidity.UsdPaired / liquidity.TokenAmount;
            double targetPrice = currentPrice * (1 + targetSpreadChange);
            return Math.Sqrt(targetPrice * constant) - liquidity.UsdPaired;
        }

        private void SubscribeToEvents()
        {
            mainWindow.Dispatcher.Invoke(() =>
            {
                WindowViewModel viewModel = new();
                mainWindow.DataContext = viewModel;
                viewModel.TransactionInitiated += OnTransactionInitiated;
            });
        }

        private void UnsubscribedFromEvents()
        {
            ((WindowViewModel)mainWindow.DataContext).TransactionInitiated -= OnTransactionInitiated;
        }

        private void OnTransactionInitiated(object? sender, TransactionEventArgs e)
        {
            OnMessage(new TransactionStarted(mainWindowCreated, (double)e.TransactionAmount / 100, e.Chain, e.Type));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (mainWindow != null)
                {
                    UnsubscribedFromEvents();
                }
            }
        }
    }
}
