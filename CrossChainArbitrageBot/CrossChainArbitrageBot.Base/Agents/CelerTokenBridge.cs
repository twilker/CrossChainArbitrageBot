using System.Configuration;
using System.Numerics;
using System.Runtime.ExceptionServices;
using Agents.Net;
using CrossChainArbitrageBot.Base.Messages;
using CrossChainArbitrageBot.Base.Models;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Newtonsoft.Json.Linq;

namespace CrossChainArbitrageBot.Base.Agents;

[Consumes(typeof(TokenBridging))]
[Consumes(typeof(TransactionExecuted))]
public class CelerTokenBridge : Agent
{    
    public CelerTokenBridge(IMessageBoard messageBoard, string name = null) : base(messageBoard, name)
    {
    }

    protected override void ExecuteCore(Message messageData)
    {
        if(messageData.TryGet(out TokenBridging bridging))
        {
            string contractAddress = bridging.SourceChain switch
            {
                BlockchainName.Bsc => ConfigurationManager.AppSettings["BscCelerBridgeAddress"] ?? throw new ConfigurationErrorsException("BscCelerBridgeAddress not defined."),
                BlockchainName.Avalanche => ConfigurationManager.AppSettings["AvalancheCelerBridgeAddress"] ?? throw new ConfigurationErrorsException("AvalancheCelerBridgeAddress not defined."),
                _ => throw new InvalidOperationException("Not implemented.")
            };
            string token = bridging.BridgedSourceToken;
            BigInteger targetChainId = bridging.SourceChain switch
            {
                BlockchainName.Bsc => int.Parse(ConfigurationManager.AppSettings["AvalancheId"] ?? throw new ConfigurationErrorsException("AvalancheId not defined.")),
                BlockchainName.Avalanche => int.Parse(ConfigurationManager.AppSettings["BscId"] ?? throw new ConfigurationErrorsException("BscId not defined.")),
                _ => throw new InvalidOperationException("Not implemented.")
            };


            DateTimeOffset dateTimeOffset = new DateTimeOffset(DateTime.Now);
            long unixDateTime = dateTimeOffset.ToUnixTimeMilliseconds();

            BigInteger amount = Web3.Convert.ToWei(bridging.Amount.RoundedAmount(), bridging.Decimals);

            //last is maxSlippage - roughly 1%
            object[] parameters = {
                bridging.TargetWallet,
                token,
                amount,
                targetChainId,
                unixDateTime,
                93702
            };

            MessageDomain.CreateNewDomainsFor(messageData);
            OnMessage(new TransactionExecuting(messageData, bridging.SourceChain,
                                               "Celer", contractAddress, 
                                               "send",
                                               new HexBigInteger(0), parameters));
        }
        else if (messageData.TryGet(out TransactionExecuted executed) &&
                 messageData.MessageDomain.Root.TryGet(out bridging))
        {
            MessageDomain.TerminateDomainsOf(messageData);
            OnMessage(new TokenBridged(messageData, executed.Success, 
                                       bridging.Amount,
                                       bridging.SourceChain switch
                                       {
                                           BlockchainName.Bsc => BlockchainName.Avalanche,
                                           BlockchainName.Avalanche => BlockchainName.Bsc,
                                           _ => throw new InvalidOperationException("Not implemented.")
                                       }, bridging.BridgedSourceToken,
                          bridging.TokenType));
        }
    }
}