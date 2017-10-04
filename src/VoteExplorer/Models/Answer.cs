using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace VoteExplorer.Models
{
    public class Answer
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }
        public string coin { get; set; }
        public string answid { get; set; }
        public string test { get; set; }

        [BsonRepresentation(BsonType.Int64), BsonIgnoreIfNull]
        public long TotalVotes { get; set; }
        [BsonIgnoreIfNull]
        public List<Address> Addresses { get; set; }
    }
}
