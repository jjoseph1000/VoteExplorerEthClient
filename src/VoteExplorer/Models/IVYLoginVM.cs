using System.Linq;
using System.ComponentModel.DataAnnotations;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Collections.Generic;

namespace VoteExplorer.Models
{
    public class IVYLoginVM
    {
        private string _userName;

        [Required]
        [IVYUsernameExists(ErrorMessage = "User name does not exist")]
        public string userName { get {
                return _userName;
            } set {
                _userName = value;
            } }


        [Required]
        [ValidPassword("demo", ErrorMessage = "Incorrect Password Entered")]
        public string passWord { get; set; }
    }

    public class IVYUsernameExists : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            try
            {
                VoteExplorerContext Context = new VoteExplorerContext();
                List<IVYAccount> ivyaccounts = Context.ivyaccounts.AsQueryable().ToList();

                return ivyaccounts.Any(u => u.userName == (string)value);
            }
            catch
            {
                return false;
            }
        }

    }

    public class ValidPassword : ValidationAttribute
    {
        string _userName;
        public ValidPassword(string userName)
        {
            _userName = userName;
        }

        public override bool IsValid(object value)
        {
            try
            {
                VoteExplorerContext Context = new VoteExplorerContext();
                List<IVYAccount> ivyaccounts = Context.ivyaccounts.AsQueryable().Where(ivy=>ivy.userName==_userName).ToList();

                if (ivyaccounts.Any())
                {
                    return ivyaccounts.Any(a => a.passWord == value.ToString());
                }
                else
                {
                    return false;
                }

                return ivyaccounts.Any(u => u.userName == (string)value);
            }
            catch
            {
                return false;
            }
        }

    }

}
