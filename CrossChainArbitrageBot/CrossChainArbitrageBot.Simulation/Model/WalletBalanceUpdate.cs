using CrossChainArbitrageBot.Base.Models;

namespace CrossChainArbitrageBot.Simulation.Model;

public readonly record struct WalletBalanceUpdate(BlockchainName Chain, TokenType Type, double NewBalance);