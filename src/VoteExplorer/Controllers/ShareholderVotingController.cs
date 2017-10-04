using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VoteExplorer.Models;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.Linq;
using System.Dynamic;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace VoteExplorer.Controllers
{
    [Route("ShareholderVoting")]
    public class ShareholderVotingController : Controller
    {
        public static readonly VoteExplorerContext Context = new VoteExplorerContext();
        private VoteExplorerBlockchainContext _blockchainContext;
        // GET: /<controller>/

        public ShareholderVotingController(VoteExplorerBlockchainContext blockchainContext)
        {
            _blockchainContext = blockchainContext;
        }


        public enum LanguagePreference
        {
            English  = 0,
            Russian = 1
        }

        public IActionResult Index_CodeBehind(LanguagePreference languagePreference)
        {
            List<VoteSubmission> voteSubmissions = Context.votesubmission.AsQueryable().ToList();

            if (HttpContext.Session.GetString("activeVoteContractAddress") == null)
            {
                if (languagePreference == LanguagePreference.Russian)
                {
                    return RedirectToAction("Login_Russian");
                }
                else
                {
                    return RedirectToAction("Login");
                }

            }

            string MeetingId = HttpContext.Session.GetString("activeVoteContractAddress");

            var existingVoteSubmissions = voteSubmissions.Where(v => v.ControlNumber == HttpContext.Session.GetString("ControlNumber") && v.voteSubmissionStatus != VoteSubmissionStatus.VotesSubmitted && v.voteSubmissionStatus != VoteSubmissionStatus.VoteCoinsTransferredPendingDelete && v.MeetingId == MeetingId);
            if (existingVoteSubmissions.Any())
            {
                if (languagePreference == LanguagePreference.Russian)
                {
                    return RedirectToAction("Confirm_Russian", new { id = existingVoteSubmissions.FirstOrDefault()._id });
                }
                else
                {
                    return RedirectToAction("Confirm", new { id = existingVoteSubmissions.FirstOrDefault()._id });
                }
            }
            else
            {
                MainVM viewModel = new MainVM();

                string meetingId =  HttpContext.Session.GetString("activeVoteContractAddress");

                if (meetingId != "-1")
                {
                    List<Question> questions = _blockchainContext.questions;
                    List<Answer> answers = _blockchainContext.answers;

                    viewModel.activeQuestions = (from q in questions
                                                 orderby q.questionIndex
                                                 select new QuestionVM
                                                 {
                                                     quid = q.quid,
                                                     text = q.text,
                                                     text_ru = q.text_ru,
                                                     questionIndex = q.questionIndex,
                                                     block = q.block,
                                                     keyid = q.quid + "|",
                                                     SelectedAnswerId = "Z"
                                                 }
                                                    ).ToList();

                    viewModel.activeQuestions.ForEach(sv => sv.orderNum = Convert.ToInt32(sv.questionIndex) + 1);

                    foreach (QuestionVM question in viewModel.activeQuestions)
                    {
                        question.Answers = (from a in answers
                                            orderby a.answid
                                            select new AnswerVM
                                            {
                                                quid = question.quid,
                                                answid = a.answid,
                                                text = a.test
                                            }
                                            ).ToList();
                    }
                }
                else
                {
                    viewModel.activeQuestions = new List<QuestionVM>();
                }

                return View(viewModel);

            }
        }

        public IActionResult RevoteStatus_CodeBehind(LanguagePreference languagePreference)
        {
            if (HttpContext.Session.GetString("activeVoteContractAddress") == null)
            {
                if (languagePreference == LanguagePreference.Russian)
                {
                    return RedirectToAction("Login_Russian");
                }
                else
                {
                    return RedirectToAction("Login");
                }

            }

            string meetingId = HttpContext.Session.GetString("activeVoteContractAddress");
            string controlNumber = HttpContext.Session.GetString("ControlNumber");

            var filter = Builders<VoteSubmission>.Filter.Eq("ControlNumber", controlNumber) & Builders<VoteSubmission>.Filter.Eq("MeetingId", meetingId) & Builders<VoteSubmission>.Filter.Eq("voteSubmissionStatus",VoteSubmissionStatus.VoteCoinsTransferred);
            var update = Builders<VoteSubmission>.Update.Set("voteSubmissionStatus", VoteSubmissionStatus.VoteCoinsTransferredPendingDelete);

            Context.votesubmission.UpdateOne(filter, update);

            //var filterSubmittedAddresses = Builders<BlockchainAddress>.Filter.Eq("meetingId", meetingId) & Builders<BlockchainAddress>.Filter.Eq("controlNumber", controlNumber);
            //Context.blockchainaddresses.DeleteMany(filterSubmittedAddresses);

            if (languagePreference == LanguagePreference.Russian)
            {
                return RedirectToAction("Index_Russian");
            }
            else
            {
                return RedirectToAction("Index");
            }
        }

        public IActionResult Submit_Codebehind(string id)
        {
            IMongoQueryable<VoteSubmission> voteSubmission = Context.votesubmission.AsQueryable();

            var savedVotes = voteSubmission.Where(e => e._id == id);

            MainVM viewModel = new MainVM();
            viewModel.VoteSubmissionId = id;
            if (savedVotes.Any())
            {
                viewModel.activeQuestions = savedVotes.FirstOrDefault().VoteSelections;
            }

            return View(viewModel);
        }

        public IActionResult Confirm_Codebehind(string id)
        {
            IMongoQueryable<VoteSubmission> voteSubmission = Context.votesubmission.AsQueryable();

            var savedVotes = voteSubmission.Where(e => e._id == id);

            MainVM viewModel = new MainVM();
            viewModel.VoteSubmissionId = id;
            if (savedVotes.Any())
            {
                viewModel.activeQuestions = savedVotes.FirstOrDefault().VoteSelections;
                viewModel.voteSubmissionStatus = savedVotes.FirstOrDefault().voteSubmissionStatus;
                if (savedVotes.FirstOrDefault().dateSubmitted != null)
                {
                    viewModel.dateSubmitted = savedVotes.FirstOrDefault().dateSubmitted;
                }
                else
                {
                    viewModel.dateSubmitted = DateTime.Now;
                }
            }

            return View(viewModel);
        }

        public IActionResult Login_Codebehind(string controlNumber, LanguagePreference languagePreference)
        {
            List<Meeting> meetings = Context.meetings.AsQueryable().Where(q => q.VoteStart != null && q.VoteStart <= DateTime.Now && q.VoteDeadline != null && DateTime.Now <= q.VoteDeadline).ToList();
            if (meetings.Any())
            {
                HttpContext.Session.SetString("activeVoteContractAddress", meetings.FirstOrDefault().ContractAddress);
            }
            else
            {
                HttpContext.Session.SetString("activeVoteContractAddress", "-1");
            }

            List<Meeting> meetingsDisplayResults = Context.meetings.AsQueryable().Where(q => q.DisplayResults == true).ToList();
            if (meetingsDisplayResults.Any())
            {
                HttpContext.Session.SetString("displayResultsContractAddress", meetingsDisplayResults.FirstOrDefault().ContractAddress);
            }
            else
            {
                HttpContext.Session.SetString("displayResultsContractAddress", "-1");
            }

            HttpContext.Session.SetString("ControlNumber", controlNumber);

            string meetingId = HttpContext.Session.GetString("activeVoteContractAddress");

            var filterSubmittedAddresses = Builders<VoteSubmission>.Filter.Eq("ControlNumber", controlNumber) & Builders<VoteSubmission>.Filter.Eq("MeetingId", meetingId) & Builders<VoteSubmission>.Filter.Eq("voteSubmissionStatus", VoteSubmissionStatus.VotesSubmitted);
            Context.votesubmission.DeleteMany(filterSubmittedAddresses);

            var filter = Builders<VoteSubmission>.Filter.Eq("ControlNumber", controlNumber) & Builders<VoteSubmission>.Filter.Eq("MeetingId", meetingId) & Builders<VoteSubmission>.Filter.Eq("voteSubmissionStatus", VoteSubmissionStatus.VoteCoinsTransferredPendingDelete);
            var update = Builders<VoteSubmission>.Update.Set("voteSubmissionStatus", VoteSubmissionStatus.VoteCoinsTransferred);

            Context.votesubmission.UpdateOne(filter, update);


            if (languagePreference == LanguagePreference.Russian)
            {
                return RedirectToAction("Index_Russian");
            }
            else
            {
                return RedirectToAction("Index");
            }
        }

        [HttpGet("en/Index")]
        public IActionResult Index()
        {
            try
            {
                return Index_CodeBehind(LanguagePreference.English);
            }
            catch (Exception ex)
            {
            }

            return View();

        }

        [HttpGet("ru/Index")]
        public IActionResult Index_Russian()
        {
            try
            {
                return Index_CodeBehind(LanguagePreference.Russian);
            }
            catch (Exception ex)
            {
            }

            return View();
        }

        [HttpGet("en/Help")]
        public IActionResult Help()
        {
            return View();
        }

        [HttpGet("ru/Help")]
        public IActionResult Help_Russian()
        {
            return View();
        }

        [HttpGet("en/RevoteStatus")]
        public IActionResult RevoteStatus()
        {
            return RevoteStatus_CodeBehind(LanguagePreference.English);
        }

        [HttpGet("ru/RevoteStatus")]
        public IActionResult RevoteStatus_Russian()
        {
            return RevoteStatus_CodeBehind(LanguagePreference.Russian);
        }

        [HttpGet("en/Submit/{id}")]
        public IActionResult Submit(string id)
        {
            return Submit_Codebehind(id);
        }

        [HttpGet("ru/Submit/{id}")]
        public IActionResult Submit_Russian(string id)
        {
            return Submit_Codebehind(id);
        }


        [HttpGet("en/Confirm/{id}")]
        public IActionResult Confirm(string id)
        {
            return Confirm_Codebehind(id);
        }

        [HttpGet("ru/Confirm/{id}")]
        public IActionResult Confirm_Russian(string id)
        {
            return Confirm_Codebehind(id);
        }

        [HttpGet("en/Attend")]
        public IActionResult Attend()
        {
            return View();
        }

        [HttpGet("ru/Attend")]
        public IActionResult Attend_Russian()
        {
            return View();
        }

        [HttpGet("en/AttendConfirm")]
        public IActionResult AttendConfirm()
        {
            return View();
        }

        [HttpGet("ru/AttendConfirm")]
        public IActionResult AttendConfirm_Russian()
        {
            return View();
        }

        [HttpGet("en/Login")]
        public IActionResult Login()
        {
            HttpContext.Session.Clear();

            return View();
        }

        [HttpGet("ru/Login")]
        public IActionResult Login_Russian()
        {
            HttpContext.Session.Clear();

            return View();
        }

        [HttpPost("en/Login")]
        public IActionResult Login(Models.SHOLoginVM shLogin)
        {
            if (ModelState.IsValid)
            {
                return Login_Codebehind(shLogin.controlNumber, LanguagePreference.English);
            }
            else
            {
                return View();
            }
        }

        [HttpPost("ru/Login")]
        public IActionResult Login_Russian(Models.SHOLogin_Russian_VM shLogin)
        {
            if (ModelState.IsValid)
            {
                return Login_Codebehind(shLogin.controlNumber, LanguagePreference.Russian);
            }
            else
            {
                return View();
            }
        }
    }
}
