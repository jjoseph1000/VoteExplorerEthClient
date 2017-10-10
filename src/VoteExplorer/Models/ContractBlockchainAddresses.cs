using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VoteExplorer.Models
{
    public class ContractBlockchainAddresses
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }

        [BsonIgnoreIfNull]
        public string contractNumber { get; set; }

        [BsonIgnoreIfNull]
        public List<BlockchainAddress> blockchainAddreses { get; set; }

        [BsonIgnoreIfNull]
        public string lastModifiedDatetime { get; set; }
    }
}
