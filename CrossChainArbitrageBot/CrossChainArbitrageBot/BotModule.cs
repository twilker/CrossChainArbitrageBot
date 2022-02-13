using Agents.Net;
using Autofac;

namespace CrossChainArbitrageBot
{
    public class BotModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<MessageBoard>().As<IMessageBoard>().InstancePerLifetimeScope();
            builder.RegisterType<MainWindow>().AsSelf().InstancePerLifetimeScope();
        }
    }
}
