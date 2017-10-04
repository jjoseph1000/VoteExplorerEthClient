using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using Microsoft.Extensions.Configuration;
using System.IO;
using Nethereum.Web3;
using Nethereum.Hex.HexConvertors.Extensions;

namespace VoteExplorer.Models
{
    public class VoteExplorerBlockchainContext
    {
        public IConfigurationRoot Configuration;
        public string abi;
        public string contractAddress;
        public string accountPublicAddress;
        public string accountPrivateKey;
        public string blockchainNetwork;
        public Web3 web3;
        public List<Question> _questions;

        public VoteExplorerBlockchainContext()
        {
            try
            {
                var builder2 = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

                Configuration = builder2.Build();
                abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"voter\",\"type\":\"string\"}],\"name\":\"getLastVoteSessionId\",\"outputs\":[{\"name\":\"voteSessionId1\",\"type\":\"string\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"indexVoter\",\"type\":\"uint256\"},{\"name\":\"voter\",\"type\":\"string\"}],\"name\":\"getVoteAnswersByVoterId\",\"outputs\":[{\"name\":\"indexVoter1\",\"type\":\"uint256\"},{\"name\":\"voter1\",\"type\":\"string\"},{\"name\":\"voteSessionId\",\"type\":\"string\"},{\"name\":\"voteAnswers\",\"type\":\"string\"},{\"name\":\"blockNumber\",\"type\":\"uint256\"},{\"name\":\"balance\",\"type\":\"uint256\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"voterIndex\",\"type\":\"uint256\"}],\"name\":\"getVoteAnswersByIndex\",\"outputs\":[{\"name\":\"indexVoter1\",\"type\":\"uint256\"},{\"name\":\"voter1\",\"type\":\"string\"},{\"name\":\"voteSessionId\",\"type\":\"string\"},{\"name\":\"voteAnswers\",\"type\":\"string\"},{\"name\":\"blockNumber\",\"type\":\"uint256\"},{\"name\":\"balance\",\"type\":\"uint256\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[],\"name\":\"totalVoters\",\"outputs\":[{\"name\":\"totalVoters\",\"type\":\"uint256\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"questionIndex\",\"type\":\"uint256\"}],\"name\":\"getQuestionByIndex\",\"outputs\":[{\"name\":\"questionIndex1\",\"type\":\"uint256\"},{\"name\":\"questionId\",\"type\":\"string\"},{\"name\":\"questionTextRows\",\"type\":\"uint256\"},{\"name\":\"boardRecommendation\",\"type\":\"string\"},{\"name\":\"isActive\",\"type\":\"uint256\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"questionId\",\"type\":\"string\"},{\"name\":\"questionTextRows\",\"type\":\"uint256\"},{\"name\":\"questionText\",\"type\":\"bytes32\"},{\"name\":\"boardRecommendation\",\"type\":\"string\"},{\"name\":\"isActive\",\"type\":\"uint256\"}],\"name\":\"insertUpdateQuestion\",\"outputs\":[{\"name\":\"insertupdate\",\"type\":\"bool\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":true,\"inputs\":[{\"name\":\"x\",\"type\":\"bytes32\"}],\"name\":\"bytes32ToString\",\"outputs\":[{\"name\":\"\",\"type\":\"string\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"questionId\",\"type\":\"string\"},{\"name\":\"questionTextRow\",\"type\":\"uint256\"}],\"name\":\"getQuestionTextByRow\",\"outputs\":[{\"name\":\"questionid\",\"type\":\"string\"},{\"name\":\"row\",\"type\":\"uint256\"},{\"name\":\"textLine\",\"type\":\"bytes32\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"questionId\",\"type\":\"string\"},{\"name\":\"questionTextRow\",\"type\":\"uint256\"},{\"name\":\"questionText\",\"type\":\"bytes32\"}],\"name\":\"addQuestionTextRow\",\"outputs\":[{\"name\":\"success\",\"type\":\"bool\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[],\"name\":\"totalQuestions\",\"outputs\":[{\"name\":\"totalQuestions\",\"type\":\"uint256\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"voter\",\"type\":\"string\"},{\"name\":\"voteSessionId\",\"type\":\"string\"},{\"name\":\"selectedAnswers\",\"type\":\"string\"},{\"name\":\"voteShares\",\"type\":\"uint256\"}],\"name\":\"vote\",\"outputs\":[{\"name\":\"Result\",\"type\":\"bool\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"source\",\"type\":\"string\"}],\"name\":\"stringToBytes32\",\"outputs\":[{\"name\":\"result\",\"type\":\"bytes32\"}],\"payable\":false,\"type\":\"function\"},{\"inputs\":[],\"payable\":false,\"type\":\"constructor\"}]";
                contractAddress = "0xB4e8F652ae2EDDBdFBFd2e1f70E42fC37DF44d88";
                accountPublicAddress = "0x4c3b38F3085A17c1fC8396A3b4B3015ABbC6A2CD";
                accountPrivateKey = "0d0c308303065f2e42bedec3211fab3cb22449cba989b51e22705a575ad12599";
                blockchainNetwork = "https://kovan.infura.io/";

                web3 = new Web3(blockchainNetwork);
                getQuestions().Wait();
                var connectionString = Configuration["MongoDBConnectionString"];
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //public List<Question> questions { get { return _questions; } }

        public async Task getQuestions()
        {
            List<Question> questions = new List<Question>();

            int totQuestions = await totalQuestions();

            for (int x = 0; x < totQuestions; x++)
            {
                Question question = await getQuestionByIndex(x);
                question.text = "";
                for (int y = 0; y < question.questionTextRows; y++)
                {
                    string questionText = await getQuestionTextByRow(question.quid, y);
                    if (y == 0)
                    {
                        question.text += questionText.Replace("\0", "");
                    }
                    else
                    {
                        question.text += " " + questionText.Replace("\0", "");
                    }
                }

                questions.Add(question);
            }

            _questions = questions;
        }

        public async Task<Question> getQuestionByIndex(int questionIndex)
        {
            var contract = web3.Eth.GetContract(abi, contractAddress);
            var function = contract.GetFunction("getQuestionByIndex");
            object[] param = new object[1];
            param[0] = questionIndex;

            Question result = await function.CallDeserializingToObjectAsync<Question>(param);
            return result;
        }

        public async Task<string> getQuestionTextByRow(string questionId, int questionTextRow)
        {
            var contract = web3.Eth.GetContract(abi, contractAddress);
            var function = contract.GetFunction("getQuestionTextByRow");
            object[] param = new object[2];
            param[0] = questionId;
            param[1] = questionTextRow;

            QuestionText result = await function.CallDeserializingToObjectAsync<QuestionText>(param);
            string s = HexStringUTF8ConvertorExtensions.HexToUTF8String(result.textLine.ToHex());
            return s;
        }



        public async Task<int> totalQuestions()
        {
            var contract = web3.Eth.GetContract(abi, contractAddress);
            var function = contract.GetFunction("totalQuestions");

            int result = await function.CallAsync<int>();
            return result;
        }


    }
}
