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
using Microsoft.Extensions.Configuration;
using System.IO;

namespace VoteExplorer.Controllers
{

    [Route("Observer")]
    public class ObserverController : Controller
    {
        public static readonly VoteExplorerContext Context = new VoteExplorerContext();
        private VoteExplorerBlockchainContext _blockchainContext;

        public ObserverController(VoteExplorerBlockchainContext blockchainContext)
        {
            _blockchainContext = blockchainContext;
        }

        public enum LanguagePreference
        {
            English = 0,
            Russian = 1
        }

        public IActionResult Index_Codebehind(string UserType, LanguagePreference languagePreference)
        {
            if (HttpContext.Session.GetString("displayResultsContractAddress") == null)
            {
                if (languagePreference == LanguagePreference.Russian)
                {
                    if (UserType=="IVY")
                    {
                        return RedirectToAction("Login_Russian","InstitutionalVoting");
                    }
                    else
                    {
                        return RedirectToAction("Login_Russian", "ShareholderVoting");
                    }
                }
                else
                {
                    if (UserType == "IVY")
                    {
                        return RedirectToAction("Login", "InstitutionalVoting");
                    }
                    else
                    {
                        return RedirectToAction("Login", "ShareholderVoting");
                    }
                }

            }

            MainVM viewModel = new MainVM();

            string meetingId = HttpContext.Session.GetString("displayResultsContractAddress");


            if (meetingId != "-1")
            {
                List<Question> questions = _blockchainContext.questions;

                viewModel.completedQuestions = (from q in questions
                                                select new QuestionVM
                                                {
                                                    quid = q.quid,
                                                    text = q.text,
                                                    block = q.block,
                                                    keyid = q.quid + "|"
                                                }
                                                                   ).ToList();

                viewModel.meeting = Context.meetings.AsQueryable().Where(m => m._id == meetingId).FirstOrDefault();
            }
            else
            {
                viewModel.completedQuestions = new List<QuestionVM>();
            }

            ViewBag.UserType = UserType;

            return View(viewModel);
        }

        public async Task<IActionResult> IndexRealtime_Codebehind(string UserType, LanguagePreference languagePreference)
        {
            if (HttpContext.Session.GetString("ControlNumber") == null)
            {
                return RedirectToAction("Login", "ShareholderVoting");
            }


            var builder = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            IConfigurationRoot Configuration = builder.Build();

            int refreshBlockchainDataInSeconds = Convert.ToInt32(Configuration["refreshBlockchainDataInSeconds"]);

            string contractNumber = _blockchainContext.ContractAddress;
            List<ContractBlockchainAddresses> contractBlockchainAddresses = Context.contractBlockchainAddresses.AsQueryable().ToList();

            MainVM viewModel = new MainVM();
            if (contractBlockchainAddresses.Any(bc => bc.contractNumber == contractNumber && Convert.ToDateTime(bc.lastModifiedDatetime).AddSeconds(refreshBlockchainDataInSeconds) > DateTime.Now))
            {
                viewModel.refreshDataFromBlockchain = "false";
            }
            else
            {
                viewModel.refreshDataFromBlockchain = "true";
            }

            viewModel.contractNumber = contractNumber;
            ViewBag.UserType = UserType;

            return await Task.Run<IActionResult>(() => { return View(viewModel); });
        }

        public IActionResult Question_Codebehind(string quidAndUserType, LanguagePreference languagePreference)
        {
            string quid = quidAndUserType.Split('|')[0].ToString();
            string UserType = quidAndUserType.Split('|')[1].ToString();

            if (HttpContext.Session.GetString("displayResultsContractAddress") == null)
            {
                if (languagePreference == LanguagePreference.Russian)
                {
                    if (UserType == "IVY")
                    {
                        return RedirectToAction("Login_Russian", "InstitutionalVoting");
                    }
                    else
                    {
                        return RedirectToAction("Login_Russian", "ShareholderVoting");
                    }
                }
                else
                {
                    if (UserType == "IVY")
                    {
                        return RedirectToAction("Login", "InstitutionalVoting");
                    }
                    else
                    {
                        return RedirectToAction("Login", "ShareholderVoting");
                    }
                }

            }


            List<Question> questions = _blockchainContext.questions;
            var question = questions.FirstOrDefault(q => q.quid == quid);
            QuestionVM questionVM = new QuestionVM();
            questionVM.quid = quid;
            questionVM.text = question.text;

            ViewBag.UserType = UserType;

            return View(questionVM);
        }

        public IActionResult QuestionRealtime_Codebehind(string quidAndUserType, LanguagePreference languagePreference)
        {
            if (HttpContext.Session.GetString("ControlNumber") == null)
            {
                return RedirectToAction("Login", "ShareholderVoting");
            }

            var builder = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            IConfigurationRoot Configuration = builder.Build();

            string quid = quidAndUserType.Split('|')[0].ToString();
            string UserType = quidAndUserType.Split('|')[1].ToString();

            List<Question> questions = _blockchainContext.questions;
            var question = questions.FirstOrDefault(q => q.quid == quid);
            QuestionVM questionVM = new QuestionVM();
            questionVM.quid = quid;
            questionVM.text = question.text;
            questionVM.blockchainExplorerUrl = Configuration["blockchainExplorerUrl"].ToString();

            ViewBag.UserType = UserType;

            return View(questionVM);
        }


        [HttpGet("en/Index/{UserType}")]
        public IActionResult Index(string UserType)
        {
            try
            {
                return Index_Codebehind(UserType, LanguagePreference.English);
            }
            catch (Exception ex)
            {
            }

            return View();
        }

        [HttpGet("en/IndexRealtime/{UserType}")]
        public async Task<IActionResult> IndexRealtime(string UserType)
        {
            try
            {
                return await IndexRealtime_Codebehind(UserType, LanguagePreference.English);
            }
            catch (Exception ex)
            {
            }

            return await Task.Run<IActionResult>(() => { return View(); });
        }

        [HttpGet("ru/Index/{UserType}")]
        public IActionResult Index_Russian(string UserType)
        {
            try
            {
                return Index_Codebehind(UserType, LanguagePreference.Russian);
            }
            catch (Exception ex)
            {
            }

            return View();
        }

        [HttpGet("ru/IndexRealtime/{UserType}")]
        public async Task<IActionResult> IndexRealtime_Russian(string UserType)
        {
            try
            {
                return await IndexRealtime_Codebehind(UserType, LanguagePreference.Russian);
            }
            catch (Exception ex)
            {
            }

            return await Task.Run<IActionResult>(() => { return View(); });
        }

        [HttpGet("en/Question/{quidAndUserType}")]
        public IActionResult Question(string quidAndUserType)
        {
            return Question_Codebehind(quidAndUserType, LanguagePreference.English);
        }

        [HttpGet("en/QuestionRealtime/{quidAndUserType}")]
        public IActionResult QuestionRealtime(string quidAndUserType)
        {
            return QuestionRealtime_Codebehind(quidAndUserType, LanguagePreference.English);
        }

        [HttpGet("ru/Question/{quidAndUserType}")]
        public IActionResult Question_Russian(string quidAndUserType)
        {
            return Question_Codebehind(quidAndUserType, LanguagePreference.Russian);
        }

        [HttpGet("ru/QuestionRealtime/{quidAndUserType}")]
        public IActionResult QuestionRealtime_Russian(string quidAndUserType)
        {
            return QuestionRealtime_Codebehind(quidAndUserType, LanguagePreference.Russian);
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
