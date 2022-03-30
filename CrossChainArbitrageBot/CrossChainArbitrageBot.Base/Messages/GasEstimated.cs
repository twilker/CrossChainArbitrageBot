using Agents.Net;

namespace CrossChainArbitrageBot.Base.Messages;

public class GasEstimated : Message
{
    public GasEstimated(Message predecessorMessage, GasEstimation gasEstimation) : base(predecessorMessage)
    {
        GasEstimation = gasEstimation;
    }

    public GasEstimated(IEnumerable<Message> predecessorMessages, GasEstimation gasEstimation) : base(predecessorMessages)
    {
        GasEstimation = gasEstimation;
    }
    
    public GasEstimation GasEstimation { get; }

    protected override string DataToString()
    {
        return $"{nameof(GasEstimation)}: {GasEstimation}";
    }
}

public readonly record struct GasEstimation(double GasCost, double BnbTradeAmount, 
                                            double BnbSingleBridgeAmount,
                                            double AvaxTradeAmount,
                                            double AvaxSingleBridgeAmount,
                                            double BscBridgeFee,
                                            double AvalancheBridgeFee,
                                            double BnbPriorityGasPrice,
                                            double AvaxPriorityGasPrice);