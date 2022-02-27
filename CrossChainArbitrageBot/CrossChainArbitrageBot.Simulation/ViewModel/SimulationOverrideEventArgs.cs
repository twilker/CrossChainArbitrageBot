using CrossChainArbitrageBot.Base.Models;
using CrossChainArbitrageBot.SimulationBase.Model;

namespace CrossChainArbitrageBot.Simulation.ViewModel;

public class SimulationOverrideEventArgs
{
    public SimulationOverrideEventArgs(SimulationOverrideValueType type, BlockchainName blockchainName)
    {
        Type = type;
        BlockchainName = blockchainName;
    }

    public SimulationOverrideValueType Type { get; }
    public BlockchainName BlockchainName { get; }
}