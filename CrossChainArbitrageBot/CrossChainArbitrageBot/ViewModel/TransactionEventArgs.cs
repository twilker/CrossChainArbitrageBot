using System;
using CrossChainArbitrageBot.Base.Models;

namespace CrossChainArbitrageBot.ViewModel;

public class TransactionEventArgs : EventArgs
{
    public int TransactionAmount { get; }
    public BlockchainName Chain { get; }
    public TransactionType Type { get; }

    public TransactionEventArgs(int transactionAmount, BlockchainName chain, TransactionType type)
    {
        TransactionAmount = transactionAmount;
        Chain = chain;
        Type = type;
    }
}