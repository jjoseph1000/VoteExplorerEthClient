using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using VoteExplorer.Controllers;

namespace VoteExplorer.Models
{
    public class MainVM
    {
        public Question activeQuestion { get; set; }

        public List<QuestionVM> activeQuestions { get; set; }
        public List<QuestionVM> completedQuestions { get; set; }

        public string VoteSubmissionId { get; set; }

        public DateTime dateSubmitted { get; set; }

        public string contractNumber { get; set; }

        public string refreshDataFromBlockchain { get; set; }

        public VoteSubmissionStatus voteSubmissionStatus { get; set; }

        public Meeting meeting { get; set; }
        public string voteString { get; internal set; }
    }

    public class AnswerPieChartVM
    {
        public string quid { get; set; }
        public string answid { get; set; }
        public string text { get; set; }
        public string text_ru { get; set; }
        public decimal TotalVotes { get; set; }

        public decimal totalCoins { get; set; }
        public decimal Value { get; set; }
        public string Category { get; set; }
        public string keyvalue { get; set; }
    }

    public class AnswerBarSchemaVM
    {
        public string name { get; set; }
        public string field { get; set; }
        public string quid { get;  set; }
        public string answid { get;  set; }
        public string text { get; set; }
        public decimal TotalVotes { get; set; }
    }

    public class QuestionVM
    {
        public string _id { get; set; }
        public string keyid { get; set; }
        public string coin { get; set; }
        public string quid { get; set; }
        public string text { get; set; }
        public string text_ru { get; set; }

        public int questionIndex { get; set; }

        public int orderNum { get; set; }

        public string block { get; set; }
        [DataType("Integer")]
        public int state { get; set; }
        public string owner { get; set; }
        public string title { get; set; }
        public string proposalNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        public string SelectedAnswerId { get; set; }

        public List<AnswerVM> Answers { get; set; }
        public string boardRecommendation { get; internal set; }
    }

    public class AnswerVM
    {
        public string quid { get; set; }
        public string answid { get; set; }
        public string text { get; set; }

    }

    public class VoteSubmissionSatusVM
    {
        public string id { get; set; }
        public string voteSubmissionStatus { get; set; }

        public string availableShares { get; set; }

    }

}
