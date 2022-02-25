using Agents.Net;
using Autofac;
using CrossChainArbitrageBot.Agents;
using CrossChainArbitrageBot.Base;

namespace CrossChainArbitrageBot;

public class BotModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<UiBot>().As<Agent>().InstancePerLifetimeScope();
        builder.RegisterType<MainWindow>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterModule<BaseModule>();
    }
}