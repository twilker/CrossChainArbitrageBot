// See https://aka.ms/new-console-template for more information

using System.Configuration;
using Agents.Net;
using Autofac;
using DataLogger;
using DataLogger.Messages;
using DataLogger.Models;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Serilog;
using Serilog.Formatting.Compact;

IMessageBoard messageBoard;

ConfigureLogging();

//Create container
ContainerBuilder builder = new();
builder.RegisterModule(new BotModule());
using IContainer container = builder.Build();

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

Console.ReadKey();

void LoadChainsAndAbis(IMessageBoard messageBoard)
{
    Web3 bscConnector = new(url: ConfigurationManager.AppSettings["BscHttpApi"],
                            account: new Account(ConfigurationManager.AppSettings["WalletPrivateKey"], 56));
    bscConnector.TransactionManager.UseLegacyAsDefault = true;
    bscConnector.TransactionManager.DefaultGasPrice = Web3.Convert.ToWei(5, fromUnit: UnitConversion.EthUnit.Gwei);
    Dictionary<string, string> bscAbis = new()
    {
        { "Erc20", File.ReadAllText("./Abis/Erc20.json") },
        { "Pair", File.ReadAllText("./Abis/Pair.json") },
        { "Pancake", File.ReadAllText("./Abis/Pancake.json") },
        { "AnySwap", File.ReadAllText("./Abis/AnySwap.json") },
        { "Celer", File.ReadAllText("./Abis/Celer.json") },
    };
    Web3 avaxConnector = new(url: ConfigurationManager.AppSettings["AvalancheHttpApi"],
                             account: new Account(ConfigurationManager.AppSettings["WalletPrivateKey"], 43114));
    avaxConnector.TransactionManager.UseLegacyAsDefault = true;
    avaxConnector.TransactionManager.DefaultGasPrice = Web3.Convert.ToWei(35, fromUnit: UnitConversion.EthUnit.Gwei);
    Dictionary<string, string> avaxAbis = new()
    {
        { "Erc20", File.ReadAllText("./Abis/Erc20.json") },
        { "Pair", File.ReadAllText("./Abis/Pair.json") },
        { "AnySwap", File.ReadAllText("./Abis/AnySwap.json") },
        { "TraderJoe", File.ReadAllText("./Abis/TraderJoe.json") },
        { "Celer", File.ReadAllText("./Abis/Celer.json") },
    };
    messageBoard.Publish(new BlockchainConnected(new BlockchainConnection(BlockchainName.Bsc, bscConnector, bscAbis),
                                                 new BlockchainConnection(
                                                     BlockchainName.Avalanche, avaxConnector, avaxAbis)));
}

void ConfigureLogging()
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