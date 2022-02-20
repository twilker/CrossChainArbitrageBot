using Agents.Net;
using Autofac;
using DataLogger.Agents;

namespace DataLogger
{
    public class BotModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DataCrawler>().As<Agent>().InstancePerLifetimeScope();
            builder.RegisterType<PancakeSwapTrader>().As<Agent>().InstancePerLifetimeScope();
            builder.RegisterType<ArbitrageBot>().As<Agent>().InstancePerLifetimeScope();
            builder.RegisterType<TraderJoeTrader>().As<Agent>().InstancePerLifetimeScope();
            builder.RegisterType<StableTokenBridge>().As<Agent>().InstancePerLifetimeScope();
            builder.RegisterType<UnstableTokenBridge>().As<Agent>().InstancePerLifetimeScope();
            builder.RegisterType<BlockchainExecuter>().As<Agent>().InstancePerLifetimeScope();
            builder.RegisterType<Agents.DataLogger>().As<Agent>().InstancePerLifetimeScope();
            builder.RegisterType<MessageBoard>().As<IMessageBoard>().InstancePerLifetimeScope();
        }
    }
}
