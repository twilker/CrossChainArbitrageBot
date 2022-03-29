using Agents.Net;
using Autofac;
using CrossChainArbitrageBot.Base.Agents;

namespace CrossChainArbitrageBot.Base;

public class BaseModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<DataCrawler>().As<Agent>().InstancePerLifetimeScope();
        builder.RegisterType<PancakeSwapTrader>().As<Agent>().InstancePerLifetimeScope();
        builder.RegisterType<TransactionGateway>().As<Agent>().InstancePerLifetimeScope();
        builder.RegisterType<TraderJoeTrader>().As<Agent>().InstancePerLifetimeScope();
        builder.RegisterType<CelerTokenBridge>().As<Agent>().InstancePerLifetimeScope();
        builder.RegisterType<DataCalculationEngine>().As<Agent>().InstancePerLifetimeScope();
        builder.RegisterType<ArbitrageLoopHandler>().As<Agent>().InstancePerLifetimeScope();
        builder.RegisterType<DataLogger>().As<Agent>().InstancePerLifetimeScope();
        builder.RegisterType<TelegramBot>().As<Agent>().InstancePerLifetimeScope();
        builder.RegisterType<GasEstimator>().As<Agent>().InstancePerLifetimeScope();
    }
}