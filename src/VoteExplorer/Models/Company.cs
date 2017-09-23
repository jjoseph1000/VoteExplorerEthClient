using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace VoteExplorer.Models
{
    [BsonIgnoreExtraElements]
    public class Company
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }

        [BsonIgnoreIfNull]
        public int _Id { get; set; }

        [BsonIgnoreIfNull]
        public string CompanyName { get; set; }


    }
}
