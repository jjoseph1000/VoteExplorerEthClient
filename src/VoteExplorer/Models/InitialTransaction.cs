using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VoteExplorer.Models
{
    public class InitialTransaction
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }

        [BsonIgnoreIfNull]
        public string controlNumber { get; set; }

        [BsonIgnoreIfNull]
        public string meetingId { get; set; }

        [BsonIgnoreIfNull]
        public string quid { get; set; }

        [BsonIgnoreIfNull]
        public string transactionId { get; set; }

        [BsonIgnoreIfNull]
        public string publicAddress { get; set; }

        [BsonIgnoreIfNull]
        public string amount { get; set; }


    }
}
