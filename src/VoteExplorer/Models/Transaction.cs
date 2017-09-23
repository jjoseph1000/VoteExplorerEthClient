using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace VoteExplorer.Models
{
    public class Transaction
    {

        [BsonIgnoreIfNull]
        public string TransactionId { get; set; }

        [BsonIgnoreIfNull]
        public string BlockId { get; set; }

        [BsonIgnoreIfNull]
        public string BlockText { get; set; }

        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(DateOnly = false, Kind = DateTimeKind.Local)]
        public DateTime ApproxTime { get; set; }

        [BsonIgnoreIfNull]
        //[BsonRepresentation(BsonType.Decimal128)]
        public Decimal Amount { get; set; }
        [BsonIgnoreIfNull]
        //[BsonRepresentation(BsonType.Decimal128)]
        public Decimal Balance { get; set; }

        [BsonIgnoreIfNull]
        public string Currency { get; set; }

    }

}
