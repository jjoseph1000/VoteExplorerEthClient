using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace VoteExplorer.Models
{
    [BsonIgnoreExtraElements]
    public class Group
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }

        [BsonIgnoreIfNull]
        public int _Id { get; set; }

        [BsonIgnoreIfNull]
        public string GroupName { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string MeetingId { get; set; }

        [BsonIgnoreIfNull]
        public List<Account> Accounts { get; set; }

    }
}
