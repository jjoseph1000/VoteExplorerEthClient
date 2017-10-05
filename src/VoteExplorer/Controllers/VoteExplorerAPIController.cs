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
        private VoteExplorerBlockchainContext _blockchainContext;
        public static bool accessBlockChain = checkIfAccessBlockChain();
        readonly ILogger<VoteExplorerAPIController> _log;

        public VoteExplorerAPIController(ILogger<VoteExplorerAPIController> log, VoteExplorerBlockchainContext blockchainContext)
        {
            _log = log;
            _blockchainContext = blockchainContext;
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

        [HttpPost("SubmitVotes")]
        public ActionResult SubmitVotes([FromBody]Newtonsoft.Json.Linq.JArray SubmittedVotes)
        {
            string controlNumber = HttpContext.Session.GetString("ControlNumber");
            List<Question> questions = _blockchainContext.questions;
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
            selectedVotes.ForEach(sv => sv.orderNum = Convert.ToInt32(sv.questionIndex) + 1);

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
            voteSubmission.ControlNumber = controlNumber;
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
            string meetingId = voteSubmission.MeetingId;
            string controlNumber = voteSubmission.ControlNumber;
            List<BlockchainAddress> blockchainAddresses = Context.blockchainaddresses.AsQueryable().Where(bc => bc.meetingId == meetingId && bc.controlNumber == controlNumber && bc.quid == lastQuestion.quid && bc.currentTransaction == true && bc.isFirstTransaction == false).ToList();

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
            string meetingId = HttpContext.Session.GetString("activeVoteContractAddress");
            string controlNumber = HttpContext.Session.GetString("ControlNumber");
            List<VoteSubmission> voteSubmissions = Context.votesubmission.AsQueryable().Where(vs => vs.MeetingId == meetingId && vs.ControlNumber == controlNumber && vs.voteSubmissionStatus == VoteSubmissionStatus.VoteCoinsPendingTransfer).ToList();
            if (voteSubmissions.Any())
            {
                voteSubmissionStatusVM.id = voteSubmissions.FirstOrDefault()._id;
            }
            else
            {
                voteSubmissionStatusVM.id = "";
            }

            List<VoteSubmission> voteSubmissions1 = Context.votesubmission.AsQueryable().Where(vs => vs.MeetingId == meetingId && vs.ControlNumber == controlNumber).ToList();
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

        [HttpGet("ConfirmVotes/{id}/{languagePreference}")]
        public ActionResult ConfirmVotes(string id, string languagePreference)
        {
            VoteSubmission voteSubmission = Context.votesubmission.AsQueryable().ToList().FirstOrDefault(vs => vs._id == id);

            var builder2 = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");

            var Configuration = builder2.Build();

            long shareDivision = Convert.ToInt64(Configuration["shareDivision"]);
            string controlNumber = HttpContext.Session.GetString("ControlNumber");
            SHOAccount shoaccount = Context.shoaccounts.AsQueryable().FirstOrDefault(sho => sho.ControlNumber == controlNumber);
            if (shoaccount.maskedVoters == null || shoaccount.maskedVoters.Count() == 0)
            {
                List<VoteMask> maskedVoters = new List<VoteMask>();
                long multipleOfShareDivision = shoaccount.AvailableShares / shareDivision;
                for (int x=0;x<multipleOfShareDivision;x++)
                {
                    VoteMask maskedVoter = new VoteMask();
                    maskedVoter.voterId = System.Guid.NewGuid().ToString("N");
                    maskedVoter.balance = shareDivision;
                    maskedVoters.Add(maskedVoter);
                }

                long remainderShares = shoaccount.AvailableShares % shareDivision;
                if (remainderShares > 0)
                {
                    VoteMask maskedVoter = new VoteMask();
                    maskedVoter.voterId = System.Guid.NewGuid().ToString("N");
                    maskedVoter.balance = remainderShares;
                    maskedVoters.Add(maskedVoter);
                }

                shoaccount.maskedVoters = maskedVoters;
                var filterShoAccount = Builders<SHOAccount>.Filter.Eq("_id", shoaccount._id);
                var updateShoAccount = Builders<SHOAccount>.Update.Set("maskedVoters", maskedVoters);
                Context.shoaccounts.UpdateOne(filterShoAccount, updateShoAccount);
            }


            var filter = Builders<VoteSubmission>.Filter.Eq("_id", id);
            var update = Builders<VoteSubmission>.Update.Set("voteSubmissionStatus", VoteSubmissionStatus.VotesConfirmed)
                                                        .Set("dateSubmitted", DateTime.Now)
                                                        .Set("completePercentage", 0);

            Context.votesubmission.UpdateOneAsync(filter, update);

            BlockchainVoteRequest blockchainVoteRequest = new BlockchainVoteRequest();
            blockchainVoteRequest.ControlNumber = controlNumber;
            blockchainVoteRequest.VoteSubmissionId = id;
            blockchainVoteRequest.dateSubmitted = DateTime.Now;
            blockchainVoteRequest.voteString = voteSubmission.voteString;
            blockchainVoteRequest.maskedVoters = new List<Voter>();
            foreach (VoteMask voteMask in shoaccount.maskedVoters)
            {
                Voter voter = new Voter();
                voter.voterId = voteMask.voterId;
                voter.voteSessionId = id;
                voter.voteAnswers = voteSubmission.voteString;
                voter.balance = voteMask.balance;
                blockchainVoteRequest.maskedVoters.Add(voter);
            }
            blockchainVoteRequest.blockchainVoterRequestStatus = BlockchainVoterRequestStatus.Submitted;
            Context.blockchainvoterequests.InsertOne(blockchainVoteRequest);

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



        public void VoteOnBlockchain(object voteSubmissionId_ControlNumber_MeetingId)
        {
            _tm.Dispose();

            _log.LogInformation("Ran VoteOnBlockchain");
            try
            {
                var parameters = voteSubmissionId_ControlNumber_MeetingId.ToString().Split('_');
                string voteSubmissionId = parameters[0].ToString();
                string controlNumber = parameters[1].ToString();
                string meetingId = parameters[2].ToString();
                string vanityAddressGeneratedMessage = "";
                string privateKeyImportedMessage = "";
                string coinsSentMessage = "";
                bool unsignableTransactionExists = false;
                if (parameters[3].ToString() == "ru")
                {
                    vanityAddressGeneratedMessage = "Создан персонализированный биткоин-адрес";
                    privateKeyImportedMessage = "Приватный ключ внесен";
                    coinsSentMessage = "Монеты высланы на адрес";
                }
                else
                {
                    vanityAddressGeneratedMessage = "Vanity Address Generated";
                    privateKeyImportedMessage = "Private Key Imported";
                    coinsSentMessage = "Coins Sent To Address";
                }

                List<Meeting_SHOAccount> meeting_shoaccounts = Context.meeting_shoaccounts.AsQueryable().Where(m => m.meetingId == meetingId && m.controlNumber == controlNumber).ToList();
                string id = voteSubmissionId.ToString();
                VoteSubmission voteSubmission = Context.votesubmission.AsQueryable().ToList().FirstOrDefault(vs => vs._id == id);

                List<QuestionVM> questions = voteSubmission.VoteSelections;

                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

                IConfigurationRoot Configuration = builder.Build();

                decimal surcharge = Convert.ToDecimal(Configuration["surcharge"]);
                decimal coinWeight = Convert.ToDecimal(Configuration["coinWeight"]);

                //Set Vote Status to Pending
                var filter = Builders<VoteSubmission>.Filter.Eq("_id", id);
                var update = Builders<VoteSubmission>.Update.Set("voteSubmissionStatus", VoteSubmissionStatus.VoteCoinsPendingTransfer)
                                                            .Set("blockChainStage", BlockChainStage.NotProcessed)
                                                            .Set("blockChainProcessingMessage", " ");

                Context.votesubmission.UpdateOne(filter, update);

                SHOAccount shoAccount = Context.shoaccounts.AsQueryable().FirstOrDefault(a => a.ControlNumber == controlNumber);

                //Go through list of questions
                for (int x = 0; x < questions.Count(); x++)
                {
                    QuestionVM question = questions[x];

                    Context.votesubmission.UpdateOne(filter, update);

                    BlockchainAddress generatedAddress = GenerateVanityAddress(string.Format("1{0}{1}", question.quid, question.SelectedAnswerId));

                    update = Builders<VoteSubmission>.Update.Set("currentQuestionBeingProcessedNumber", x + 1)
                                                            .Set("blockChainStage", BlockChainStage.VanityAddressGenerated)
                                                            .Set("blockChainProcessingMessage", vanityAddressGeneratedMessage);

                    Context.votesubmission.UpdateOne(filter, update);

                    Console.Write("PubAddress:" + generatedAddress.publicAddress + "\n");
                    Console.Write("PrivateKey:" + generatedAddress.privateKey + "\n");
                    ImportPrivateKey(generatedAddress.privateKey);
                    update = Builders<VoteSubmission>.Update.Set("blockChainStage", BlockChainStage.PrivateKeyImported)
                                                            .Set("blockChainProcessingMessage", privateKeyImportedMessage);

                    Context.votesubmission.UpdateOne(filter, update);

                    //string fromAccountName = "";
                    //string coinsToSend = "";
                    //string generalFundAccount = string.Format("GeneralFund_{0}", meetingId);

                    //if (meeting_shoaccounts.Any())
                    //{
                    //    fromAccountName = walletAccountName;
                    //}
                    //else
                    //{
                    //    fromAccountName = generalFundAccount;
                    //}
                    //coinsToSend = (shoAccount.AvailableShares * coinWeight).ToString();
                    //string transactionId = sendCoinsFromAccount(fromAccountName, generatedAddress.publicAddress, coinsToSend);
                    List<BlockchainAddress> blockchainaddresses = Context.blockchainaddresses.AsQueryable().Where(bc => bc.meetingId == meetingId && bc.controlNumber == controlNumber && bc.quid == question.quid && bc.currentTransaction == true).ToList();
                    if (blockchainaddresses.Any())
                    {
                        BlockchainAddress prevBlockchainAddress = blockchainaddresses.FirstOrDefault();
                        decimal coinsToSendVote = (shoAccount.AvailableShares * coinWeight);
                        decimal coinsInGeneralFund;
                        string hexValue;
                        SignedHex signedHex = new SignedHex(); ;

                        if (prevBlockchainAddress.isFirstTransaction)
                        {
                            string hexValue1;
                            string hexValue2;
                            coinsInGeneralFund = Convert.ToDecimal(prevBlockchainAddress.coins) - coinsToSendVote - surcharge;
                            hexValue1 = createRawTransactionOneInput(prevBlockchainAddress.transactionId, "1", generatedAddress.publicAddress, coinsToSendVote.ToString(), prevBlockchainAddress.generalFundPublicAddress, coinsInGeneralFund.ToString());
                            SignedHex signedHex1 = signRawTransaction(hexValue1);
                            if (signedHex1.complete == "true")
                            {
                                signedHex = signedHex1;
                            }
                            else
                            {
                                hexValue2 = createRawTransactionOneInput(prevBlockchainAddress.transactionId, "0", generatedAddress.publicAddress, coinsToSendVote.ToString(), prevBlockchainAddress.generalFundPublicAddress, coinsInGeneralFund.ToString());
                                SignedHex signedHex2 = signRawTransaction(hexValue2);
                                if (signedHex2.complete == "true")
                                {
                                    signedHex = signedHex2;
                                }
                                else
                                {
                                    signedHex.complete = "false";
                                }
                            }

                        }
                        else
                        {
                            coinsInGeneralFund = Convert.ToDecimal(prevBlockchainAddress.generalFundCoins) - surcharge;
                            hexValue = createRawTransactionTwoInputs(prevBlockchainAddress.transactionId, "0", prevBlockchainAddress.transactionId, "1", generatedAddress.publicAddress, coinsToSendVote.ToString(), prevBlockchainAddress.generalFundPublicAddress, coinsInGeneralFund.ToString());
                            signedHex = signRawTransaction(hexValue);
                        }

                        if (signedHex.complete == "true")
                        {
                            string transactionId = sendRawTransaction(signedHex.hex);
                            //Clear and Log address and transaction info into database
                            var filterSubmittedAddresses = Builders<BlockchainAddress>.Filter.Eq("meetingId", meetingId) & Builders<BlockchainAddress>.Filter.Eq("controlNumber", controlNumber) &
                                                                Builders<BlockchainAddress>.Filter.Eq("quid", question.quid);
                            var updateSubmittedAddresses = Builders<BlockchainAddress>.Update.Set("currentTransaction", false);

                            Context.blockchainaddresses.UpdateMany(filterSubmittedAddresses, updateSubmittedAddresses);

                            generatedAddress.inputTransactionId = prevBlockchainAddress.transactionId;
                            generatedAddress.generalFundPublicAddress = prevBlockchainAddress.generalFundPublicAddress;
                            generatedAddress.generalFundCoins = coinsInGeneralFund.ToString();
                            generatedAddress.quid = question.quid;
                            generatedAddress.ansid = question.SelectedAnswerId;
                            generatedAddress.meetingId = meetingId;
                            generatedAddress.transactionId = transactionId;
                            generatedAddress.controlNumber = controlNumber;
                            generatedAddress.currentTransaction = true;
                            generatedAddress.isFirstTransaction = false;
                            generatedAddress.coins = coinsToSendVote.ToString();
                            Context.blockchainaddresses.InsertOneAsync(generatedAddress);

                            update = Builders<VoteSubmission>.Update.Set("blockChainStage", BlockChainStage.CoinsSent)
                                                                    .Set("blockChainProcessingMessage", coinsSentMessage);

                            Context.votesubmission.UpdateOne(filter, update);


                        }
                        else
                        {
                            unsignableTransactionExists = true;
                            break;
                        }
                    }
                }

                if (!unsignableTransactionExists)
                {
                    if (!meeting_shoaccounts.Any())
                    {
                        Meeting_SHOAccount meeting_shoaccount = new Meeting_SHOAccount();
                        meeting_shoaccount.controlNumber = controlNumber;
                        meeting_shoaccount.meetingId = meetingId;
                        Context.meeting_shoaccounts.InsertOne(meeting_shoaccount);
                    }

                    update = Builders<VoteSubmission>.Update.Set("voteSubmissionStatus", VoteSubmissionStatus.VoteCoinsTransferred);
                    Context.votesubmission.UpdateOne(filter, update);
                }
                else
                {
                    update = Builders<VoteSubmission>.Update.Set("voteSubmissionStatus", VoteSubmissionStatus.VotesSubmitted);
                    Context.votesubmission.UpdateOne(filter, update);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(0, ex, "Failed on VoteOnBlockchain");
            }
        }

        [HttpGet("_GetCompletedQuestions")]
        public ActionResult _GetCompletedQuestions()
        {
            try
            {
                string meetingId = HttpContext.Session.GetString("displayResultsContractAddress");

                List<Question> questions = _blockchainContext.questions;
                List<Answer> answers = _blockchainContext.answers;

                List<Question> completedQuestions = questions;

                foreach (Question question in completedQuestions)
                {
                    decimal allAnswerVotesTotal = (from a in answers
                                                   select a.TotalVotes).Sum();

                    var winningAnswer = (from a in answers
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

        [HttpGet("_GetCompletedQuestions_Realtime/{UserType}")]
        public ActionResult _GetCompletedQuestions_Realtime(string UserType)
        {
            try
            {
                var builder = new ConfigurationBuilder()
                     .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json");

                IConfigurationRoot Configuration = builder.Build();

                decimal coinWeight = Convert.ToDecimal(Configuration["coinWeight"]);

                string meetingId = HttpContext.Session.GetString("displayResultsContractAddress");
                string controlNumber = HttpContext.Session.GetString("ControlNumber");
                List<BlockchainAddress> blockchainAddresses = Context.blockchainaddresses.AsQueryable().Where(bc => bc.meetingId == meetingId && bc.currentTransaction == true && bc.isFirstTransaction == false).ToList();
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

                List<Question> questions = _blockchainContext.questions;

                List<Question> completedQuestions = questions.ToList();

                foreach (Question question in completedQuestions)
                {
                    var answers = (from a1 in _blockchainContext.answers
                                   join a2 in voteCalculation on a1.answid equals a2.ansid into a3
                                   from a2 in a3.DefaultIfEmpty()
                                   select new
                                   {
                                       quid = question.quid,
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

                    if (UserType == "SH")
                    {
                        List<BlockchainAddress> proxyChouce = Context.blockchainaddresses.AsQueryable().Where(bc => bc.meetingId == meetingId && bc.controlNumber == controlNumber && bc.quid == question.quid && bc.currentTransaction == true && bc.isFirstTransaction == false).ToList();
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
                    }
                    else
                    {
                        question.ProxyChoice = "FOR";
                        question.ProxyChoice_ru = "за";
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

        [HttpGet("_GetAnswerInformation/{quid}")]
        public ActionResult _GetAnswerInformation(string quid)
        {
            string meetingId = HttpContext.Session.GetString("displayResultsContractAddress");
            List<Question> questions = _blockchainContext.questions;
            var question = (from q in questions
                            where q.quid == quid
                            select new
                            {
                                questionText = q.text,
                                questionText_ru = q.text_ru
                            }).FirstOrDefault();
            string questionText = question.questionText;
            string questionText_ru = question.questionText_ru;

            List<Answer> answers = _blockchainContext.answers;

            List<AnswerPieChartVM> answersPieChartResult = (from a in answers
                                                            orderby a.answid
                                                            select new AnswerPieChartVM
                                                            {
                                                                keyvalue = quid + "/" + a.answid + "|",
                                                                quid = quid,
                                                                answid = a.answid,
                                                                text = a.test,
                                                                TotalVotes = a.TotalVotes,
                                                                Value = a.TotalVotes,
                                                                Category = a.answid + ") " + a.test
                                                            }
                                 ).ToList();

            answersPieChartResult.ForEach(a => a.text_ru = (a.text.ToUpper().Trim() == "FOR") ? "за" : ((a.text.ToUpper().Trim() == "AGAINST") ? "против" : "воздержался"));

            List<AnswerBarSchemaVM> answersBarChartSchema = (from a in answers
                                                             orderby a.answid
                                                             select new AnswerBarSchemaVM
                                                             {
                                                                 name = a.answid + ")",
                                                                 field = a.answid.ToLower(),
                                                                 quid = quid,
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

        [HttpGet("_GetAnswerInformation_Realtime/{quid}")]
        public ActionResult _GetAnswerInformation_Realtime(string quid)
        {
            var builder = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");

            IConfigurationRoot Configuration = builder.Build();

            //decimal surcharge = Convert.ToDecimal(Configuration["surcharge"]);
            decimal coinWeight = Convert.ToDecimal(Configuration["coinWeight"]);


            string meetingId = HttpContext.Session.GetString("displayResultsContractAddress");
            List<Question> questions = _blockchainContext.questions;
            var question = (from q in questions
                            where q.quid == quid
                            select new
                            {
                                questionText = q.text,
                                questionText_ru = q.text_ru
                            }).FirstOrDefault();
            string questionText = question.questionText;
            string questionText_ru = question.questionText_ru;

            List<BlockchainAddress> blockchainAddresses = Context.blockchainaddresses.AsQueryable().Where(bc => bc.meetingId == meetingId && bc.quid == quid && bc.currentTransaction == true && bc.isFirstTransaction == false).ToList();
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
            var answers = (from a1 in _blockchainContext.answers
                           join a2 in voteCalculation on a1.answid equals a2.ansid into a3
                           from a2 in a3.DefaultIfEmpty()
                           select new
                           {
                               quid = quid,
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
            string meetingId = HttpContext.Session.GetString("displayResultsContractAddress");
            List<Question> questions = _blockchainContext.questions;
            Question questionDetails = questions.AsQueryable().Where(q => q.quid == quid).FirstOrDefault();

            List<Answer> answers = _blockchainContext.answers;

            Answer answerDocument = answers.AsQueryable().Where(a => a.answid == ansid).FirstOrDefault();

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

            string meetingId = HttpContext.Session.GetString("displayResultsContractAddress");
            List<Question> questions = _blockchainContext.questions;
            Question questionDetails = questions.AsQueryable().Where(q => q.quid == quid).FirstOrDefault();

            List<BlockchainAddress> blockchainAddresses = Context.blockchainaddresses.AsQueryable().Where(bc => bc.meetingId == meetingId && bc.quid == quid && bc.ansid == ansid && bc.currentTransaction == true && bc.isFirstTransaction == false).ToList();
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

            var answers = (from a1 in _blockchainContext.answers
                           join a2 in voteCalculation on a1.answid equals a2.ansid into a3
                           from a2 in a3.DefaultIfEmpty()
                           select new
                           {
                               quid = quid,
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
                                 keyvalue = addr.publicAddress + "|",
                                 id = counter++,
                                 address = addr.publicAddress,
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
            List<Answer> answers = _blockchainContext.answers;

            List<IDictionary<string, Object>> finalResult = new List<IDictionary<string, object>>();
            var dynamicObject = new ExpandoObject() as IDictionary<string, Object>;
            dynamicObject.Add("year", "2018");
            List<AnswerPieChartVM> answersPieChartResult = (from a in answers
                                                            orderby a.answid
                                                            select new AnswerPieChartVM
                                                            {
                                                                quid = quid,
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

            string meetingId = HttpContext.Session.GetString("displayResultsContractAddress");
            string controlNumber = HttpContext.Session.GetString("ControlNumber");
            List<BlockchainAddress> blockchainAddresses = Context.blockchainaddresses.AsQueryable().Where(bc => bc.meetingId == meetingId && bc.quid == quid && bc.currentTransaction == true && bc.isFirstTransaction == false).ToList();
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
            var answers = (from a1 in _blockchainContext.answers
                           join a2 in voteCalculation on a1.answid equals a2.ansid into a3
                           from a2 in a3.DefaultIfEmpty()
                           select new
                           {
                               quid = quid,
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
                string meetingId = HttpContext.Session.GetString("activeVoteContractAddress");
                questions = _blockchainContext.questions;
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
            var answers = _blockchainContext.answers;

            return Ok(answers);
        }

        [HttpGet("answers/BTCS/{quid}")]
        public IActionResult GetAnswersForQuestion(string quid)
        {
            try
            {
                List<Answer> answers = _blockchainContext.answers;
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
