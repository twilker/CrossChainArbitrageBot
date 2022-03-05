using System.Configuration;
using Agents.Net;
using Autofac;
using CrossChainArbitrageBot.Base.Messages;
using CrossChainArbitrageBot.Base.Models;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Serilog;

namespace CrossChainArbitrageBot.SpreadWatcher;

public class App : IDisposable
{    
    private readonly IContainer? container;

    public App()
    {
        IMessageBoard messageBoard;

        //Create container
        ContainerBuilder builder = new();
        builder.RegisterModule(new WatcherModule());
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

        LoadChainsAndAbis(messageBoard);
    }

    private void LoadChainsAndAbis(IMessageBoard messageBoard)
    {
        Web3 bscConnector = new(url: ConfigurationManager.AppSettings["BscHttpApi"],
                                account: new Account(ConfigurationManager.AppSettings["WalletPrivateKey"], 56));
        bscConnector.TransactionManager.UseLegacyAsDefault = true;
        int gasPrice = int.Parse(ConfigurationManager.AppSettings["BscGasPrice"]
                                ?? throw new ConfigurationErrorsException("BscGasPrice not configured."));
        bscConnector.TransactionManager.DefaultGasPrice = Web3.Convert.ToWei(gasPrice, fromUnit: UnitConversion.EthUnit.Gwei);
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
        gasPrice = int.Parse(ConfigurationManager.AppSettings["AvalancheGasPrice"]
                             ?? throw new ConfigurationErrorsException("AvalancheGasPrice not configured."));
        avaxConnector.TransactionManager.DefaultGasPrice = Web3.Convert.ToWei(gasPrice, fromUnit: UnitConversion.EthUnit.Gwei);
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

    public void Dispose()
    {
        container?.Dispose();
    }
}