using Agents.Net;
using CrossChainArbitrageBot.Base.Models;

namespace CrossChainArbitrageBot.Base.Messages;

public class TokenBridging : Message
{
    public TokenBridging(Message predecessorMessage, BlockchainName sourceChain, double amount, double originalTargetAmount, string targetWallet, int decimals, string bridgedSourceToken, TokenType tokenType) : base(predecessorMessage)
    {
        SourceChain = sourceChain;
        Amount = amount;
        OriginalTargetAmount = originalTargetAmount;
        TargetWallet = targetWallet;
        Decimals = decimals;
        BridgedSourceToken = bridgedSourceToken;
        TokenType = tokenType;
    }

    public TokenBridging(IEnumerable<Message> predecessorMessages, BlockchainName sourceChain, double amount, double originalTargetAmount, string targetWallet, int decimals, string bridgedSourceToken, TokenType tokenType) : base(predecessorMessages)
    {
        SourceChain = sourceChain;
        Amount = amount;
        OriginalTargetAmount = originalTargetAmount;
        TargetWallet = targetWallet;
        Decimals = decimals;
        BridgedSourceToken = bridgedSourceToken;
        TokenType = tokenType;
    }

    public BlockchainName SourceChain { get; }
    public double Amount { get; }
    public double OriginalTargetAmount { get; }
    public string TargetWallet { get; }
    public int Decimals { get; }
    public string BridgedSourceToken { get; }
    public TokenType TokenType { get; }

    protected override string DataToString()
    {
        return $"{nameof(SourceChain)}: {SourceChain}; {nameof(Amount)}: {Amount}; {nameof(OriginalTargetAmount)}: {OriginalTargetAmount}; {nameof(BridgedSourceToken)}: {BridgedSourceToken}," +
               $"{nameof(TargetWallet)}: {TargetWallet}, {nameof(Decimals)}: {Decimals}; {nameof(TokenType)}: {TokenType}";
    }
}