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
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Threading;
using Microsoft.AspNetCore.Http;

namespace VoteExplorer.Controllers
{


    public class HomeController : Controller
    {
        public static readonly VoteExplorerContext Context = new VoteExplorerContext();
        private VoteExplorerBlockchainContext _blockchainContext;
        public static bool accessBlockChain = checkIfAccessBlockChain();
        readonly ILogger<HomeController> _log;

        public HomeController(ILogger<HomeController> log, VoteExplorerBlockchainContext blockchainContext)
        {
            _log = log;
            _blockchainContext = blockchainContext;
        }

        public ActionResult Index()
        {
            return RedirectToAction("Login", "ShareholderVoting");
        }

        public IActionResult About()
        {
            var builder = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");

            IConfigurationRoot Configuration = builder.Build();

            decimal surcharge = Convert.ToDecimal(Configuration["surcharge"]);
            decimal coinWeight = Convert.ToDecimal(Configuration["coinWeight"]);

            ViewData["Message"] = "Your application description page.";
            string meetingId = HttpContext.Session.GetString("activeVoteContractAddress");
            List<SHOAccount> shoaccounts = Context.shoaccounts.AsQueryable().ToList();

            string publicAddress = "1zzzJv5hYmWbN79zHm5f4ZQRR8k9GWQTf";
            if (meetingId != "-1")
            {
                ////////////////
                List<Question> questions = _blockchainContext.questions;
                List<BlockchainAddress> blockchainAddresses = new List<BlockchainAddress>();

                foreach (SHOAccount shoaccount in shoaccounts)
                {
                    foreach (Question question in questions)
                    {
                        BlockchainAddress blockchainAddress = new BlockchainAddress();
                        decimal totalCoins = Convert.ToDecimal(shoaccount.AvailableShares) * coinWeight + Convert.ToDecimal("0.02");
                        string transactionid = sendCoinsToAddress(publicAddress, totalCoins.ToString());
                        Console.Write(transactionid);
                        blockchainAddress.isFirstTransaction = true;
                        blockchainAddress.publicAddress = publicAddress;
                        blockchainAddress.generalFundPublicAddress = publicAddress;
                        blockchainAddress.meetingId = meetingId;
                        blockchainAddress.controlNumber = shoaccount.ControlNumber;
                        blockchainAddress.quid = question.quid;
                        blockchainAddress.coins = totalCoins.ToString();
                        blockchainAddress.transactionId = transactionid;
                        blockchainAddress.currentTransaction = true;
                        blockchainAddresses.Add(blockchainAddress);
                    }



                    //}
                    /////////////////////////////////////
                    //for (int x = 0; x < 50000; x++)
                    //{
                    //    foreach (Question question in questions)
                    //    {
                    //        decimal totalCoins = Convert.ToDecimal("1000") * coinWeight;
                    //        string transactionid = sendCoinsToAddress(publicAddress, totalCoins.ToString());
                    //        Console.Write(x.ToString() + " " + transactionid);

                    //        InitialTransaction initialTransaction = new InitialTransaction();
                    //        initialTransaction.transactionId = transactionid;
                    //        initialTransaction.controlNumber = "444";
                    //        initialTransaction.meetingId = meetingId;
                    //        initialTransaction.quid = question.quid;
                    //        initialTransaction.publicAddress = publicAddress;
                    //        initialTransaction.amount = totalCoins.ToString();

                    //        initialtransactions.Add(initialTransaction);
                    //    }


                    //}



                }
                Console.Write(DateTime.Now.ToString() + "\n");
                Context.blockchainaddresses.InsertManyAsync(blockchainAddresses);
                Console.Write(DateTime.Now.ToString() + "\n");

            }
            else
            {
            }
            return View();
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

        public string createRawTransactionOneInput(string transactionId, string toPublicAddress, string toAmount, string chgPublicAddress, string chgAmount)
        {
            string returnValue = "";
            if (accessBlockChain)
            {
                var command = "sh";
                var argss = "/root/externalApps/BCI-XXX/createRawTransaction_first_txid_toadd_toamt_chgadd_chgamt.sh " + transactionId + " " + toPublicAddress + " " + toAmount + " " + chgPublicAddress + " " + chgAmount;
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
                var argss = "/root/externalApps/BCI-XXX/createRawTransaction_txid_toadd_toamt_chgadd_chgamt.sh " + firstInputTransactionId + " "+ firstInputIndex + " " + secondInputTransactionId + " " + secondInputIndex + " " + toPublicAddress + " " + toAmount + " " + chgPublicAddress + " " + chgAmount;
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

                returnValue = "23423asd";
            }

            return returnValue;
        }

        public string sendCoinsToAddress(string toPublicAddress, string amount)
        {
            string returnValue = "";
            if (accessBlockChain)
            {
                var command = "sh";
                var argss = "/root/externalApps/BCI-XXX/sendCoins.sh " + toPublicAddress + " " + amount;
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

                returnValue = "23423asd";
            }

            return returnValue;
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
