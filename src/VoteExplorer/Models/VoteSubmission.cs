using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace VoteExplorer.Models
{
    public class VoteSubmission
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }

        [BsonIgnoreIfNull]
        public int UserId { get; set; }

        [BsonIgnoreIfNull]
        public string ControlNumber { get; set; }

        [BsonIgnoreIfNull]
        public string MeetingId { get; set; }

        [BsonIgnoreIfNull]
        public List<QuestionVM> VoteSelections { get; set; }

        [BsonIgnoreIfNull]
        public VoteSubmissionStatus voteSubmissionStatus { get; set; }

        [BsonIgnoreIfNull]
        public DateTime dateSubmitted { get; set; }

        [BsonIgnoreIfNull]
        public int currentQuestionBeingProcessedNumber { get; set; }

        [BsonIgnoreIfNull]
        public string currentQuestionBeingProcessedQuid { get; set; }

        [BsonIgnoreIfNull]
        public string blockChainProcessingMessage { get; set; }

        [BsonIgnoreIfNull]
        public BlockChainStage blockChainStage { get; set; }

        [BsonIgnoreIfNull]
        public string blockChainStatus { get; set; }
        public string voteString { get; internal set; }
    }

    public enum BlockChainStage
    {
        NotProcessed = 0,
        VanityAddressGenerated = 1,
        PrivateKeyImported = 2,
        CoinsSent = 3
    }

    public enum VoteSubmissionStatus
    {
        VotesSubmitted = 0,
        VotesConfirmed = 1,
        VoteCoinsPendingTransfer = 2,
        VoteCoinsTransferred = 3,
        VoteCoinsTransferredPendingDelete = 4
    }

}
