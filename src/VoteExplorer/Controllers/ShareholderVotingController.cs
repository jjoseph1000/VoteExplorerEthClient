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

        public async Task<IActionResult> Index_CodeBehind(LanguagePreference languagePreference, string revote)
        {
            string voteAnswerChoices = "";
            if (HttpContext.Session.GetString("ControlNumber") == null)
            {
                return await Task.Run<IActionResult>(() => { return RedirectToAction("Login"); });                
            }

            List<VoteSubmission> voteSubmissions = Context.votesubmission.AsQueryable().ToList();

            Voter test = await _blockchainContext.getVoteAnswersByVoterId("fce7fce0735c4accb01bb909334cfdd1");

            string controlNumber = HttpContext.Session.GetString("ControlNumber");

            SHOAccount shoaccount = Context.shoaccounts.AsQueryable().Where(sho => sho.ControlNumber == controlNumber).FirstOrDefault();
            if (shoaccount.maskedVoters != null && shoaccount.maskedVoters.Any())
            {
                List<string> voteSessionIds = new List<string>();
                string voteSessionId = "";
                string firstVoteSessionId = "";
                bool allTheSame = true;
                for (int x=0;x<shoaccount.maskedVoters.Count();x++)
                {
                    VoteMask voteMask = shoaccount.maskedVoters[x];
                    Voter voter1 = await _blockchainContext.getVoteAnswersByVoterId(voteMask.voterId);
                    voteAnswerChoices = voter1.voteAnswers;
                    string voteSessionId1 = voter1.voteSessionId;
                    voteSessionIds.Add(voteSessionId1);
                    if (x==0)
                    {
                        firstVoteSessionId = voteSessionId1;
                    }
                    else
                    {
                        if (firstVoteSessionId != voteSessionId1)
                        {
                            allTheSame = false;
                        }
                    }
                }

                if (allTheSame)
                {
                    voteSessionId = firstVoteSessionId;
                }
                else
                {

                }

                var existingVoteSubmissions = voteSubmissions.Where(v => v.ControlNumber == controlNumber && v.voteSubmissionStatus == VoteSubmissionStatus.VotesConfirmed);
                if (existingVoteSubmissions.Any())
                {
                    if (existingVoteSubmissions.Any(vs => vs._id == voteSessionId) && allTheSame)
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
                        var filterVoteSubmission = Builders<VoteSubmission>.Filter.Eq("ControlNumber", controlNumber);
                        Context.votesubmission.DeleteMany(filterVoteSubmission);
                    }
                }


            }

            if (revote == "1" || voteAnswerChoices == "")
            {
                MainVM viewModel = new MainVM();
                if (controlNumber != "-1")
                {
                    List<Question> questions = _blockchainContext.questions;

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

                    viewModel.activeQuestions.ForEach(sv => sv.orderNum = Convert.ToInt32(sv.questionIndex) + 1);

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

                return await Task.Run<IActionResult>(() => { return View(viewModel); });                
            }
            else
            {
                if (languagePreference == LanguagePreference.Russian)
                {
                    return await Task.Run<IActionResult>(() => { return RedirectToAction("Confirm_Russian", new { id = "0" }); });                    
                }
                else
                {
                    return await Task.Run<IActionResult>(() => { return RedirectToAction("Confirm", new { id = "0" }); });
                }
            }            
        }

        public async Task<IActionResult> RevoteStatus_CodeBehind(LanguagePreference languagePreference)
        {
            if (HttpContext.Session.GetString("activeVoteContractAddress") == null)
            {
                if (languagePreference == LanguagePreference.Russian)
                {
                    return await Task.Run<IActionResult>(() => { return RedirectToAction("Login_Russian"); });                    
                }
                else
                {
                    return await Task.Run<IActionResult>(() => { return RedirectToAction("Login"); });                    
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
                return await Task.Run<IActionResult>(() => { return RedirectToAction("Index_Russian"); });
            }
            else
            {
                return await Task.Run<IActionResult>(() => { return RedirectToAction("Index"); });                
            }
        }

        public async Task<IActionResult> Submit_Codebehind(string id)
        {
            IMongoQueryable<VoteSubmission> voteSubmission = Context.votesubmission.AsQueryable();

            var savedVotes = voteSubmission.Where(e => e._id == id);

            MainVM viewModel = new MainVM();
            viewModel.VoteSubmissionId = id;
            if (savedVotes.Any())
            {
                viewModel.activeQuestions = savedVotes.FirstOrDefault().VoteSelections;
            }

            return await Task.Run<IActionResult>(() => { return View(viewModel); });            
        }

        public async Task<IActionResult> Confirm_Codebehind(string id)
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

            return await Task.Run<IActionResult>(() => { return View(viewModel); });
        }

        public async Task<IActionResult> Login_Codebehind(string controlNumber, LanguagePreference languagePreference)
        {
            HttpContext.Session.SetString("ControlNumber", controlNumber);
            
            if (languagePreference == LanguagePreference.Russian)
            {
                return await Task.Run<IActionResult>(() => { return RedirectToAction("Index_Russian", new { revote = "0" }); });                
            }
            else
            {
                return await Task.Run<IActionResult>(() => { return RedirectToAction("Index", new { revote = "0" }); });                
            }
        }

        [HttpGet("en/Index/{revote}")]
        public async Task<IActionResult> Index(string revote)
        {
            try
            {                
                return await Index_CodeBehind(LanguagePreference.English, revote);
            }
            catch (Exception ex)
            {
            }

            return await Task.Run<IActionResult>(() => { return View(); });
        }

        [HttpGet("ru/Index/{revote}")]
        public async Task<IActionResult> Index_Russian(string revote)
        {
            try
            {
                return await Index_CodeBehind(LanguagePreference.Russian, revote);
            }
            catch (Exception ex)
            {
            }

            return await Task.Run<IActionResult>(() => { return View(); });
        }

        [HttpGet("en/Help")]
        public async Task<IActionResult> Help()
        {
            return await Task.Run<IActionResult>(() => { return View(); });
        }

        [HttpGet("ru/Help")]
        public async Task<IActionResult> Help_Russian()
        {
            return await Task.Run<IActionResult>(() => { return View(); });
        }

        [HttpGet("en/RevoteStatus")]
        public async Task<IActionResult> RevoteStatus()
        {
            return await RevoteStatus_CodeBehind(LanguagePreference.English);
        }

        [HttpGet("ru/RevoteStatus")]
        public async Task<IActionResult> RevoteStatus_Russian()
        {
            return await RevoteStatus_CodeBehind(LanguagePreference.Russian);
        }

        [HttpGet("en/Submit/{id}")]
        public async Task<IActionResult> Submit(string id)
        {
            return await Submit_Codebehind(id);
        }

        [HttpGet("ru/Submit/{id}")]
        public async Task<IActionResult> Submit_Russian(string id)
        {
            return await Submit_Codebehind(id);
        }


        [HttpGet("en/Confirm/{id}")]
        public async Task<IActionResult> Confirm(string id)
        {
            return await Confirm_Codebehind(id);
        }

        [HttpGet("ru/Confirm/{id}")]
        public async Task<IActionResult> Confirm_Russian(string id)
        {
            return await Confirm_Codebehind(id);
        }

        [HttpGet("en/Attend")]
        public async Task<IActionResult> Attend()
        {
            return await Task.Run<IActionResult>(() => { return View(); });            
        }

        [HttpGet("ru/Attend")]
        public async Task<IActionResult> Attend_Russian()
        {
            return await Task.Run<IActionResult>(() => { return View(); });            
        }

        [HttpGet("en/AttendConfirm")]
        public async Task<IActionResult> AttendConfirm()
        {
            return await Task.Run<IActionResult>(() => { return View(); });            
        }

        [HttpGet("ru/AttendConfirm")]
        public async Task<IActionResult> AttendConfirm_Russian()
        {
            return await Task.Run<IActionResult>(() => { return View(); });
        }

        [HttpGet("en/Login")]
        public async Task<IActionResult> Login()
        {
            HttpContext.Session.Clear();

            return await Task.Run<IActionResult>(() => { return View(); });
        }

        [HttpGet("ru/Login")]
        public async Task<IActionResult> Login_Russian()
        {
            HttpContext.Session.Clear();

            return await Task.Run<IActionResult>(() => { return View(); });
        }

        [HttpPost("en/Login")]
        public async Task<IActionResult> Login(Models.SHOLoginVM shLogin)
        {
            if (ModelState.IsValid)
            {
                return await Login_Codebehind(shLogin.controlNumber, LanguagePreference.English);
            }
            else
            {
                return await Task.Run<IActionResult>(() => { return View(); });                
            }
        }

        [HttpPost("ru/Login")]
        public async Task<IActionResult> Login_Russian(Models.SHOLogin_Russian_VM shLogin)
        {
            if (ModelState.IsValid)
            {
                return await Login_Codebehind(shLogin.controlNumber, LanguagePreference.Russian);
            }
            else
            {
                return await Task.Run<IActionResult>(() => { return View(); });
            }
        }
    }
}
