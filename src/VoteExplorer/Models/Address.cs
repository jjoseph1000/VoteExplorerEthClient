using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace VoteExplorer.Models
{
    public class Address
    {
        public string AddressValue { get; set; }
        public string AddressUrl { get; set; }
        [BsonRepresentation(BsonType.Int64), BsonIgnoreIfNull]
        public long TotalVotes { get; set; }
        public List<Transaction> Transactions { get; set; }

    }
}
