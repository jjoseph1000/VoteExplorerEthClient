using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace VoteExplorer.Models
{
    [BsonIgnoreExtraElements]
    public class dashboardNewmeetings
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }

        [BsonIgnoreIfNull]
        public string Company { get; set; }

        [BsonIgnoreIfNull]
        public DateTime AddDate { get; set; }

        [BsonIgnoreIfNull]
        public DateTime AGMDate { get; set; }

        [BsonIgnoreIfNull]
        public string FullText { get; set; }

    }
}
