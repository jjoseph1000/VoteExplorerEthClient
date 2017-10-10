using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using Microsoft.Extensions.Configuration;
using System.IO;


namespace VoteExplorer.Models
{
    public class VoteExplorerContext
    {
        public IMongoDatabase Database;
        public IConfigurationRoot Configuration;
        public VoteExplorerContext()
        {
            try
            {
                var builder2 = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

                Configuration = builder2.Build();

                var connectionString = Configuration["MongoDBConnectionString"];
                var settings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));
                //settings.ClusterConfigurator = builder => builder.Subscribe(new Log4NetMongoEvents());
                var client = new MongoClient(settings);
                Database = client.GetDatabase(Configuration["DatabaseName"]);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public IMongoCollection<VoteSubmission> votesubmission => Database.GetCollection<VoteSubmission>("votesubmission");

        public IMongoCollection<BlockchainVoteRequest> blockchainvoterequests => Database.GetCollection<BlockchainVoteRequest>("blockchainvoterequests");

        public IMongoCollection<ContractBlockchainAddresses> contractBlockchainAddresses => Database.GetCollection<ContractBlockchainAddresses>("contractblockchainaddresses");

        public IMongoCollection<Alert> alerts => Database.GetCollection<Alert>("alerts");

        public IMongoCollection<Custodian> custodians => Database.GetCollection<Custodian>("custodian");

        public IMongoCollection<Account> accounts => Database.GetCollection<Account>("account");

        public IMongoCollection<Company> companies => Database.GetCollection<Company>("company");

        public IMongoCollection<Customer> customers => Database.GetCollection<Customer>("customer");

        public IMongoCollection<Meeting> meetings => Database.GetCollection<Meeting>("meeting");

        public IMongoCollection<Group> groups => Database.GetCollection<Group>("groups");

        public IMongoCollection<BlockchainAddress> blockchainaddresses => Database.GetCollection<BlockchainAddress>("blockchainaddresses");

        public IMongoCollection<SHOAccount> shoaccounts => Database.GetCollection<SHOAccount>("shoaccounts");

        public IMongoCollection<IVYAccount> ivyaccounts => Database.GetCollection<IVYAccount>("ivyaccounts");

        public IMongoCollection<Meeting_SHOAccount> meeting_shoaccounts => Database.GetCollection<Meeting_SHOAccount>("meeting_shoaccounts");
        
        public IMongoCollection<dashboardAnnouncements> dashboardannouncements => Database.GetCollection<dashboardAnnouncements>("dashboard-announcements");

        public IMongoCollection<dashboardUpcomingmeetings> dashboardupcomingmeetings => Database.GetCollection<dashboardUpcomingmeetings>("dashboard-upcomingmeetings");

        public IMongoCollection<dashboardNewmeetings> dashboardnewmeetings => Database.GetCollection<dashboardNewmeetings>("dashboard-newmeetings");

        public IMongoCollection<dashboardNewaccounts> dashboardnewaccounts => Database.GetCollection<dashboardNewaccounts>("dashboard-newaccounts");

        public IMongoCollection<InitialTransaction> initialtransactions => Database.GetCollection<InitialTransaction>("initialtransactions");

    }
}
