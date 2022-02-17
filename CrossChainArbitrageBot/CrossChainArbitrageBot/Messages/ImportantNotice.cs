using System.Collections.Generic;
using Agents.Net;

namespace CrossChainArbitrageBot.Messages
{
    internal class ImportantNotice : Message
    {
        public ImportantNotice(Message predecessorMessage, string notice) : base(predecessorMessage)
        {
            Notice = notice;
        }

        public ImportantNotice(IEnumerable<Message> predecessorMessages, string notice) : base(predecessorMessages)
        {
            Notice = notice;
        }

        public string Notice { get; }

        protected override string DataToString()
        {
            return Notice;
        }
    }
}
