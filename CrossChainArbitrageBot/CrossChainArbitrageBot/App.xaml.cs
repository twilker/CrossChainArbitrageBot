using Agents.Net;
using Autofac;
using CrossChainArbitrageBot.Messages;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Serilog;
using Serilog.Formatting.Compact;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CrossChainArbitrageBot
{
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
            Web3 bscConnector = new Web3(url: ConfigurationManager.AppSettings["BscHttpApi"],
                account: new Account(ConfigurationManager.AppSettings["WalletPrivateKey"]));
            Dictionary<string, string> abis = new Dictionary<string, string>
            {
                {"Erc20", File.ReadAllText("./Abis/Erc20.json") },
                {"Pair", File.ReadAllText("./Abis/Pair.json") },
                {"Pancake", File.ReadAllText("./Abis/Pancake.json") },
            };
            messageBoard.Publish(new BlockchainConnected(BlockchainName.Bsc, bscConnector, abis));
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
}
