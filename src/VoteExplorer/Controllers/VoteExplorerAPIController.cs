using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using VoteExplorer.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Dynamic;
using Microsoft.AspNetCore.Http; // Needed for the SetString and GetString extension methods
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.Extensions.Logging;
using HtmlAgilityPack;


// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace VoteExplorer.Controllers
{
    public enum LanguagePreference
    {
        English = 0,
        Russian = 1
    }

    [Route("api")]
    public class VoteExplorerAPIController : Controller
    {
        Timer _tm = null;
        public static readonly VoteExplorerContext Context = new VoteExplorerContext();
        public static bool accessBlockChain = checkIfAccessBlockChain();
        readonly ILogger<VoteExplorerAPIController> _log;

        public VoteExplorerAPIController(ILogger<VoteExplorerAPIController> log)
        {
            _log = log;
        }

        public static bool checkIfAccessBlockChain()
        {
            IConfigurationRoot Configuration;

            var builder2 = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");

            Configuration = builder2.Build();

            bool accessBlockChain = Convert.ToBoolean(Configuration["accessBlockChain"]);
            return accessBlockChain;
        }

        [HttpPost("SetSessionVariables")]
        public ActionResult SetSessionVariables([FromBody]Newtonsoft.Json.Linq.JObject sessionValues)
        {
            string contractNumber = sessionValues["contractNumber"].ToString();
            HttpContext.Session.SetString("contractNumber", contractNumber);
            string voteTokenAddress = sessionValues["voteTokenAdd"].ToString();
            HttpContext.Session.SetString("voteTokenAddress", voteTokenAddress);
            string voteTokenName = sessionValues["currentTokenName"].ToString();
            HttpContext.Session.SetString("voteTokenName", voteTokenName);
            string voteTokenSymbol = sessionValues["currentTokenSymbol"].ToString();
            HttpContext.Session.SetString("voteTokenSymbol", voteTokenSymbol);
            string voteTokenDecimal = sessionValues["currentTokenDecimal"].ToString();
            HttpContext.Session.SetString("voteTokenDecimal", voteTokenDecimal);
            string account = sessionValues["account"].ToString();
            HttpContext.Session.SetString("account", account);
            string userBalance = sessionValues["userBalance"].ToString();
            HttpContext.Session.SetString("userBalance", userBalance);
            HttpContext.Session.SetString("questions", Newtonsoft.Json.JsonConvert.SerializeObject(sessionValues["questions"]));
            List<Question> questions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Question>>(HttpContext.Session.GetString("questions"));
            List<Answer> answers = new List<Answer>();
            foreach (Question question in questions)
            {
                Answer answer = new Answer();
                answer.quid = question.quid;
                answer.answid = "A";
                answer.test = "FOR";
                answers.Add(answer);
                answer = new Answer();
                answer.quid = question.quid;
                answer.answid = "B";
                answer.test = "AGAINST";
                answers.Add(answer);
                answer = new Answer();
                answer.quid = question.quid;
                answer.answid = "Z";
                answer.test = "ABSTAIN";
                answers.Add(answer); 
            }
            HttpContext.Session.SetString("answers", Newtonsoft.Json.JsonConvert.SerializeObject(answers));


            string voteSessionId = sessionValues["voteSessionId"].ToString();
            HttpContext.Session.SetString("voteSessionId", voteSessionId);
            string voteAnswerChoices = sessionValues["voteAnswerChoices"].ToString();
            HttpContext.Session.SetString("voteAnswerChoices", voteAnswerChoices);
            string transactionId = sessionValues["transactionId"].ToString();
            HttpContext.Session.SetString("transactionId", transactionId);

            return Json("OK");
        }

        [HttpGet("DeleteVoteSubmissionById/{id}")]
        public ActionResult DeleteVoteSubmissionById(string id)
        {
            var filter = Builders<VoteSubmission>.Filter.Eq("_id", id);

            Context.votesubmission.DeleteOne(filter);

            return Json("OK");
        }

        [HttpGet("GetSessionVariables")]
        public ActionResult GetSessionVariables()
        {
            string contractNumber = HttpContext.Session.GetString("contractNumber");
            string account = HttpContext.Session.GetString("account");

            var dynamicOutput = new ExpandoObject() as IDictionary<string, Object>;
            dynamicOutput.Add("contractNumber", HttpContext.Session.GetString("contractNumber").ToString());
            dynamicOutput.Add("voteTokenAddress", HttpContext.Session.GetString("voteTokenAddress").ToString());
            dynamicOutput.Add("voteTokenName", HttpContext.Session.GetString("voteTokenName").ToString());
            dynamicOutput.Add("voteTokenSymbol", HttpContext.Session.GetString("voteTokenSymbol").ToString());
            dynamicOutput.Add("voteTokenDecimal", HttpContext.Session.GetString("voteTokenDecimal").ToString());
            dynamicOutput.Add("account", HttpContext.Session.GetString("account").ToString());
            dynamicOutput.Add("userBalance", HttpContext.Session.GetString("userBalance").ToString());
            dynamicOutput.Add("questions", Newtonsoft.Json.JsonConvert.SerializeObject(HttpContext.Session.GetString("questions")));
            dynamicOutput.Add("voteSessionId", HttpContext.Session.GetString("voteSessionId").ToString());
            dynamicOutput.Add("voteAnswerChoices", HttpContext.Session.GetString("voteAnswerChoices").ToString());
            dynamicOutput.Add("transactionId", HttpContext.Session.GetString("transactionId").ToString());

            List<VoteSubmission> voteSubmissions = Context.votesubmission.AsQueryable().Where(vs => vs.contractNumber == contractNumber && vs.account == account && vs.voteSubmissionStatus == VoteSubmissionStatus.VotesConfirmed).ToList();
            if (voteSubmissions.Any())
            {
                dynamicOutput.Add("voteSubmission", voteSubmissions.FirstOrDefault());
            }
            else
            {
                dynamicOutput.Add("voteSubmission", "");
            }


            return Json(dynamicOutput);
        }

        [HttpPost("SubmitVotes")]
        public ActionResult SubmitVotes([FromBody]Newtonsoft.Json.Linq.JArray SubmittedVotes)
        {
            string contractNumber = HttpContext.Session.GetString("contractNumber");
            string account = HttpContext.Session.GetString("account");
            List<Question> questions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Question>>(HttpContext.Session.GetString("questions"));
            List<QuestionVM> selectedVotes = (from q in questions                                                
                                                select new QuestionVM
                                                {
                                                    quid = q.quid,
                                                    text = q.text,
                                                    text_ru = q.text_ru,
                                                    boardRecommendation = q.boardRecommendation,
                                                    questionIndex = q.questionIndex,
                                                    keyid = q.quid + "|"
                                                }
                                                ).ToList();
            selectedVotes.ForEach(sv => sv.orderNum = Convert.ToInt32(sv.questionIndex)+1);

            string voteString = "";
            for (int x = 0; x < SubmittedVotes.Count(); x++)
            {
                string quid = SubmittedVotes[x]["quid"].ToString();
                string selectedAnswerId = SubmittedVotes[x]["selectedAnswerId"].ToString();
                var questionVote = selectedVotes.Where(v => v.quid == quid);
                if (questionVote.Any())
                {
                    selectedVotes.FirstOrDefault(v => v.quid == quid).SelectedAnswerId = selectedAnswerId;
                    voteString += selectedAnswerId;
                }
            }

            VoteSubmission voteSubmission = new VoteSubmission();
            voteSubmission.account = HttpContext.Session.GetString("account");
            voteSubmission.contractNumber = HttpContext.Session.GetString("contractNumber");
            voteSubmission.VoteSelections = selectedVotes;
            voteSubmission.voteString = voteString;
            voteSubmission.voteSubmissionStatus = VoteSubmissionStatus.VotesSubmitted;

            Context.votesubmission.InsertOne(voteSubmission);

            return Json(voteSubmission._id.ToString());            
        }

        [HttpGet("GetVoteSubmissionStatus/{id}")]
        public ActionResult GetVoteSubmissionStatus(string id)
        {
            VoteSubmission voteSubmission = Context.votesubmission.AsQueryable().ToList().FirstOrDefault(vs => vs._id == id);
            List<QuestionVM> voteSelections = voteSubmission.VoteSelections;
            QuestionVM lastQuestion = voteSelections[voteSelections.Count() - 1];
            string contractNumber = voteSubmission.contractNumber;
            string account = voteSubmission.account;
            List<BlockchainAddress> blockchainAddresses = Context.contractBlockchainAddresses.AsQueryable().Where(bca=>bca.contractNumber==contractNumber).FirstOrDefault().blockchainAddreses.AsQueryable().Where(bc => bc.contractNumber == contractNumber && bc.account == account && bc.quid == lastQuestion.quid && bc.currentTransaction == true && bc.isFirstTransaction == false).ToList();

            if (blockchainAddresses.Any())
            {
                string transactionId = blockchainAddresses.FirstOrDefault().transactionId;
                string url = "http://192.81.216.69:2750/tx/" + transactionId;
                var webGet = new HtmlWeb();
                HtmlAgilityPack.HtmlDocument document = webGet.Load(url);
                if (document.DocumentNode.InnerText.ToUpper().Contains("HASH:"))
                {
                    voteSubmission.blockChainStatus = "Accepted";
                }
                else
                {
                    voteSubmission.blockChainStatus = "NotAccepted";
                }
            }
            else
            {
                voteSubmission.blockChainStatus = "NotAccepted";
            }

            return Json(voteSubmission);
        }

        [HttpGet("GetCurrentVoteSubmissionId")]
        public ActionResult GetCurrentVoteSubmissionId()
        {
            VoteSubmissionSatusVM voteSubmissionStatusVM = new VoteSubmissionSatusVM();
            string contractNumber = HttpContext.Session.GetString("contractNumber");
            string account = HttpContext.Session.GetString("account");
            List<VoteSubmission> voteSubmissions = Context.votesubmission.AsQueryable().Where(vs => vs.contractNumber == contractNumber && vs.account == account && vs.voteSubmissionStatus == VoteSubmissionStatus.VoteCoinsPendingTransfer).ToList();
            if (voteSubmissions.Any())
            {
                voteSubmissionStatusVM.id = voteSubmissions.FirstOrDefault()._id;
            }
            else
            {
                voteSubmissionStatusVM.id = "";
            }

            List<VoteSubmission> voteSubmissions1 = Context.votesubmission.AsQueryable().Where(vs => vs.contractNumber == contractNumber && vs.account == account).ToList();
            if (voteSubmissions1.Any())
            {
                switch (voteSubmissions1.FirstOrDefault().voteSubmissionStatus)
                {
                    case VoteSubmissionStatus.VoteCoinsPendingTransfer:
                        voteSubmissionStatusVM.voteSubmissionStatus = "Votes Pending";
                        break;
                    case VoteSubmissionStatus.VotesSubmitted:
                        voteSubmissionStatusVM.voteSubmissionStatus = "No Votes Submitted";
                        break;
                    default:
                        voteSubmissionStatusVM.voteSubmissionStatus = "Submitted Successfully";
                        break;
                }
            }
            else
            {
                voteSubmissionStatusVM.voteSubmissionStatus = "No Votes Submitted";
            }

            return Json(voteSubmissionStatusVM);
        }

        [HttpGet("ConfirmVotes/{id}/{languagePreference}/{transactionId}")]
        public ActionResult ConfirmVotes(string id, string languagePreference, string transactionId)
        {
            VoteSubmission voteSubmission = Context.votesubmission.AsQueryable().ToList().FirstOrDefault(vs => vs._id == id);

            var filter = Builders<VoteSubmission>.Filter.Eq("_id", id);
            var update = Builders<VoteSubmission>.Update.Set("voteSubmissionStatus", VoteSubmissionStatus.VotesConfirmed)
                                                        .Set("dateSubmitted", DateTime.Now)
                                                        .Set("transactionId", transactionId);
            Context.votesubmission.UpdateOneAsync(filter, update);

            VoteSubmissionTransaction voteSubmissionTransaction = new VoteSubmissionTransaction();
            voteSubmissionTransaction.voteSubmissionid = id;
            voteSubmissionTransaction.transactionid = transactionId;
            Context.votesubmissiontransactions.InsertOneAsync(voteSubmissionTransaction);

            return Json(voteSubmission);
        }

        public BlockchainAddress GenerateVanityAddress(string addressPrefix)
        {
            BlockchainAddress newAddress = new BlockchainAddress();
            if (accessBlockChain)
            {
                var command = "sh";
                var argss = "/root/externalApps/BCI-XXX/vanityAddressGenerator.sh " + addressPrefix;

                var processInfo = new ProcessStartInfo();
                processInfo.UseShellExecute = false;
                processInfo.RedirectStandardOutput = true;
                processInfo.FileName = command;   // 'sh' for bash 
                processInfo.Arguments = argss;    // The Script name 
                Console.Write("Before Process is kicked off\n");

                var process = Process.Start(processInfo);   // Start that process.
                int lineNum = 0;
                while (!process.StandardOutput.EndOfStream)
                {
                    string line = process.StandardOutput.ReadLine();
                    Console.Write(lineNum.ToString() + ": " + line + "\n");
                    lineNum += 1;

                    if (line.Length >= 8)
                    {
                        switch (line.Substring(0, 8))
                        {
                            case "Pattern:":
                                newAddress.pattern = line.Replace("Pattern:", "").Trim();
                                break;
                            case "Address:":
                                newAddress.publicAddress = line.Replace("Address:", "").Trim();
                                break;
                            case "Privkey:":
                                newAddress.privateKey = line.Replace("Privkey:", "").Trim();
                                break;
                        }

                    }
                }

                process.WaitForExit();

                _log.LogInformation("Private KEy:" + newAddress.privateKey);
                _log.LogInformation("Public KEy:" + newAddress.publicAddress);
            }
            else
            {
                Console.Write("Before Process is kicked off\n");
                newAddress.pattern = addressPrefix;
                newAddress.publicAddress = addressPrefix + "asdgasdgashashasdfs";
                newAddress.privateKey = "asgdasdghasdhfahasdfh";

                Thread.Sleep(1000);
            }

            return newAddress;
        }

        public string GetBalance(string accountName)
        {
            string line = "";
            if (accessBlockChain)
            {
                _log.LogInformation("Right before GetBalance Execution");
                var command = "sh";
                var argss = "/root/externalApps/BCI-XXX/getBalance.sh " + accountName;

                var processInfo = new ProcessStartInfo();
                processInfo.UseShellExecute = false;
                processInfo.RedirectStandardOutput = true;
                processInfo.FileName = command;   // 'sh' for bash 
                processInfo.Arguments = argss;    // The Script name 
                Console.Write("Before Process is kicked off\n");

                var process = Process.Start(processInfo);   // Start that process.
                int lineNum = 0;
                while (!process.StandardOutput.EndOfStream)
                {
                    line = process.StandardOutput.ReadLine();
                    Console.Write(lineNum.ToString() + ": " + line + "\n");
                    lineNum += 1;
                }

                process.WaitForExit();
                _log.LogInformation("Right after GetBalance Execution");
            }
            else
            {
                Console.Write("Before Process is kicked off\n");

                line = "100";
            }

            return line;
        }

        public void ImportPrivateKey(string privateKey)
        {
            if (accessBlockChain)
            {
                _log.LogInformation("Right Before Import of key");
                var command = "sh";
                var argss = "/root/externalApps/BCI-XXX/importPrivateKey.sh " + privateKey;

                var processInfo = new ProcessStartInfo();
                processInfo.UseShellExecute = false;
                processInfo.RedirectStandardOutput = true;
                processInfo.FileName = command;   // 'sh' for bash 
                processInfo.Arguments = argss;    // The Script name 

                var process = Process.Start(processInfo);   // Start that process.
                int lineNum = 0;
                while (!process.StandardOutput.EndOfStream)
                {
                    string line = process.StandardOutput.ReadLine();
                    Console.Write(lineNum.ToString() + ": " + line + "\n");
                    lineNum += 1;

                }

                process.WaitForExit();

                _log.LogInformation("Right Before Import of key -" + lineNum);
            }
            else
            {
                Console.Write("1 :" + privateKey + " \n");
                Thread.Sleep(2000);
            }
        }

        public string createRawTransactionOneInput(string transactionId, string firstInputIndex, string toPublicAddress, string toAmount, string chgPublicAddress, string chgAmount)
        {
            string returnValue = "";
            if (accessBlockChain)
            {
                var command = "sh";
                var argss = "/root/externalApps/BCI-XXX/createRawTransaction_first_txid_toadd_toamt_chgadd_chgamt.sh " + transactionId + " " + firstInputIndex + " " + toPublicAddress + " " + toAmount + " " + chgPublicAddress + " " + chgAmount;
                _log.LogInformation("Before Coins Send = " + argss);
                var processInfo = new ProcessStartInfo();
                processInfo.UseShellExecute = false;
                processInfo.RedirectStandardOutput = true;
                processInfo.FileName = command;   // 'sh' for bash 
                processInfo.Arguments = argss;    // The Script name 

                var process = Process.Start(processInfo);   // Start that process.
                int lineNum = 0;
                string line = "";
                while (!process.StandardOutput.EndOfStream)
                {
                    line = process.StandardOutput.ReadLine();
                    Console.Write(lineNum.ToString() + ": " + line + "\n");
                    lineNum += 1;

                }

                process.WaitForExit();

                returnValue = line;
                _log.LogInformation("After Coins Send = " + returnValue);
            }
            else
            {
                Console.Write("Sent coinsz\n");
                Thread.Sleep(2000);

                returnValue = "23423asd";
            }

            return returnValue;
        }

        public string createRawTransactionTwoInputs(string firstInputTransactionId, string firstInputIndex, string secondInputTransactionId, string secondInputIndex, string toPublicAddress, string toAmount, string chgPublicAddress, string chgAmount)
        {
            string returnValue = "";
            if (accessBlockChain)
            {
                var command = "sh";
                var argss = "/root/externalApps/BCI-XXX/createRawTransaction_txid_toadd_toamt_chgadd_chgamt.sh " + firstInputTransactionId + " " + firstInputIndex + " " + secondInputTransactionId + " " + secondInputIndex + " " + toPublicAddress + " " + toAmount + " " + chgPublicAddress + " " + chgAmount;
                _log.LogInformation("Before Coins Send = " + argss);
                var processInfo = new ProcessStartInfo();
                processInfo.UseShellExecute = false;
                processInfo.RedirectStandardOutput = true;
                processInfo.FileName = command;   // 'sh' for bash 
                processInfo.Arguments = argss;    // The Script name 

                var process = Process.Start(processInfo);   // Start that process.
                int lineNum = 0;
                string line = "";
                while (!process.StandardOutput.EndOfStream)
                {
                    line = process.StandardOutput.ReadLine();
                    Console.Write(lineNum.ToString() + ": " + line + "\n");
                    lineNum += 1;

                }

                process.WaitForExit();

                returnValue = line;
                _log.LogInformation("After Coins Send = " + returnValue);
            }
            else
            {
                Console.Write("Sent coinsz\n");
                Thread.Sleep(2000);

                returnValue = "23423asd";
            }

            return returnValue;
        }

        public SignedHex signRawTransaction(string hexValue)
        {
            string returnValue = "";
            if (accessBlockChain)
            {
                var command = "sh";
                var argss = "/root/externalApps/BCI-XXX/signRawTransaction.sh " + hexValue;
                _log.LogInformation("Before Coins Send = " + argss);
                var processInfo = new ProcessStartInfo();
                processInfo.UseShellExecute = false;
                processInfo.RedirectStandardOutput = true;
                processInfo.FileName = command;   // 'sh' for bash 
                processInfo.Arguments = argss;    // The Script name 

                var process = Process.Start(processInfo);   // Start that process.
                int lineNum = 0;
                string line = "";
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                while (!process.StandardOutput.EndOfStream)
                {
                    line = process.StandardOutput.ReadLine();
                    Console.Write(lineNum.ToString() + ": " + line + "\n");
                    sb.Append(line);
                    lineNum += 1;

                }

                process.WaitForExit();

                returnValue = sb.ToString();
                _log.LogInformation("After Coins Send = " + returnValue);
            }
            else
            {
                Console.Write("Sent coinsz\n");
                Thread.Sleep(2000);

                returnValue = "{    \"hex\" : \"0100000002b7128951465be95a56e723e8b3b17faff2031dd96a9e35dea922df7c328c99e4000000008b483045022100a6d93a046f127e32461c8c0b4935944db2a0ab39536f207b0f6715522937bf2602207519a477f36a45ac22dbe1172dccb37befaaa001df409595deab1022e6a8e9080141044b8e35273a719261ae8acdc30c500558d1bd2fbf4cda393ab34935ac36947d334e93efaf36416c4e369a4267eec46636b9f101572dafb8be463d8268dfea96aaffffffffb7128951465be95a56e723e8b3b17faff2031dd96a9e35dea922df7c328c99e4010000008a47304402204b0714218e0f3a21500ab7966aeb7803ec0c68299b254784f179bbe36676dffd0220729271845b25bdac65a357ca253577340b78bfad456c98cd16948aaaa38708780141049bb3677a824d7920226e14d5c1c673b8851407b4e6a56c69b164d3fb45ab0b12bea32d938700eb7911c17a28b8c6dfa550ee99a8f8b25bbb07a7fdce56110287ffffffff0200f90295000000001976a914c0ccab330983a9153a2050fd1b461abec00019f688ace057fb94000000001976a914c266e3a93c095352ffc9785a8a7a028fd05ef96888ac00000000\",    \"complete\" : true}";
            }

            return Newtonsoft.Json.JsonConvert.DeserializeObject<SignedHex>(returnValue);
        }

        public string sendRawTransaction(string signedHexValue)
        {
            string returnValue = "";
            if (accessBlockChain)
            {
                var command = "sh";
                var argss = "/root/externalApps/BCI-XXX/sendRawTransaction.sh " + signedHexValue;
                _log.LogInformation("Before Coins Send = " + argss);
                var processInfo = new ProcessStartInfo();
                processInfo.UseShellExecute = false;
                processInfo.RedirectStandardOutput = true;
                processInfo.FileName = command;   // 'sh' for bash 
                processInfo.Arguments = argss;    // The Script name 

                var process = Process.Start(processInfo);   // Start that process.
                int lineNum = 0;
                string line = "";
                while (!process.StandardOutput.EndOfStream)
                {
                    line = process.StandardOutput.ReadLine();
                    Console.Write(lineNum.ToString() + ": " + line + "\n");
                    lineNum += 1;

                }

                process.WaitForExit();

                returnValue = line;
                _log.LogInformation("After Coins Send = " + returnValue);
            }
            else
            {
                Console.Write("Sent coinsz\n");
                Thread.Sleep(2000);

                returnValue = "23423asd";
            }

            return returnValue;
        }

        public string sendCoinsFromAccount(string fromAccountName, string toPublicAddress, string amount)
        {
            string returnValue = "";
            if (accessBlockChain)
            {
                var command = "sh";
                var argss = "/root/externalApps/BCI-XXX/sendCoinsFromAccount.sh " + fromAccountName + " " + toPublicAddress + " " + amount;
                _log.LogInformation("Before Coins Send = " + argss);
                var processInfo = new ProcessStartInfo();
                processInfo.UseShellExecute = false;
                processInfo.RedirectStandardOutput = true;
                processInfo.FileName = command;   // 'sh' for bash 
                processInfo.Arguments = argss;    // The Script name 

                var process = Process.Start(processInfo);   // Start that process.
                int lineNum = 0;
                string line = "";
                while (!process.StandardOutput.EndOfStream)
                {
                    line = process.StandardOutput.ReadLine();
                    Console.Write(lineNum.ToString() + ": " + line + "\n");
                    lineNum += 1;

                }

                process.WaitForExit();

                returnValue = line;
                _log.LogInformation("After Coins Send = " + returnValue);
            }
            else
            {
                Console.Write("Sent coinsz\n");
                Thread.Sleep(2000);

                returnValue = "23423asd";
            }

            return returnValue;
        }



        [HttpGet("_GetCompletedQuestions")]
        public ActionResult _GetCompletedQuestions()
        {
            try
            {
                string contractNumber = HttpContext.Session.GetString("contractNumber");

                List<Question> questions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Question>>(HttpContext.Session.GetString("questions"));
                List<Answer> answers = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Answer>>(HttpContext.Session.GetString("answers"));
                List<Question> completedQuestions = questions.Where(q => q.state == 2).ToList();

                foreach (Question question in completedQuestions)
                {
                    decimal allAnswerVotesTotal = (from a in answers
                                                   where a.quid == question.quid
                                                   select a.TotalVotes).Sum();

                    var winningAnswer = (from a in answers
                                         where a.quid == question.quid
                                         orderby a.TotalVotes descending
                                         select new
                                         {
                                             a.answid,
                                             AnswerText = a.test.ToUpper(),
                                             a.TotalVotes
                                         }).First();

                    question.WinningAnswer = winningAnswer.AnswerText;
                    switch (winningAnswer.AnswerText.ToUpper())
                    {
                        case "FOR":
                            question.WinningAnswer_ru = "за";
                            break;
                        case "AGAINST":
                            question.WinningAnswer_ru = "против";
                            break;
                        default:
                            question.WinningAnswer_ru = "воздержался";
                            break;
                    }

                    question.text = question.text.ToUpper();
                    question.WinningPercentage = (Convert.ToDecimal(winningAnswer.TotalVotes) / allAnswerVotesTotal).ToString("0.0%");

                }

                //var answersWinnerTotal = (from a in answers
                //                    group a by a.quid into q
                //                    select new { q.First().quid,
                //                                TotalVotes = q.Max(e=>e.TotalVotes) }).ToList();



                //var completedQuestions = (from q in questions
                //                                where q.state == 2
                //                                select new
                //                                {
                //                                    quid = q.quid,
                //                                    text = q.text,
                //                                    block = q.block,
                //                                    keyid = q.quid + "|"
                //                                }
                //                                                   ).ToList();

                return Json(completedQuestions);

            }
            catch (Exception ex)
            {
            }

            return View();


        }

        [HttpPost("_GetCompletedQuestions_Realtime")]
        public ActionResult _GetCompletedQuestions_Realtime([FromBody]Newtonsoft.Json.Linq.JArray voteCollections)
        {
            try
            {
                var builder = new ConfigurationBuilder()
                     .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json");

                IConfigurationRoot Configuration = builder.Build();

                decimal coinWeight = Convert.ToDecimal(Configuration["coinWeight"]);
                List<Question> questions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Question>>(HttpContext.Session.GetString("questions")).OrderBy(q=>q.questionIndex).ToList();
                List<Answer> answersContext = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Answer>>(HttpContext.Session.GetString("answers"));
                string contractNumber = HttpContext.Session.GetString("contractNumber").ToString();
                List<BlockchainAddress> blockchainAddresses;
                if (voteCollections[0]["index"].ToString() != "")
                {
                    List<IDictionary<string, Object>> lstRawResults = new List<IDictionary<string, object>>();
                    for (int x = 0; x < voteCollections.Count(); x++)
                    {
                        string index = voteCollections[x]["index"].ToString();
                        string senderAddress = voteCollections[x]["senderAddress"].ToString();
                        string sessionId = voteCollections[x]["sessionId"].ToString();
                        string voteSelections = voteCollections[x]["voteSelections"].ToString();
                        string blockNumber = voteCollections[x]["blockNumber"].ToString();
                        string balance = voteCollections[x]["balance"].ToString();

                        var dynamicObjectVoteResults = new ExpandoObject() as IDictionary<string, Object>;
                        dynamicObjectVoteResults.Add("index", index);
                        dynamicObjectVoteResults.Add("senderAddress", senderAddress);
                        dynamicObjectVoteResults.Add("sessionId", sessionId);
                        dynamicObjectVoteResults.Add("blockNumber", blockNumber);
                        dynamicObjectVoteResults.Add("balance", Convert.ToInt32(balance));

                        for (int i = 0; i < questions.Count(); i++)
                        {
                            dynamicObjectVoteResults.Add(questions[i].quid, voteSelections.Substring(i, 1));
                        }
                        lstRawResults.Add(dynamicObjectVoteResults);
                    }

                    blockchainAddresses = new List<BlockchainAddress>();

                    foreach (Question question in questions)
                    {
                        List<Answer> rawAnswers = answersContext.Where(a => a.quid == question.quid).ToList();
                        foreach (Answer answer in rawAnswers)
                        {
                            var subQuery = lstRawResults.Where(rr => rr[question.quid].ToString() == answer.answid).ToList();
                            foreach (IDictionary<string, object> rawVoter in subQuery)
                            {
                                BlockchainAddress blockChainAddress = new BlockchainAddress();
                                blockChainAddress.quid = question.quid;
                                blockChainAddress.ansid = answer.answid;
                                blockChainAddress.account = rawVoter["senderAddress"].ToString();
                                blockChainAddress.publicAddress = rawVoter["senderAddress"].ToString();
                                blockChainAddress.blockNumber = rawVoter["blockNumber"].ToString();
                                blockChainAddress.coins = rawVoter["balance"].ToString();
                                blockChainAddress.contractNumber = contractNumber;
                                if (blockChainAddress.coins != "0")
                                {
                                    blockchainAddresses.Add(blockChainAddress);
                                }
                            }
                        }
                    }
                    ContractBlockchainAddresses contractBlockchainAddresses = new ContractBlockchainAddresses();
                    contractBlockchainAddresses.contractNumber = contractNumber;
                    contractBlockchainAddresses.blockchainAddreses = blockchainAddresses;
                    contractBlockchainAddresses.lastModifiedDatetime = DateTime.Now.ToString();
                    var filter = Builders<ContractBlockchainAddresses>.Filter.Eq("contractNumber", contractNumber);
                    Context.contractBlockchainAddresses.DeleteMany(filter);
                    Context.contractBlockchainAddresses.InsertOne(contractBlockchainAddresses);
                }
                else
                {
                    blockchainAddresses = Context.contractBlockchainAddresses.AsQueryable().Where(bc => bc.contractNumber == contractNumber).FirstOrDefault().blockchainAddreses;
                }

                string account = HttpContext.Session.GetString("account");
                blockchainAddresses.ForEach(bc => bc.TotalVotes = Convert.ToDecimal(bc.coins) / coinWeight);
                blockchainAddresses.ForEach(bc => bc.totalCoins = Convert.ToDecimal(bc.coins));
                var voteCalculation = (from bc in blockchainAddresses
                                       group bc by new { bc.quid, bc.ansid }
                                       into grp
                                       select new
                                       {
                                           quid = grp.Key.quid,
                                           ansid = grp.Key.ansid,
                                           TotalVotes = grp.Sum(t => t.TotalVotes),
                                           totalCoins = grp.Sum(t => t.totalCoins)
                                       }).ToList();

                List<Question> completedQuestions = questions.ToList();
                foreach (Question question in completedQuestions)
                {
                    var answers = (from a1 in answersContext
                                   join a2 in voteCalculation on a1.answid equals a2.ansid into a3
                                   from a2 in a3.DefaultIfEmpty()
                                   where a1.quid == question.quid && (a2 == null || a2.quid == question.quid)
                                   select new
                                   {
                                       quid = a1.quid,
                                       answid = a1.answid,
                                       test = a1.test,
                                       TotalVotes = (a2 == null) ? 0 : a2.TotalVotes,
                                       totalCoins = (a2 == null) ? 0 : a2.totalCoins
                                   }).ToList();

                    decimal allAnswerVotesTotal = (from a in answers
                                                   where a.quid == question.quid
                                                   select a.TotalVotes).Sum();

                    var winningAnswer = (from a in answers
                                         where a.quid == question.quid
                                         orderby a.TotalVotes descending
                                         select new
                                         {
                                             a.answid,
                                             AnswerText = a.test.ToUpper(),
                                             a.TotalVotes,
                                             a.totalCoins
                                         }).First();



                    question.WinningAnswer = winningAnswer.AnswerText;
                    switch (winningAnswer.AnswerText.ToUpper())
                    {
                        case "FOR":
                            question.WinningAnswer_ru = "за";
                            break;
                        case "AGAINST":
                            question.WinningAnswer_ru = "против";
                            break;
                        default:
                            question.WinningAnswer_ru = "воздержался";
                            break;
                    }

                    List<BlockchainAddress> proxyChouce = Context.contractBlockchainAddresses.AsQueryable().Where(bca => bca.contractNumber == contractNumber).FirstOrDefault().blockchainAddreses.AsQueryable().Where(bc => bc.contractNumber == contractNumber && bc.account == account && bc.quid == question.quid).ToList();
                    if (proxyChouce.Any())
                    {
                        switch (proxyChouce.FirstOrDefault().ansid.ToUpper())
                        {
                            case "A":
                                question.ProxyChoice = "FOR";
                                question.ProxyChoice_ru = "за";
                                break;
                            case "B":
                                question.ProxyChoice = "AGAINST";
                                question.ProxyChoice_ru = "против";
                                break;
                            default:
                                question.ProxyChoice = "ABSTAIN";
                                question.ProxyChoice_ru = "воздержался";
                                break;
                        }

                    }
                    else
                    {
                        question.ProxyChoice = "N/A";
                        question.ProxyChoice_ru = "N/A";
                    }
                    question.text = question.text.ToUpper();
                    question.WinningPercentage = (Convert.ToDecimal(winningAnswer.TotalVotes) / allAnswerVotesTotal).ToString("0.0%");

                }
                completedQuestions.ForEach(q => q.questionIndex = (Convert.ToInt32(q.questionIndex) + 1).ToString());
                completedQuestions = completedQuestions.OrderBy(q => q.questionIndex).ToList();
                return Json(completedQuestions);
            }
            catch (Exception ex)
            {
            }

            return View();


        }

        [HttpGet("_GetAnswerInformation/{quid}")]
        public ActionResult _GetAnswerInformation(string quid)
        {
            string contractNumber = HttpContext.Session.GetString("contractNumber");
            List<Question> questions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Question>>(HttpContext.Session.GetString("questions"));
            var question = (from q in questions
                            where q.quid == quid
                            select new
                            {
                                questionText = q.text,
                                questionText_ru = q.text_ru
                            }).FirstOrDefault();
            string questionText = question.questionText;
            string questionText_ru = question.questionText_ru;

            List<Answer> answers = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Answer>>(HttpContext.Session.GetString("answers"));

            List<AnswerPieChartVM> answersPieChartResult = (from a in answers
                                                            where a.quid == quid
                                                            orderby a.answid
                                                            select new AnswerPieChartVM
                                                            {
                                                                keyvalue = a.quid + "/" + a.answid + "|",
                                                                quid = a.quid,
                                                                answid = a.answid,
                                                                text = a.test,
                                                                TotalVotes = a.TotalVotes,
                                                                Value = a.TotalVotes,
                                                                Category = a.answid + ") " + a.test
                                                            }
                                 ).ToList();

            answersPieChartResult.ForEach(a => a.text_ru = (a.text.ToUpper().Trim() == "FOR") ? "за" : ((a.text.ToUpper().Trim() == "AGAINST") ? "против" : "воздержался"));

            List<AnswerBarSchemaVM> answersBarChartSchema = (from a in answers
                                                             where a.quid == quid
                                                             orderby a.answid
                                                             select new AnswerBarSchemaVM
                                                             {
                                                                 name = a.answid + ")",
                                                                 field = a.answid.ToLower(),
                                                                 quid = a.quid,
                                                                 answid = a.answid,
                                                                 text = a.test,
                                                                 TotalVotes = a.TotalVotes,
                                                             }
                                 ).ToList();

            var dynamicObjectAnswerFinalOutput = new ExpandoObject() as IDictionary<string, Object>;
            dynamicObjectAnswerFinalOutput.Add("PieChartData", answersPieChartResult);
            dynamicObjectAnswerFinalOutput.Add("BarChartSchema", answersBarChartSchema);
            dynamicObjectAnswerFinalOutput.Add("BarChartData", answersBarChartSchema);
            dynamicObjectAnswerFinalOutput.Add("quid", quid);
            dynamicObjectAnswerFinalOutput.Add("questionText", questionText);
            dynamicObjectAnswerFinalOutput.Add("questionText_ru", questionText_ru);

            return Json(dynamicObjectAnswerFinalOutput);
        }

        [HttpGet("_GetAnswerInformation_Realtime/{quid}")]
        public ActionResult _GetAnswerInformation_Realtime(string quid)
        {
            var builder = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");

            IConfigurationRoot Configuration = builder.Build();

            //decimal surcharge = Convert.ToDecimal(Configuration["surcharge"]);
            decimal coinWeight = Convert.ToDecimal(Configuration["coinWeight"]);


            string contractNumber = HttpContext.Session.GetString("contractNumber");
            List<Question> questions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Question>>(HttpContext.Session.GetString("questions"));
            var question = (from q in questions
                            where q.quid == quid
                            select new
                            {
                                questionIndex = q.questionIndex,
                                questionText = q.text,
                                questionText_ru = q.text_ru
                            }).FirstOrDefault();
            string questionText = string.Format("{0}: {1}", (Convert.ToInt32(question.questionIndex)+1).ToString(), question.questionText);
            string questionText_ru = question.questionText_ru;

            List<BlockchainAddress> blockchainAddresses = Context.contractBlockchainAddresses.AsQueryable().Where(bca => bca.contractNumber == contractNumber).FirstOrDefault().blockchainAddreses.AsQueryable().Where(bc => bc.contractNumber == contractNumber && bc.quid == quid).ToList();
            blockchainAddresses.ForEach(bc => bc.TotalVotes = Convert.ToDecimal(bc.coins) / coinWeight);
            blockchainAddresses.ForEach(bc => bc.totalCoins = Convert.ToDecimal(bc.coins));
            var voteCalculation = (from bc in blockchainAddresses
                                   group bc by new { bc.quid, bc.ansid }
                                   into grp
                                   select new
                                   {
                                       quid = grp.Key.quid,
                                       ansid = grp.Key.ansid,
                                       TotalVotes = grp.Sum(t => t.TotalVotes),
                                       totalCoins = grp.Sum(t => t.totalCoins)
                                   }).ToList();

            //IMongoQueryable<Answer> answers = Context.answers.AsQueryable();
            List<Answer> answersContext = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Answer>>(HttpContext.Session.GetString("answers"));
            var answers = (from a1 in answersContext
                           join a2 in voteCalculation on a1.answid equals a2.ansid into a3
                           from a2 in a3.DefaultIfEmpty()
                           where a1.quid == quid && (a2 == null || a2.quid == quid)
                           select new
                           {
                               quid = a1.quid,
                               answid = a1.answid,
                               test = a1.test,
                               TotalVotes = (a2 == null) ? 0 : a2.TotalVotes,
                               TotalCoins = (a2 == null) ? 0 : a2.totalCoins
                           }).ToList();

            List<AnswerPieChartVM> answersPieChartResult = (from a in answers
                                                            where a.quid == quid
                                                            orderby a.answid
                                                            select new AnswerPieChartVM
                                                            {
                                                                keyvalue = a.quid + "/" + a.answid + "|",
                                                                quid = a.quid,
                                                                answid = a.answid,
                                                                text = a.test,
                                                                TotalVotes = a.TotalVotes,
                                                                totalCoins = a.TotalCoins,
                                                                Value = a.TotalVotes,
                                                                Category = a.answid + ") " + a.test
                                                            }
                                 ).ToList();

            answersPieChartResult.ForEach(a => a.text_ru = (a.text.ToUpper().Trim() == "FOR") ? "за" : ((a.text.ToUpper().Trim() == "AGAINST") ? "против" : "воздержался"));

            List<AnswerBarSchemaVM> answersBarChartSchema = (from a in answers
                                                             where a.quid == quid
                                                             orderby a.answid
                                                             select new AnswerBarSchemaVM
                                                             {
                                                                 name = a.answid + ")",
                                                                 field = a.answid.ToLower(),
                                                                 quid = a.quid,
                                                                 answid = a.answid,
                                                                 text = a.test,
                                                                 TotalVotes = a.TotalVotes,
                                                             }
                                 ).ToList();

            var dynamicObjectAnswerFinalOutput = new ExpandoObject() as IDictionary<string, Object>;
            List<IDictionary<string, Object>> dynamicObjAnsers = new List<IDictionary<string, object>>();

            dynamicObjectAnswerFinalOutput.Add("PieChartData", answersPieChartResult);
            dynamicObjectAnswerFinalOutput.Add("BarChartSchema", answersBarChartSchema);
            dynamicObjectAnswerFinalOutput.Add("BarChartData", answersBarChartSchema);
            dynamicObjectAnswerFinalOutput.Add("quid", quid);
            dynamicObjectAnswerFinalOutput.Add("questionText", questionText);
            dynamicObjectAnswerFinalOutput.Add("questionText_ru", questionText_ru);

            return Json(dynamicObjectAnswerFinalOutput);
        }

        [HttpGet("_GetAnswerInformation/{quid}/{ansid}")]
        public ActionResult _GetAnswerInformation(string quid, string ansid)
        {
            string contractNumber = HttpContext.Session.GetString("contractNumber");
            List<Question> questions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Question>>(HttpContext.Session.GetString("questions"));
            Question questionDetails = questions.AsQueryable().Where(q => q.quid == quid).FirstOrDefault();

            List<Answer> answers = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Answer>>(HttpContext.Session.GetString("answers"));

            Answer answerDocument = answers.AsQueryable().Where(a => a.quid == quid && a.answid == ansid).FirstOrDefault();

            var answerDetails = new { ansid = answerDocument.answid, description = answerDocument.test, totalVotes = answerDocument.TotalVotes };
            int counter = 1;
            var addresses = (from addr in answerDocument.Addresses
                             select new
                             {
                                 keyvalue = addr.AddressValue + "|",
                                 id = counter++,
                                 address = addr.AddressValue,
                                 totalVotes = addr.TotalVotes
                             }).ToList();

            var dynamicObjectAnswerFinalOutput = new ExpandoObject() as IDictionary<string, Object>;
            List<IDictionary<string, Object>> dynamicObjAnsers = new List<IDictionary<string, object>>();

            dynamicObjectAnswerFinalOutput.Add("questionDetails", questionDetails);
            dynamicObjectAnswerFinalOutput.Add("answerDetails", answerDetails);
            dynamicObjectAnswerFinalOutput.Add("addresses", addresses);


            return Json(dynamicObjectAnswerFinalOutput);
        }

        [HttpGet("_GetAnswerInformation_Realtime/{quid}/{ansid}")]
        public ActionResult _GetAnswerInformation_Realtime(string quid, string ansid)
        {
            var builder = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            IConfigurationRoot Configuration = builder.Build();

            //decimal surcharge = Convert.ToDecimal(Configuration["surcharge"]);
            decimal coinWeight = Convert.ToDecimal(Configuration["coinWeight"]);

            string contractNumber = HttpContext.Session.GetString("contractNumber");
            List<Question> questions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Question>>(HttpContext.Session.GetString("questions"));
            Question questionDetails = questions.AsQueryable().Where(q => q.quid == quid).FirstOrDefault();

            List<BlockchainAddress> blockchainAddresses = Context.contractBlockchainAddresses.AsQueryable().Where(bca => bca.contractNumber == contractNumber).FirstOrDefault().blockchainAddreses.AsQueryable().Where(bc => bc.contractNumber == contractNumber && bc.quid == quid && bc.ansid == ansid).ToList();
            blockchainAddresses.ForEach(bc => bc.TotalVotes = Convert.ToDecimal(bc.coins) / coinWeight);
            blockchainAddresses.ForEach(bc => bc.totalCoins = Convert.ToDecimal(bc.coins));
            var voteCalculation = (from bc in blockchainAddresses
                                   group bc by new { bc.quid, bc.ansid }
                                   into grp
                                   select new
                                   {
                                       quid = grp.Key.quid,
                                       ansid = grp.Key.ansid,
                                       TotalVotes = grp.Sum(t => t.TotalVotes),
                                       totalCoins = grp.Sum(t => t.totalCoins)
                                   }).ToList();
            List<Answer> answersContext = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Answer>>(HttpContext.Session.GetString("answers"));
            var answers = (from a1 in answersContext
                           join a2 in voteCalculation on a1.answid equals a2.ansid into a3
                           from a2 in a3.DefaultIfEmpty()
                           where a1.quid == quid && (a2 == null || a2.quid == quid)
                           select new
                           {
                               quid = a1.quid,
                               answid = a1.answid,
                               test = a1.test,
                               TotalVotes = (a2 == null) ? 0 : a2.TotalVotes,
                               TotalCoins = (a2 == null) ? 0 : a2.totalCoins
                           }).ToList();
            //IMongoQueryable<Answer> answers = Context.answers.AsQueryable();

            var answerDocument = answers.AsQueryable().Where(a => a.quid == quid && a.answid == ansid).FirstOrDefault();

            var answerDetails = new { ansid = answerDocument.answid, description = answerDocument.test, totalVotes = answerDocument.TotalVotes };
            int counter = 1;
            var addresses = (from addr in blockchainAddresses
                             select new
                             {
                                 keyvalue = addr.blockNumber + "|",
                                 id = counter++,
                                 address = addr.publicAddress,
                                 blockNumber = addr.blockNumber,
                                 totalVotes = addr.TotalVotes,
                                 totalCoins = addr.totalCoins
                             }).ToList();

            var dynamicObjectAnswerFinalOutput = new ExpandoObject() as IDictionary<string, Object>;
            List<IDictionary<string, Object>> dynamicObjAnsers = new List<IDictionary<string, object>>();

            dynamicObjectAnswerFinalOutput.Add("questionDetails", questionDetails);
            dynamicObjectAnswerFinalOutput.Add("answerDetails", answerDetails);
            dynamicObjectAnswerFinalOutput.Add("addresses", addresses);


            return Json(dynamicObjectAnswerFinalOutput);
        }

        [HttpGet("_GetAnswersBarChartData/{quid}")]
        public ActionResult _GetAnswersBarChartData(string quid)
        {
            List<Answer> answers = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Answer>>(HttpContext.Session.GetString("answers"));

            List<IDictionary<string, Object>> finalResult = new List<IDictionary<string, object>>();
            var dynamicObject = new ExpandoObject() as IDictionary<string, Object>;
            dynamicObject.Add("year", "2018");
            List<AnswerPieChartVM> answersPieChartResult = (from a in answers
                                                            where a.quid == quid
                                                            orderby a.answid
                                                            select new AnswerPieChartVM
                                                            {
                                                                quid = a.quid,
                                                                answid = a.answid,
                                                                text = a.test,
                                                                TotalVotes = a.TotalVotes,
                                                                Value = a.TotalVotes,
                                                                Category = a.answid + ") " + a.test
                                                            }
                                 ).ToList();
            foreach (AnswerPieChartVM answer in answersPieChartResult)
            {
                dynamicObject.Add(answer.answid.ToLower(), answer.TotalVotes);
            }

            finalResult.Add(dynamicObject);
            return Json(finalResult);
        }

        [HttpGet("_GetAnswersBarChartData_Realtime/{quid}")]
        public ActionResult _GetAnswersBarChartData_Realtime(string quid)
        {
            var builder = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            IConfigurationRoot Configuration = builder.Build();

            //decimal surcharge = Convert.ToDecimal(Configuration["surcharge"]);
            decimal coinWeight = Convert.ToDecimal(Configuration["coinWeight"]);

            string contractNumber = HttpContext.Session.GetString("contractNumber");
            string account = HttpContext.Session.GetString("account");
            List<BlockchainAddress> blockchainAddresses = Context.contractBlockchainAddresses.AsQueryable().Where(bca => bca.contractNumber == contractNumber).FirstOrDefault().blockchainAddreses.AsQueryable().Where(bc => bc.contractNumber == contractNumber && bc.quid == quid).ToList();
            blockchainAddresses.ForEach(bc => bc.TotalVotes = Convert.ToDecimal(bc.coins) / coinWeight);
            blockchainAddresses.ForEach(bc => bc.totalCoins = Convert.ToDecimal(bc.coins));
            var voteCalculation = (from bc in blockchainAddresses
                                   group bc by new { bc.quid, bc.ansid }
                                   into grp
                                   select new
                                   {
                                       quid = grp.Key.quid,
                                       ansid = grp.Key.ansid,
                                       TotalVotes = grp.Sum(t => t.TotalVotes),
                                       totalCoins = grp.Sum(t => t.totalCoins)
                                   }).ToList();

            List<Answer> answersContext = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Answer>>(HttpContext.Session.GetString("answers"));
            var answers = (from a1 in answersContext
                           join a2 in voteCalculation on a1.answid equals a2.ansid into a3
                           from a2 in a3.DefaultIfEmpty()
                           where a1.quid == quid && (a2 == null || a2.quid == quid)
                           select new
                           {
                               quid = a1.quid,
                               answid = a1.answid,
                               test = a1.test,
                               TotalVotes = (a2 == null) ? 0 : a2.TotalVotes,
                               TotalCoins = (a2 == null) ? 0 : a2.totalCoins
                           }).ToList();

            List<IDictionary<string, Object>> finalResult = new List<IDictionary<string, object>>();
            var dynamicObject = new ExpandoObject() as IDictionary<string, Object>;
            dynamicObject.Add("year", "2018");
            List<AnswerPieChartVM> answersPieChartResult = (from a in answers
                                                            where a.quid == quid
                                                            orderby a.answid
                                                            select new AnswerPieChartVM
                                                            {
                                                                quid = a.quid,
                                                                answid = a.answid,
                                                                text = a.test,
                                                                TotalVotes = a.TotalVotes,
                                                                Value = a.TotalVotes,
                                                                Category = a.answid + ") " + a.test
                                                            }
                                 ).ToList();
            foreach (AnswerPieChartVM answer in answersPieChartResult)
            {
                dynamicObject.Add(answer.answid.ToLower(), answer.TotalVotes);
            }

            finalResult.Add(dynamicObject);
            return Json(finalResult);
        }

        [HttpGet("_GetCustodians")]
        public ActionResult _GetCustodians()
        {
            string CustomerId = HttpContext.Session.GetString("CustomerId");


            IMongoQueryable<Custodian> custodians = Context.custodians.AsQueryable();

            return Json(custodians);
        }

        [HttpGet("Accounts/_GetAccounts")]
        public ActionResult _GetAccounts()
        {
            string CustomerId = HttpContext.Session.GetString("CustomerId");

            IMongoQueryable<Account> accounts = Context.accounts.AsQueryable();
            IMongoQueryable<Company> companies = Context.companies.AsQueryable();
            IMongoQueryable<Custodian> custodians = Context.custodians.AsQueryable();
            IMongoQueryable<Customer> customers = Context.customers.AsQueryable();

            List<AccountVM> vmAccounts = (from acc in accounts
                                          join comp in companies on acc.CompanyId equals comp._id
                                          join cstdn in custodians on acc.CustodianId equals cstdn._id
                                          join csmr in customers on acc.CustomerID equals csmr._id
                                          where acc.CustomerID == CustomerId
                                          select new AccountVM
                                          {
                                              _id = acc._id,
                                              AccountNumber = acc.AccountNumber,
                                              CompanyName = comp.CompanyName,
                                              BankName = cstdn.CustodianBankName,
                                              CustomerName = csmr.CustomerName,
                                              AvailableShares = acc.AvailableShares,
                                              CustomerID = acc.CustomerID
                                          }).ToList();

            vmAccounts.ForEach(a => a.KeyValue = string.Format("{0}|", a._id));

            return Json(vmAccounts);
        }

        [HttpGet("Accounts/_GetByGroupId/{GroupId}")]
        public ActionResult _GetAccountsByGroupId(string GroupId)
        {
            string CustomerId = HttpContext.Session.GetString("CustomerId");

            List<Group> groups = Context.groups.AsQueryable().ToList();
            List<Account> accounts = new List<Account>();

            Group group = groups.FirstOrDefault(g => g._id == GroupId);

            if (group.Accounts != null)
            {
                accounts = group.Accounts;
            }
            else
            {
                accounts = new List<Account>();
            }

            List<Company> companies = Context.companies.AsQueryable().ToList();
            List<Custodian> custodians = Context.custodians.AsQueryable().ToList();
            List<Customer> customers = Context.customers.AsQueryable().ToList();

            List<AccountVM> vmAccounts = (from acc in accounts
                                          join comp in companies on acc.CompanyId equals comp._id
                                          join cstdn in custodians on acc.CustodianId equals cstdn._id
                                          join csmr in customers on acc.CustomerID equals csmr._id
                                          where acc.CustomerID == CustomerId
                                          select new AccountVM
                                          {
                                              _id = acc._id,
                                              AccountNumber = acc.AccountNumber,
                                              CompanyName = comp.CompanyName,
                                              BankName = cstdn.CustodianBankName,
                                              CustomerName = csmr.CustomerName,
                                              AvailableShares = acc.AvailableShares,
                                              CustomerID = acc.CustomerID

                                          }).ToList();

            vmAccounts.ForEach(a => a.KeyValue = string.Format("{0}|", a._id));

            return Json(vmAccounts);
        }

        [HttpGet("Groups/AddAccount/{GroupId}/{AccountIds}")]
        public ActionResult Groups_AddAccount(string GroupId, string AccountIds)
        {
            Group group = Context.groups.AsQueryable().FirstOrDefault(g => g._id == GroupId);
            string[] accountId = AccountIds.Split('|');

            for (int x = 0; x < accountId.Length - 1; x++)
            {
                Account account = Context.accounts.AsQueryable().FirstOrDefault(a => a._id == accountId[x].ToString());

                if (group.Accounts == null)
                {
                    group.Accounts = new List<Account>();
                }

                if (!group.Accounts.Any(a => a._id == account._id))
                {
                    group.Accounts.Add(account);
                }

            }


            var filter = Builders<Group>.Filter.Eq("_id", group._id);
            var update = Builders<Group>.Update.Set("Accounts", group.Accounts);

            Context.groups.UpdateOne(filter, update);

            return Json(group);
        }

        [HttpGet("Groups/RemoveAccount/{GroupId}/{AccountIds}")]
        public ActionResult Groups_RemoveAccount(string GroupId, string AccountIds)
        {
            Group group = Context.groups.AsQueryable().FirstOrDefault(g => g._id == GroupId);
            string[] accountId = AccountIds.Split('|');

            for (int x = 0; x < accountId.Length - 1; x++)
            {

                group.Accounts.Remove(group.Accounts.FirstOrDefault(a => a._id == accountId[x]));
            }


            var filter = Builders<Group>.Filter.Eq("_id", group._id);
            var update = Builders<Group>.Update.Set("Accounts", group.Accounts);

            Context.groups.UpdateOne(filter, update);

            return Json(group);
        }


        [HttpGet("_GetAnnouncements")]
        public ActionResult _GetAnnouncements()
        {
            IMongoQueryable<dashboardAnnouncements> announcements = Context.dashboardannouncements.AsQueryable();

            return Json(announcements);
        }

        [HttpGet("_GetNewmeetings")]
        public ActionResult _GetNewmeetings()
        {
            List<dashboardNewmeetings> newmeetings = Context.dashboardnewmeetings.AsQueryable().ToList();

            newmeetings.ForEach(m => m.FullText = string.Format("{0} was added on [{1}] and the next AGM is scheduled for [{2}]", m.Company, m.AddDate.ToString("MMMM d, yyyy"), m.AGMDate.ToString("MMMM d, yyyy")));

            return Json(newmeetings);
        }

        [HttpGet("_GetNewAccounts")]
        public ActionResult _GetNewAccounts()
        {
            List<dashboardNewaccounts> newaccounts = Context.dashboardnewaccounts.AsQueryable().ToList();

            newaccounts.ForEach(m => m.FullText = string.Format("Account [{0}] for [{1}] added with [{2}] shares", m.Account, m.MeetingSymbol, string.Format("{0:n0}", m.NumberOfShares)));

            return Json(newaccounts);
        }

        [HttpGet("_GetUpcomingMeetings")]
        public ActionResult _GetUpcomingMeetings()
        {
            List<dashboardUpcomingmeetings> upcomingmeetings = Context.dashboardupcomingmeetings.AsQueryable().ToList();

            upcomingmeetings.ForEach(m => m.FullText = string.Format("[{0}] {1}", m.Date.ToString("MMM dd"), m.Meeting));

            return Json(upcomingmeetings);
        }

        [HttpGet("_GetMeetings")]
        public ActionResult _GetMeetings()
        {
            IMongoQueryable<Meeting> meetings = Context.meetings.AsQueryable();
            IMongoQueryable<Company> companies = Context.companies.AsQueryable();

            List<Meeting> meetingsVM = (from m in meetings
                                        join c in companies on m.CompanyId equals c._id
                                        select new Meeting
                                        {
                                            _id = m._id,
                                            Entity = m.Entity,
                                            CompanyName = c.CompanyName,
                                            SecurityID = m.SecurityID,
                                            MeetingDate = m.MeetingDate,
                                            VoteDeadline = m.VoteDeadline,
                                            BallotReceptionDate = m.BallotReceptionDate
                                        }).ToList();

            meetingsVM.ForEach(m => m.VoteStatus = (m.CompanyName.Contains("Exxon") || m.CompanyName.Contains("Broadridge")) ? "Voted" : "NotVoted");
            meetingsVM.ForEach(m => m.KeyValue = m._id + "|");

            return Json(meetingsVM);
        }

        [HttpGet("_GetMeetingByEntityAndDate/{EntityName}/{strEventDate}")]
        public ActionResult _GetMeetingByEntityAndDate(string EntityName, string strEventDate)
        {
            IMongoQueryable<Meeting> meetings = Context.meetings.AsQueryable();
            DateTime dtEventDate = Convert.ToDateTime(strEventDate);

            Meeting eventInfo = meetings.Where(e => e.Entity == EntityName && e.MeetingDate == dtEventDate).FirstOrDefault();

            return Json(eventInfo);
        }

        [HttpGet("Groups/_GetGroups")]
        public ActionResult _GetGroups()
        {
            IMongoQueryable<Group> groups = Context.groups.AsQueryable();
            IMongoQueryable<Meeting> meetings = Context.meetings.AsQueryable();


            List<GroupVM> result = (from gps in groups
                                    join mtg in meetings on gps.MeetingId equals mtg._id
                                    select new GroupVM
                                    {
                                        _id = gps._id,
                                        GroupName = gps.GroupName,
                                        MeetingEntity = mtg.Entity
                                    }).ToList();

            result.ForEach(g => g.KeyValue = string.Format("{0}|{1}|", g._id, g.GroupName));

            return Json(result);
        }



        [HttpGet("Groups/Create")]
        public ActionResult Groups_Create()
        {
            GroupVM groupVM = Newtonsoft.Json.JsonConvert.DeserializeObject<List<GroupVM>>(HttpContext.Request.Query["models"].ToString())[0];
            IMongoQueryable<Meeting> meetings = Context.meetings.AsQueryable();

            string meetingId = meetings.FirstOrDefault(m => m.Entity == groupVM.MeetingEntity)._id;

            Group newGroup = new Group();
            newGroup.GroupName = groupVM.GroupName;
            newGroup.MeetingId = meetingId;
            newGroup.Accounts = new List<Account>();

            Context.groups.InsertOne(newGroup);

            groupVM._id = newGroup._id;
            groupVM.KeyValue = string.Format("{0}|{1}|", groupVM._id, groupVM.GroupName);

            return Json(groupVM);
        }

        [HttpGet("Groups/Update")]
        public ActionResult Groups_Update()
        {
            List<GroupVM> groupsVM = Newtonsoft.Json.JsonConvert.DeserializeObject<List<GroupVM>>(HttpContext.Request.Query["models"].ToString());
            IMongoQueryable<Meeting> meetings = Context.meetings.AsQueryable();

            string meetingId = meetings.FirstOrDefault(m => m.Entity == groupsVM[0].MeetingEntity)._id;

            var filter = Builders<Group>.Filter.Eq("_id", groupsVM[0]._id);
            var update = Builders<Group>.Update.Set("GroupName", groupsVM[0].GroupName)
                                                .Set("MeetingId", meetingId);

            Context.groups.UpdateOne(filter, update);

            return Json(groupsVM);
        }

        [HttpGet("Groups/Destroy")]
        public ActionResult Groups_Destroy()
        {
            GroupVM groupVM = Newtonsoft.Json.JsonConvert.DeserializeObject<List<GroupVM>>(HttpContext.Request.Query["models"].ToString())[0];

            var filter = Builders<Group>.Filter.Eq("_id", groupVM._id);

            Context.groups.DeleteOne(filter);

            return Json(groupVM);
        }

        [HttpGet("questions/BTCS")]
        public IActionResult GetQuestions()
        {
            List<Question> questions;


            try
            {
                string contractNumber = HttpContext.Session.GetString("contractNumber");

                questions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Question>>(HttpContext.Session.GetString("questions"));
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }

            return Ok(questions);
        }

        [HttpGet("answers/BTCS")]
        public IActionResult GetAnswers()
        {
            List<Answer> answers = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Answer>>(HttpContext.Session.GetString("answers"));

            return Ok(answers);
        }

        [HttpGet("answers/BTCS/{quid}")]
        public IActionResult GetAnswersForQuestion(string quid)
        {
            try
            {
                List<Answer> answers = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Answer>>(HttpContext.Session.GetString("answers")).Where(a => a.quid == quid).ToList();
                return Ok(answers);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        // GET: /<controller>/
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("Vote/Test2")]
        public IActionResult Test2()
        {
            return View();
        }
    }
}
