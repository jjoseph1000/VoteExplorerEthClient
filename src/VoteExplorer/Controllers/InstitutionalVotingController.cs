using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // Needed for the SetString and GetString extension methods
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using VoteExplorer.Models;
using MongoDB.Driver.Linq;
using MongoDB.Driver;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace VoteExplorer.Controllers
{
    [Route("InstitutionalVoting")]
    public class InstitutionalVotingController : Controller
    {
        public static readonly VoteExplorerContext Context = new VoteExplorerContext();
        private GroupService groupService;

        public InstitutionalVotingController()
        {
            groupService = new GroupService(Context);
        }

        // GET: /<controller>/
        public IActionResult Index()
        {

            return View();
        }

        [HttpGet("Vote")]
        public IActionResult Vote()
        {
            try
            {
                List<Question> questions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Question>>(HttpContext.Session.GetString("questions"));
                List<Answer> answers = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Answer>>(HttpContext.Session.GetString("answers"));
                MainVM viewModel = new MainVM();

                viewModel.activeQuestions = (from q in questions
                                             where q.state == 2
                                             select new QuestionVM
                                             {
                                                 quid = q.quid,
                                                 text = q.text,
                                                 block = q.block,
                                                 keyid = q.quid + "|",
                                                 SelectedAnswerId = "Z"
                                             }
                                                ).ToList();

                foreach (QuestionVM question in viewModel.activeQuestions)
                {
                    question.Answers = (from a in answers
                                        where a.quid == question.quid
                                        orderby a.answid
                                        select new AnswerVM
                                        {
                                            quid = a.quid,
                                            answid = a.answid,
                                            text = a.test
                                        }
                                        ).ToList();
                }

                return View(viewModel);

            }
            catch (Exception ex)
            {
            }

            return View();
        }

        [HttpGet("Submit/{id}")]
        public ActionResult Submit(string id)
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

        [HttpGet("Confirm/{id}")]
        public IActionResult Confirm(string id)
        {
            List<Question> questions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Question>>(HttpContext.Session.GetString("questions"));
            List<Answer> answers = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Answer>>(HttpContext.Session.GetString("answers"));
            IMongoQueryable<VoteSubmission> voteSubmission = Context.votesubmission.AsQueryable();

            var savedVotes = voteSubmission.Where(e => e._id == id);

            MainVM viewModel = new MainVM();

            if (savedVotes.Any())
            {
                viewModel.activeQuestions = savedVotes.FirstOrDefault().VoteSelections;
            }

            return View(viewModel);
        }

        [HttpGet("Attend")]
        public IActionResult Attend()
        {


            return View();
        }

        [HttpGet("AttendConfirm")]
        public IActionResult AttendConfirm()
        {


            return View();
        }

        [HttpGet("Login")]
        public IActionResult Login()
        {
            HttpContext.Session.SetString("CustomerId", "594b272343a0ac0a9f8186ad");
            List<Meeting> meetingsDisplayResults = Context.meetings.AsQueryable().Where(q => q.DisplayResults == true).ToList();
            if (meetingsDisplayResults.Any())
            {
                HttpContext.Session.SetString("displayResultsMeetingId", meetingsDisplayResults.FirstOrDefault()._id);
            }
            else
            {
                HttpContext.Session.SetString("displayResultsMeetingId", "-1");
            }

            return View();
        }

        [HttpPost("en/Login")]
        public IActionResult Login(Models.IVYLoginVM ivyLogin)
        {
            if (ModelState.IsValid)
            {
                return RedirectToAction("Dashboard");
            }
            else
            {
                return View();
            }
        }

        [HttpGet("Dashboard")]
        public IActionResult Dashboard()
        {


            return View();
        }


        [HttpGet("Calendar")]
        public IActionResult Calendar()
        {


            return View();
        }

        [HttpGet("MeetingsList")]
        public IActionResult MeetingsList()
        {


            return View();
        }

        [HttpGet("AccountsList")]
        public IActionResult AccountsList()
        {


            return View();
        }

        [HttpGet("VoteReports")]
        public IActionResult VoteReports()
        {


            return View();
        }


        [HttpGet("CoReporting")]
        public IActionResult CoReporting()
        {


            return View();
        }

        [HttpGet("Alerts")]
        public IActionResult Alerts()
        {


            return View();
        }

        [HttpGet("Admin")]
        public IActionResult Admin()
        {
            ViewBag.CustomerId = HttpContext.Session.GetString("CustomerId");

            return View();
        }


    }
}
