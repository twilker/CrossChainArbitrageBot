namespace CrossChainArbitrageBot.Base.Models;

public enum LoopState
{
    Stopped,
    Idle,
    Running
}

public enum LoopKind
{
    None,
    SyncTrade,
    Single,
    Auto
}