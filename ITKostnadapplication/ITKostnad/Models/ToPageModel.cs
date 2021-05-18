using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ITKostnad.Models
{
    public class ToPageModel
    {
        public List<ComputerModel> Computer { get; set; }
        public List<UserModel> User { get; set; }
        public ComputerModel SelectedComputer { get; set; }
        public string SearchString { get; set; }
        public string ReplaceString { get; set; }
    }
}
