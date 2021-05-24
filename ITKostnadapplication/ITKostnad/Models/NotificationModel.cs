using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITKostnad.Net.OptionEnums;

namespace ITKostnad.Models
{
    public class NotificationModel
    {

        public string message { get; set; }
        //Description
        public Position position { get; set; }
        //Förvaltning
        public ToastType toastType { get; set; }
        //Plats
        public int timeOut { get; set; }

    }
}
