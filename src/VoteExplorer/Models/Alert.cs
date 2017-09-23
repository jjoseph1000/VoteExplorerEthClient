using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace VoteExplorer.Models
{
    [BsonIgnoreExtraElements]
    public class Alert
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }

        [BsonIgnoreIfNull]
        public string AlertCode { get; set; }

        [BsonIgnoreIfNull]
        public string AlertDescription { get; set; }

        [BsonIgnoreIfNull]
        public string AlertIcon { get; set; }

    }
}
