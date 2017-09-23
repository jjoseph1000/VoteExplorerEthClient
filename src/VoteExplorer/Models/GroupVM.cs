using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace VoteExplorer.Models
{
    public class GroupVM
    {
        public string _id { get; set; }

        public string KeyValue { get; set; }

        public int _Id { get; set; }

        public string GroupName { get; set; }

        public string MeetingId { get; set; }

        public string MeetingEntity { get; set; }
    }
}
