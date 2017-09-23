using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace VoteExplorer.Models
{
    [BsonIgnoreExtraElements]
    public class dashboardNewaccounts
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }

        [BsonIgnoreIfNull]
        public string Account { get; set; }

        [BsonIgnoreIfNull]
        public string MeetingSymbol { get; set; }

        [BsonIgnoreIfNull]
        public int NumberOfShares { get; set; }

        [BsonIgnoreIfNull]
        public string FullText { get; set; }

    }
}
