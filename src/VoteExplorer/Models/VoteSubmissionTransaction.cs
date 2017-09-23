using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace VoteExplorer.Models
{
    public class VoteSubmissionTransaction
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id
        {
            get; set;
        }

        [BsonIgnoreIfNull]
        public string voteSubmissionid { get; set; }

        [BsonIgnoreIfNull]
        public string transactionid { get; set; }

    }
}
