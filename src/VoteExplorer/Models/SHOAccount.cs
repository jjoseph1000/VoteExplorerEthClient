using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VoteExplorer.Models
{
    [BsonIgnoreExtraElements]
    public class SHOAccount
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }

        [BsonIgnoreIfNull]
        public string ControlNumber { get; set; }

        [BsonIgnoreIfNull]
        public long AvailableShares { get; set; }

    }
}
