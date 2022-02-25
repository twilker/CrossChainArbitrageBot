using Agents.Net;
using Autofac;
using CrossChainArbitrageBot.Simulation.Agents;

namespace CrossChainArbitrageBot.Simulation;

public class SimulationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<UiBridge>().As<Agent>().InstancePerLifetimeScope();
    }
}