using Nethereum.ABI.FunctionEncoding.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VoteExplorer.Models
{
    [FunctionOutput]
    public class Voter
    {
        [Parameter("uint256", 1)]
        public long indexVoter { get; set; }

        [Parameter("string", 2)]
        public string voterId { get; set; }

        [Parameter("string", 3)]
        public string voteSessionId { get; set; }

        [Parameter("string", 4)]
        public string voteAnswers { get; set; }

        [Parameter("uint", 5)]
        public int blockNumber { get; set; }

        [Parameter("uint256", 6)]
        public long balance { get; set; }

    }
}
