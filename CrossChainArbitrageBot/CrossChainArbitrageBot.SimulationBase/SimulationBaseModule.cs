using Agents.Net;
using Autofac;
using CrossChainArbitrageBot.SimulationBase.Agents;

namespace CrossChainArbitrageBot.SimulationBase;

public class SimulationBaseModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<DataSimulator>().As<Agent>().InstancePerLifetimeScope();
    }
}