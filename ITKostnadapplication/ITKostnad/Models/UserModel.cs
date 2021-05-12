using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ITKostnad.Models
{
    public class UserModel
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }
        public string Samaccountname { get; set; }
        public string EmailAddress { get; set; }
    }
}
