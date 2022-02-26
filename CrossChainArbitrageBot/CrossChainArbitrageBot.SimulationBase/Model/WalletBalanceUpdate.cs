using CrossChainArbitrageBot.Base.Models;

namespace CrossChainArbitrageBot.SimulationBase.Model;

public readonly record struct WalletBalanceUpdate(BlockchainName Chain, TokenType Type, double NewBalance);