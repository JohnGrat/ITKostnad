using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITKostnad.Net.OptionEnums;

namespace ITKostnad.Models
{
    public class NotificationModel
    {

        public string Message { get; set; }
        //Description
        public Position Position { get; set; }
        //Förvaltning
        public ToastType ToastType { get; set; }
        //Plats
        public int TimeOut { get; set; }

    }
}
