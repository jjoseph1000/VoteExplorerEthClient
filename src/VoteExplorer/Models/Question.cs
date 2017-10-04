using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace VoteExplorer.Models
{
    [BsonIgnoreExtraElements]
    [FunctionOutput]
    public class Question
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }

        [Parameter("uint", 1)]
        public int questionIndex { get; set; }

        [Parameter("string", 2)]
        public string quid { get; set; }

        [Parameter("uint", 3)]
        public int questionTextRows { get; set; }

        [Parameter("string", 4)]
        public string boardRecommendation { get; set; }

        [Parameter("uint", 5)]
        public int isActive { get; set; }

        public string coin { get; set; }
        public string text { get; set; }
        [BsonIgnoreIfNull]
        public string text_ru { get; set; }
        public string block { get; set; }
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
