using Agents.Net;
using Autofac;
using CrossChainArbitrageBot.Simulation.Agents;
using CrossChainArbitrageBot.SimulationBase;

namespace CrossChainArbitrageBot.Simulation;

public class SimulationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<UiBridge>().As<Agent>().InstancePerLifetimeScope();
        builder.RegisterModule<SimulationBaseModule>();
    }
}