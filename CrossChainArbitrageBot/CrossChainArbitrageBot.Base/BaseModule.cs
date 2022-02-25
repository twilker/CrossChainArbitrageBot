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
        builder.RegisterType<ArbitrageBot>().As<Agent>().InstancePerLifetimeScope();
        builder.RegisterType<TraderJoeTrader>().As<Agent>().InstancePerLifetimeScope();
        builder.RegisterType<StableTokenBridge>().As<Agent>().InstancePerLifetimeScope();
        builder.RegisterType<CelerTokenBridge>().As<Agent>().InstancePerLifetimeScope();
        builder.RegisterType<BlockchainExecuter>().As<Agent>().InstancePerLifetimeScope();
        builder.RegisterType<MessageBoard>().As<IMessageBoard>().InstancePerLifetimeScope();
    }
}