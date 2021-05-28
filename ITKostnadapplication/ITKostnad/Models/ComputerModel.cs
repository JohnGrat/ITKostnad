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
        public string Name { get; set; }
        //Description
        public string Description { get; set; }
        //Förvaltning
        public string Department { get; set; }
        //Plats
        public string Location { get; set; }
        //Placering
        public string PhysicalDeliveryOfficeName { get; set; }
        //Ansvarig
        public string ExtensionAttribute9 { get; set; }
        //Delad/Personlig
        public string ExtensionAttribute13 { get; set; }

    }
}
