using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace VoteExplorer.Models
{
    [BsonIgnoreExtraElements]
    public class Account
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }

        [BsonIgnoreIfNull]
        public int _Id { get; set; }

        [BsonIgnoreIfNull]
        public int AccountNumber { get; set; }
        
        [BsonRepresentation(BsonType.ObjectId)]
        public string CompanyId { get; set; }

        [BsonIgnoreIfNull]
        public long AvailableShares { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string CustodianId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string CustomerID { get; set; }

    }
}
