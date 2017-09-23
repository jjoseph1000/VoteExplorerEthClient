using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace VoteExplorer.Models
{
    public class AccountVM
    {
        public string _id { get; set; }

        public int _Id { get; set; }

        public int AccountNumber { get; set; }

        public string CompanyId { get; set; }

        public string CompanyName { get; set; }

        public long AvailableShares { get; set; }

        public string CustodianId { get; set; }

        public string BankName { get; set; }

        public string CustomerID { get; set; }

        public string CustomerName { get; set; }

        public string KeyValue { get; set; }

    }
}
