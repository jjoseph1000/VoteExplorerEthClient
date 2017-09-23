using System.Linq;
using System.ComponentModel.DataAnnotations;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Collections.Generic;

namespace VoteExplorer.Models
{
    public class SHOLoginVM
    {
        public string contractNumber { get; set; }
        public string tokenSymbol { get; set; }
        public string tokenName { get; set; }
        public string balance { get; set; }

    }

    public class SHOLogin_Russian_VM
    {
        [Required]
        [ControlNumberExists(ErrorMessage = "Контрольная сумма не существует")]
        public string controlNumber { get; set; }
    }

    public class ControlNumberExists : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            try
            {
                VoteExplorerContext Context = new VoteExplorerContext();
                List<SHOAccount> shoaccounts = Context.shoaccounts.AsQueryable().ToList();

                return shoaccounts.Any(u => u.account == (string)value);
            }
            catch
            {
                return false;
            }
        }

    }


}
