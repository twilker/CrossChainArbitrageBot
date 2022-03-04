using Agents.Net;
using CrossChainArbitrageBot.Base.Models;

namespace CrossChainArbitrageBot.Base.Messages;

public class TokenBridged : Message
{
    public TokenBridged(Message predecessorMessage, bool success, double amountSend, BlockchainName targetChain, string bridgedTokenSource, TokenType tokenType) : base(predecessorMessage)
    {
        Success = success;
        AmountSend = amountSend;
        TargetChain = targetChain;
        BridgedTokenSource = bridgedTokenSource;
        TokenType = tokenType;
    }

    public TokenBridged(IEnumerable<Message> predecessorMessages, bool success, double amountSend, BlockchainName targetChain, string bridgedTokenSource, TokenType tokenType) : base(predecessorMessages)
    {
        Success = success;
        AmountSend = amountSend;
        TargetChain = targetChain;
        BridgedTokenSource = bridgedTokenSource;
        TokenType = tokenType;
    }

    public bool Success { get; }

    public double AmountSend { get; }

    public BlockchainName TargetChain { get; }
    
    public string BridgedTokenSource { get; }
    
    public TokenType TokenType { get; }

    protected override string DataToString()
    {
        return $"{nameof(Success)}: {Success}; {nameof(AmountSend)}: {AmountSend}; {nameof(TargetChain)}: {TargetChain}; {nameof(BridgedTokenSource)}: {BridgedTokenSource}; {nameof(TokenType)}: {TokenType}";
    }
}