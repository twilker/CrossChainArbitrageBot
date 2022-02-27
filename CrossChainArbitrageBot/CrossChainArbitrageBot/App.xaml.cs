using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows;
using Agents.Net;
using Autofac;
using CrossChainArbitrageBot.Base.Messages;
using CrossChainArbitrageBot.Base.Models;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Serilog;
using Serilog.Formatting.Compact;

namespace CrossChainArbitrageBot;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IContainer container;

    protected override void OnExit(ExitEventArgs e)
    {
        container?.Dispose();
        base.OnExit(e);
    }

    private void OnStartup(object sender, StartupEventArgs e)
    {
        IMessageBoard messageBoard;

        ConfigureLogging();

        //Create container
        ContainerBuilder builder = new();
        builder.RegisterModule(new BotModule());
        container = builder.Build();

        //Start agent community
        try
        {
            messageBoard = container.Resolve<IMessageBoard>();
            Agent[] agents = container.Resolve<IEnumerable<Agent>>().ToArray();
            messageBoard.Register(agents);
            messageBoard.Start();
        }
        catch (Exception exception)
        {
            Log.Error(exception, $"Unhandled exception during setup.{Environment.NewLine}{exception}");
            return;
        }

        //Show main window
        MainWindow mainWindow = container.Resolve<MainWindow>();
        mainWindow.Show();

        //Declare MainWindow as Message
        messageBoard.Publish(new MainWindowCreated(mainWindow));

        LoadChainsAndAbis(messageBoard);
    }

    private void LoadChainsAndAbis(IMessageBoard messageBoard)
    {
        Web3 bscConnector = new(url: ConfigurationManager.AppSettings["BscHttpApi"],
                                account: new Account(ConfigurationManager.AppSettings["WalletPrivateKey"], 56));
        bscConnector.TransactionManager.UseLegacyAsDefault = true;
        bscConnector.TransactionManager.DefaultGasPrice = Web3.Convert.ToWei(5, fromUnit: UnitConversion.EthUnit.Gwei);
        Dictionary<string, string> bscAbis = new()
        {
            {"Erc20", File.ReadAllText("./Abis/Erc20.json") },
            {"Pair", File.ReadAllText("./Abis/Pair.json") },
            {"Pancake", File.ReadAllText("./Abis/Pancake.json") },
            {"Celer", File.ReadAllText("./Abis/Celer.json") },
        };
        Web3 avaxConnector = new(url: ConfigurationManager.AppSettings["AvalancheHttpApi"],
                                 account: new Account(ConfigurationManager.AppSettings["WalletPrivateKey"], 43114));
        avaxConnector.TransactionManager.UseLegacyAsDefault = true;
        avaxConnector.TransactionManager.DefaultGasPrice = Web3.Convert.ToWei(35, fromUnit: UnitConversion.EthUnit.Gwei);
        Dictionary<string, string> avaxAbis = new()
        {
            {"Erc20", File.ReadAllText("./Abis/Erc20.json") },
            {"Pair", File.ReadAllText("./Abis/Pair.json") },
            {"TraderJoe", File.ReadAllText("./Abis/TraderJoe.json") },
            {"Celer", File.ReadAllText("./Abis/Celer.json") },
        };
        messageBoard.Publish(new BlockchainConnected(new BlockchainConnection(BlockchainName.Bsc, bscConnector, bscAbis),
                                                     new BlockchainConnection(BlockchainName.Avalanche, avaxConnector, avaxAbis)));
    }

    private void ConfigureLogging()
    {
        if (File.Exists("log.json"))
        {
            File.Delete("log.json");
        }
        Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.Async(l => l.File(new CompactJsonFormatter(), "log.json"))
                    .CreateLogger();
    }
}