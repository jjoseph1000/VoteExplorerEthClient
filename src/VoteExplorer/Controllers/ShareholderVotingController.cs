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
        // GET: /<controller>/

        public enum LanguagePreference
        {
            English  = 0,
            Russian = 1
        }

        public IActionResult Index_CodeBehind(LanguagePreference languagePreference, string revote)
        {
            if (HttpContext.Session.GetString("contractNumber") == null)
            {
                return RedirectToAction("Login");
            }

            List<VoteSubmission> voteSubmissions = Context.votesubmission.AsQueryable().ToList();
            string voteSessionId = HttpContext.Session.GetString("voteSessionId");
            string contractNumber = HttpContext.Session.GetString("contractNumber");
            string account = HttpContext.Session.GetString("account");
            string voteAnswerChoices = HttpContext.Session.GetString("voteAnswerChoices");

            var existingVoteSubmissions = voteSubmissions.Where(v => v.account == HttpContext.Session.GetString("account") && v.voteSubmissionStatus == VoteSubmissionStatus.VotesConfirmed && v.contractNumber == HttpContext.Session.GetString("contractNumber"));
            if (existingVoteSubmissions.Any())
            {
                if (existingVoteSubmissions.Any(vs=>vs._id == voteSessionId))
                {
                    var filterVoteSubmission = Builders<VoteSubmission>.Filter.Eq("_id", voteSessionId);
                    Context.votesubmission.DeleteMany(filterVoteSubmission);
                }
                else
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
            }
            else
            {
                if (revote == "0")
                {
                    var filterVoteSubmission = Builders<VoteSubmission>.Filter.Eq("account", account) & Builders<VoteSubmission>.Filter.Eq("contractNumber", contractNumber);
                    Context.votesubmission.DeleteMany(filterVoteSubmission);
                }
            }

            if (revote == "1" || voteAnswerChoices == "")
            {
                MainVM viewModel = new MainVM();
                viewModel.voteTokenName = HttpContext.Session.GetString("voteTokenName");
                if (contractNumber != "-1")
                {
                    List<Question> questions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Question>>(HttpContext.Session.GetString("questions"));

                    viewModel.activeQuestions = (from q in questions
                                                 orderby q.questionIndex                                                 
                                                 select new QuestionVM
                                                 {
                                                     quid = q.quid,
                                                     boardRecommendation = q.boardRecommendation,
                                                     text = q.text,
                                                     text_ru = q.text_ru,
                                                     questionIndex = q.questionIndex,
                                                     keyid = q.quid + "|",
                                                     SelectedAnswerId = "Z"
                                                 }
                                                    ).ToList();

                    viewModel.activeQuestions.ForEach(sv => sv.orderNum = Convert.ToInt32(sv.questionIndex)+1);

                    List<Answer> answers = new List<Answer>();
                    Answer answer = new Answer();
                    answer.answid = "A";
                    answer.test = "FOR";
                    answers.Add(answer);
                    Answer answer2 = new Answer();
                    answer2.answid = "B";
                    answer2.test = "AGAINST";
                    answers.Add(answer2);
                    Answer answer3 = new Answer();
                    answer3.answid = "Z";
                    answer3.test = "ABSTAIN";
                    answers.Add(answer3);

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
            else
            {
                if (languagePreference == LanguagePreference.Russian)
                {
                    return RedirectToAction("Confirm_Russian", new { id = "0" });
                }
                else
                {
                    return RedirectToAction("Confirm", new { id = "0" });
                }
            }
        }

        public IActionResult RevoteStatus_CodeBehind(LanguagePreference languagePreference)
        {
            if (HttpContext.Session.GetString("contractNumber") == null)
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

            string meetingId = HttpContext.Session.GetString("activeVoteMeetingId");
            string controlNumber = HttpContext.Session.GetString("ControlNumber");

            var filter = Builders<VoteSubmission>.Filter.Eq("ControlNumber", controlNumber) & Builders<VoteSubmission>.Filter.Eq("MeetingId", meetingId) & Builders<VoteSubmission>.Filter.Eq("voteSubmissionStatus",VoteSubmissionStatus.VoteCoinsTransferred);
            var update = Builders<VoteSubmission>.Update.Set("voteSubmissionStatus", VoteSubmissionStatus.VoteCoinsTransferredPendingDelete);

            Context.votesubmission.UpdateOne(filter, update);

            //var filterSubmittedAddresses = Builders<BlockchainAddress>.Filter.Eq("meetingId", meetingId) & Builders<BlockchainAddress>.Filter.Eq("controlNumber", controlNumber);
            //Context.blockchainaddresses.DeleteMany(filterSubmittedAddresses);

            if (languagePreference == LanguagePreference.Russian)
            {
                return RedirectToAction("Index_Russian", new { revote = "1" });
            }
            else
            {
                return RedirectToAction("Index", new { revote = "1" });
            }
        }

        public IActionResult Submit_Codebehind(string id)
        {
            IMongoQueryable<VoteSubmission> voteSubmission = Context.votesubmission.AsQueryable();

            var savedVotes = voteSubmission.Where(e => e._id == id);

            MainVM viewModel = new MainVM();
            viewModel.VoteSubmissionId = id;
            viewModel.voteTokenName = HttpContext.Session.GetString("voteTokenName");

            if (savedVotes.Any())
            {
                viewModel.activeQuestions = savedVotes.FirstOrDefault().VoteSelections.OrderBy(sv=>sv.orderNum).ToList();
                viewModel.voteString = savedVotes.FirstOrDefault().voteString;
            }

            return View(viewModel);
        }

        public IActionResult Confirm_Codebehind(string id)
        {
            if (HttpContext.Session.GetString("contractNumber") == null)
            {
                return RedirectToAction("Login");
            }

            MainVM viewModel = new MainVM();
            viewModel.voteTokenName = HttpContext.Session.GetString("voteTokenName");

            if (id != "0")
            {
                IMongoQueryable<VoteSubmission> voteSubmission = Context.votesubmission.AsQueryable();

                var savedVotes = voteSubmission.Where(e => e._id == id);

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
            }
            else
            {
                string voteString = HttpContext.Session.GetString("voteAnswerChoices");
                viewModel.VoteSubmissionId = id;
                List<Question> questions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Question>>(HttpContext.Session.GetString("questions"));

                viewModel.activeQuestions = (from q in questions
                                             orderby q.questionIndex                                             
                                             select new QuestionVM
                                             {
                                                 quid = q.quid,
                                                 boardRecommendation = q.boardRecommendation,
                                                 text = q.text,
                                                 text_ru = q.text_ru,
                                                 questionIndex = q.questionIndex,
                                                 keyid = q.quid + "|",
                                                 SelectedAnswerId = "Z"                                                 
                                             }
                                                ).ToList();

                viewModel.activeQuestions.ForEach(q => q.questionIndex = (Convert.ToInt32(q.questionIndex) + 1).ToString());
                viewModel.activeQuestions = viewModel.activeQuestions.OrderBy(q => q.questionIndex).ToList();

                for (int x=0;x<viewModel.activeQuestions.Count();x++)
                {
                    viewModel.activeQuestions[x].SelectedAnswerId = voteString.Substring(x, 1);
                }

                viewModel.voteSubmissionStatus = VoteSubmissionStatus.VotesConfirmed;
            }

            return View(viewModel);
        }

        public IActionResult Login_Codebehind(string contractNumber, LanguagePreference languagePreference)
        {
            HttpContext.Session.SetString("contractNumber", contractNumber);

            if (languagePreference == LanguagePreference.Russian)
            {
                return RedirectToAction("Index_Russian", new { revote = "0" });
            }
            else
            {
                return RedirectToAction("Index", new { revote = "0" });
            }
        }

        [HttpGet("en/Index/{revote}")]
        public IActionResult Index(string revote)
        {
            try
            {
                return Index_CodeBehind(LanguagePreference.English, revote);
            }
            catch (Exception ex)
            {
            }

            return View();

        }

        [HttpGet("ru/Index/{revote}")]
        public IActionResult Index_Russian(string revote)
        {
            try
            {
                return Index_CodeBehind(LanguagePreference.Russian, revote);
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
                return Login_Codebehind(shLogin.contractNumber, LanguagePreference.English);
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
