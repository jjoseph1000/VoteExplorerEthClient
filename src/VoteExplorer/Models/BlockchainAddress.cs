using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VoteExplorer.Models
{
    public class BlockchainAddress
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }

        [BsonIgnoreIfNull]
        public string inputTransactionId { get; set; }

        [BsonIgnoreIfNull]
        public bool isFirstTransaction { get; set; }

        [BsonIgnoreIfNull]
        public string pattern { get; set; }

        [BsonIgnoreIfNull]
        public string publicAddress { get; set; }

        [BsonIgnoreIfNull]
        public string privateKey { get; set; }

        [BsonIgnoreIfNull]
        public string generalFundPublicAddress { get; set; }

        [BsonIgnoreIfNull]
        public string generalFundCoins { get; set; }

        [BsonIgnoreIfNull]
        public string contractNumber { get; set; }

        [BsonIgnoreIfNull]
        public string account { get; set; }

        [BsonIgnoreIfNull]
        public string quid { get; set; }

        [BsonIgnoreIfNull]
        public string ansid { get; set; }

        [BsonIgnoreIfNull]
        public string coins { get; set; }

        [BsonIgnoreIfNull]
        public string blockNumber { get; set; }
        
        [BsonIgnoreIfNull]
        public string transactionId { get; set; }

        [BsonIgnoreIfNull]
        public bool currentTransaction { get; set; }

        [BsonIgnoreIfNull]
        public decimal totalCoins { get; set; }

        [BsonIgnoreIfNull]
        public decimal TotalVotes { get; set; }
    }
}
