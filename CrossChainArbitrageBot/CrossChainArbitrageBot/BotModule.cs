using Agents.Net;
using Autofac;
using CrossChainArbitrageBot.Agents;

namespace CrossChainArbitrageBot
{
    public class BotModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DataCrawler>().As<Agent>().InstancePerLifetimeScope();
            builder.RegisterType<UiBot>().As<Agent>().InstancePerLifetimeScope();
            builder.RegisterType<PancakeSwapTrader>().As<Agent>().InstancePerLifetimeScope();
            builder.RegisterType<ArbitrageBot>().As<Agent>().InstancePerLifetimeScope();
            builder.RegisterType<MessageBoard>().As<IMessageBoard>().InstancePerLifetimeScope();
            builder.RegisterType<MainWindow>().AsSelf().InstancePerLifetimeScope();
        }
    }
}
