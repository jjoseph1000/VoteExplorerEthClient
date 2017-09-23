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

namespace VoteExplorer.Controllers
{

    [Route("Shareholder")]
    public class ShareholderController : Controller
    {
        public static readonly VoteExplorerContext Context = new VoteExplorerContext();

        [HttpGet("Vote")]
        public ActionResult Vote()
        {
            return View();
        }

    }
}
