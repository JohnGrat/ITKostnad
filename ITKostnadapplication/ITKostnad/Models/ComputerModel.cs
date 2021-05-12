using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ITKostnad.Models
{
    public class ComputerModel
    {
        //Datornamn
        public string name { get; set; }
        //Description
        public string description { get; set; }
        //Förvaltning
        public string department { get; set; }
        //Plats
        public string location { get; set; }
        //Placering
        public string physicalDeliveryOfficeName { get; set; }
        //Ansvarig
        public string extensionAttribute9 { get; set; }
        //Delad/Personlig
        public string extensionAttribute13 { get; set; }

    }
}
