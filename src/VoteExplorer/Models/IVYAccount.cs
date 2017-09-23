using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VoteExplorer.Models
{
    [BsonIgnoreExtraElements]
    public class IVYAccount
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }

        [BsonIgnoreIfNull]
        public string userName { get; set; }

        [BsonIgnoreIfNull]
        public string passWord { get; set; }

    }
}
