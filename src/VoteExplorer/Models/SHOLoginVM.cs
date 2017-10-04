using System.Linq;
using System.ComponentModel.DataAnnotations;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Collections.Generic;

namespace VoteExplorer.Models
{
    public class SHOLoginVM
    {
        [Required]
        [ControlNumberExists(ErrorMessage = "Control Number Does Not Exist.")]
        public string controlNumber { get; set; }



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

                return shoaccounts.Any(u => u.ControlNumber == (string)value);
            }
            catch
            {
                return false;
            }
        }

    }


}
