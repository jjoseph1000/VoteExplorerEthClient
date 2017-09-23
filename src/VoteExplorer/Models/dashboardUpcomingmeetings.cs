using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace VoteExplorer.Models
{
    [BsonIgnoreExtraElements]
    public class dashboardUpcomingmeetings
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }

        [BsonIgnoreIfNull]
        public string Status { get; set; }

        [BsonIgnoreIfNull]
        public DateTime Date { get; set; }

        [BsonIgnoreIfNull]
        public string Meeting { get; set; }

        [BsonIgnoreIfNull]
        public string FullText { get; set; }

    }
}
