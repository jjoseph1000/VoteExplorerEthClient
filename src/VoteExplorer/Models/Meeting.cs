using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace VoteExplorer.Models
{
    [BsonIgnoreExtraElements]
    public class Meeting
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }

        [BsonIgnoreIfNull]
        public int _Id { get; set; }

        [BsonIgnoreIfNull]
        public string KeyValue { get; set; }

        [BsonIgnoreIfNull]
        public string Entity { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string CompanyId { get; set; }

        [BsonIgnoreIfNull]
        public string CompanyName { get; set; }

        [BsonIgnoreIfNull]
        public string TypeofMeeting { get; set; }

        [BsonIgnoreIfNull]
        public int SecurityID { get; set; }

        [BsonIgnoreIfNull]
        public DateTime MeetingDate { get; set; }

        [BsonIgnoreIfNull]
        public DateTime VoteStart { get; set; }

        [BsonIgnoreIfNull]
        public DateTime VoteDeadline { get; set; }

        [BsonIgnoreIfNull]
        public DateTime BallotReceptionDate { get; set; }

        [BsonIgnoreIfNull]
        public bool DisplayResults { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string CustomerID { get; set; }

        [BsonIgnoreIfNull]
        public string VoteStatus { get; set; }

    }
}
