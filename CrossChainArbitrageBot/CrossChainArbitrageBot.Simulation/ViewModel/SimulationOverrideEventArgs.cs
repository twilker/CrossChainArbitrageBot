using CrossChainArbitrageBot.Base.Models;
using CrossChainArbitrageBot.SimulationBase.Model;

namespace CrossChainArbitrageBot.Simulation.ViewModel;

public class SimulationOverrideEventArgs
{
    public SimulationOverrideEventArgs(SimulationOverrideValueType type, BlockchainName blockchainName, double amount)
    {
        Type = type;
        BlockchainName = blockchainName;
        Amount = amount;
    }

    public SimulationOverrideValueType Type { get; }
    public BlockchainName BlockchainName { get; }
    public double Amount { get; }
}