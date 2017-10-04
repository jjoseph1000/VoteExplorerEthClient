using Nethereum.ABI.FunctionEncoding.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VoteExplorer.Models
{
    [FunctionOutput]
    public class QuestionText
    {
        [Parameter("string", 1)]
        public string questionid { get; set; }

        [Parameter("uint", 2)]
        public int row { get; set; }

        [Parameter("bytes32", 3)]
        public byte[] textLine { get; set; }



    }
}
