using System.Collections.Specialized;
using System.Configuration;
using Agents.Net;
using Autofac;
using CrossChainArbitrageBot.Base;
using CrossChainArbitrageBot.SpreadWatcher.Agents;

namespace CrossChainArbitrageBot;

public class WatcherModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<MessageBoard>().As<IMessageBoard>().InstancePerLifetimeScope();
        builder.RegisterType<TelegramBot>().As<Agent>().InstancePerLifetimeScope();
        builder.RegisterModule<BaseModule>();
    }
}