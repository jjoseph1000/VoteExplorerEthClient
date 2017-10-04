using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VoteExplorer.Models
{
    [BsonIgnoreExtraElements]
    public class Meeting_SHOAccount
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }

        [BsonIgnoreIfNull]
        public string meetingId { get; set; }

        [BsonIgnoreIfNull]
        public string controlNumber { get; set; }
    }
}
