using System.Collections.Specialized;
using System.Configuration;
using Agents.Net;
using Autofac;
using CrossChainArbitrageBot.Agents;
using CrossChainArbitrageBot.Base;
using CrossChainArbitrageBot.Base.Agents;
using CrossChainArbitrageBot.Simulation;

namespace CrossChainArbitrageBot;

public class BotModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<UiBridge>().As<Agent>().InstancePerLifetimeScope();
        builder.RegisterType<MainWindow>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterModule<BaseModule>();
        if (ConfigurationManager.GetSection("SimulationConfiguration") is NameValueCollection config &&
            bool.TryParse(config["RunSimulation"], out bool simulationMode) &&
            simulationMode)
        {
            builder.RegisterModule<SimulationModule>();
        }
        else
        {
            builder.RegisterType<BlockchainExecuter>().As<Agent>().InstancePerLifetimeScope();
        }
    }
}