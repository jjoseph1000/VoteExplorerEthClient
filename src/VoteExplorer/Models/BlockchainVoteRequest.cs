using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VoteExplorer.Models
{
    [BsonIgnoreExtraElements]
    public class BlockchainVoteRequest
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }

        [BsonIgnoreIfNull]
        public string ControlNumber { get; set; }

        [BsonIgnoreIfNull]
        public string VoteSubmissionId { get; set; }

        [BsonIgnoreIfNull]
        public DateTime dateSubmitted { get; set; }

        [BsonIgnoreIfNull]
        public string voteString { get; set; }

        [BsonIgnoreIfNull]
        public List<Voter> maskedVoters { get; set; }

        [BsonIgnoreIfNull]
        public BlockchainVoterRequestStatus blockchainVoterRequestStatus { get; set; }
    }

    public enum BlockchainVoterRequestStatus
    {
        Submitted,
        Processing,
        AcceptedByBlockchain
    }
}
