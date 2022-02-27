using Agents.Net;
using CrossChainArbitrageBot.Base.Models;

namespace CrossChainArbitrageBot.Base.Messages;

public class TokenBridged : Message
{
    public TokenBridged(Message predecessorMessage, bool success, double amountSend, double originalTargetAmount, BlockchainName targetChain, string bridgedTokenSource) : base(predecessorMessage)
    {
        Success = success;
        AmountSend = amountSend;
        OriginalTargetAmount = originalTargetAmount;
        TargetChain = targetChain;
        BridgedTokenSource = bridgedTokenSource;
    }

    public TokenBridged(IEnumerable<Message> predecessorMessages, bool success, double amountSend, double originalTargetAmount, BlockchainName targetChain, string bridgedTokenSource) : base(predecessorMessages)
    {
        Success = success;
        AmountSend = amountSend;
        OriginalTargetAmount = originalTargetAmount;
        TargetChain = targetChain;
        BridgedTokenSource = bridgedTokenSource;
    }

    public bool Success { get; }

    public double AmountSend { get; }

    public double OriginalTargetAmount { get; }

    public BlockchainName TargetChain { get; }
    
    public string BridgedTokenSource { get; }

    protected override string DataToString()
    {
        return $"{nameof(Success)}: {Success}; {nameof(AmountSend)}: {AmountSend}; {nameof(OriginalTargetAmount)}: {OriginalTargetAmount}; {nameof(TargetChain)}: {TargetChain}; {nameof(BridgedTokenSource)}: {BridgedTokenSource}";
    }
}