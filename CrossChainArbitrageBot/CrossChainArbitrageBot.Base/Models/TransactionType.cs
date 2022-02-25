namespace CrossChainArbitrageBot.Base.Models;

public enum TransactionType
{
    StableToUnstable,
    UnstableToStable,
    BridgeStable,
    BridgeUnstable,
    StableToNative,
    UnstableToNative
}