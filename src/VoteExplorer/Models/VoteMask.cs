using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VoteExplorer.Models
{
    [BsonIgnoreExtraElements]
    public class VoteMask
    {
        [BsonIgnoreIfNull]
        public string voterId { get; set; }

        [BsonIgnoreIfNull]
        public long balance { get; set; }
    }
}
