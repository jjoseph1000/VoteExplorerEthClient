using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace VoteExplorer.Models
{
    [BsonIgnoreExtraElements]
    public class Question
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }
        public string coin { get; set; }
        public string quid { get; set; }
        public string text { get; set; }
        [BsonIgnoreIfNull]
        public string text_ru { get; set; }
        public string block { get; set; }
        public int state { get; set; }
        public string owner { get; set; }
        public string title { get; set; }

        [BsonIgnoreIfNull]
        public string proposalNumber { get; set; }

        [BsonDateTimeOptions(DateOnly = false, Kind = DateTimeKind.Local)]
        public DateTime EndDate { get; set; }

        [BsonIgnoreIfNull]
        public List<Answer> Answers { get; set; }

        [BsonIgnoreIfNull]
        public string WinningPercentage { get; set; }

        [BsonIgnoreIfNull]
        public string WinningAnswer { get; set; }

        [BsonIgnoreIfNull]
        public string WinningAnswer_ru { get; set; }

        [BsonIgnoreIfNull]
        public string ProxyChoice { get; set; }

        [BsonIgnoreIfNull]
        public string ProxyChoice_ru { get; set; }

        [BsonIgnoreIfNull]
        public string MeetingId { get; set; }
    }
}
