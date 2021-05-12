using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ITKostnad.Models;

namespace ITKostnad.Controllers
{

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private static IOptions<AppSettingsModel> _appSettings;

        private static string Domain;
        private static string ComputerOU;
        private static string UserOU;

        public HomeController(ILogger<HomeController> logger, IOptions<AppSettingsModel> app)
        {
            _logger = logger;
            _appSettings = app;

            Domain = _appSettings.Value.Domain;
            ComputerOU = _appSettings.Value.ComputerOU;
            UserOU = _appSettings.Value.UserOU;
        }

        public IActionResult Index()
        {
            ToPageModel Page = new ToPageModel();
            Page.Computer = GetallComputers();
            Page.User = GetallAdUsers();
            return View(Page);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public void SetProperty(DirectoryEntry oDE, string sPropertyName, string sPropertyValue)
        {
            //check if the value is valid, otherwise dont update
            if (sPropertyValue != string.Empty)
            {
                //check if the property exists before adding it to the list
                if (oDE.Properties.Contains(sPropertyName))
                {
                    oDE.Properties[sPropertyName].Value = sPropertyValue;
                    oDE.CommitChanges();
                    oDE.Close();
                }
                else
                {
                    oDE.Properties[sPropertyName].Add(sPropertyValue);
                    oDE.CommitChanges();
                    oDE.Close();
                }
            }
            else
            {
                oDE.Properties[sPropertyName].Clear();
                oDE.CommitChanges();
                oDE.Close();
            }
        }

        //if you want to get Groups of Specific OU you have to add OU Name in Context        
        public static List<UserModel> GetallAdUsers()
        {

            List<UserModel> AdUsers = new List<UserModel>();
            //MBS.com My Domain Controller which i created 
            //OU=DevOU --Organizational Unit which i created 
            //and create users and groups inside it 
            var ctx = new PrincipalContext(ContextType.Domain, Domain, UserOU);
            UserPrincipal userPrin = new UserPrincipal(ctx);
            userPrin.Name = "*";
            var searcher = new System.DirectoryServices.AccountManagement.PrincipalSearcher();
            searcher.QueryFilter = userPrin;
            ((DirectorySearcher)searcher.GetUnderlyingSearcher()).PageSize = 500;
            var results = searcher.FindAll();
            foreach (UserPrincipal p in results)
            {
                AdUsers.Add(new UserModel
                {
                    DisplayName = p.DisplayName,
                    Samaccountname = p.SamAccountName,
                    EmailAddress = p.EmailAddress


                });
            }
            return AdUsers;
        }

        //if you want to get Groups of Specific OU you have to add OU Name in Context
        public static List<ComputerModel> GetallComputers()
        {

            List<ComputerModel> ADComputers = new List<ComputerModel>();
            var ctx = new PrincipalContext(ContextType.Domain, Domain, ComputerOU);
            ComputerPrincipal compPrin = new ComputerPrincipal(ctx);
            compPrin.Name = "*";
            var searcher = new System.DirectoryServices.AccountManagement.PrincipalSearcher();
            searcher.QueryFilter = compPrin;
            ((DirectorySearcher)searcher.GetUnderlyingSearcher()).PageSize = 500;
            var results = searcher.FindAll();
            foreach (Principal p in results)
            {
                ADComputers.Add(new ComputerModel
                {
                    name = p.Name,
                });
            }

            return ADComputers;
        }

        [HttpPost]
        public ActionResult GetComputer([FromBody] ComputerModel data)
        {
            try
            {
                ComputerModel myComputer = new ComputerModel();

                PrincipalContext context = new PrincipalContext
                                           (ContextType.Domain, Domain, ComputerOU);
                ComputerPrincipal computer = ComputerPrincipal.FindByIdentity
                                 (context, IdentityType.Name, data.name);

                using (DirectoryEntry de = computer.GetUnderlyingObject() as DirectoryEntry)
                {

                    // Go for those attributes and do what you need to do...
                    myComputer.name = de.Properties["Name"].Value as string;
                    myComputer.description = de.Properties["Description"].Value as string;
                    myComputer.department = de.Properties["Department"].Value as string;
                    myComputer.location = de.Properties["Location"].Value as string;
                    myComputer.physicalDeliveryOfficeName = de.Properties["PhysicalDeliveryOfficeName"].Value as string;
                    myComputer.extensionAttribute13 = de.Properties["extensionAttribute13"].Value as string;
                    myComputer.extensionAttribute9 = de.Properties["extensionAttribute9"].Value as string;
                }

                return Json(new { result = true, data = myComputer });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, error = ex.Message });
            }

        }

        [HttpPost]
        public ActionResult SetComputer([FromBody] ComputerModel data)
        {

            try
            {
                PrincipalContext context = new PrincipalContext
                                       (ContextType.Domain, Domain, ComputerOU);
                ComputerPrincipal computer = ComputerPrincipal.FindByIdentity
                                 (context, IdentityType.Name, data.name);

                using (DirectoryEntry de = computer.GetUnderlyingObject() as DirectoryEntry)
                {
                    string newDescription = ($"{data.department}, {data.location}, {data.physicalDeliveryOfficeName}, {data.extensionAttribute9}, {data.extensionAttribute13}");

                    SetProperty(de, "Department", data.department);
                    SetProperty(de, "Location", data.location);
                    SetProperty(de, "PhysicalDeliveryOfficeName", data.physicalDeliveryOfficeName);
                    SetProperty(de, "extensionAttribute9", data.extensionAttribute9);
                    SetProperty(de, "extensionAttribute13", data.extensionAttribute13);
                    SetProperty(de, "Description", newDescription);

                }

                return Json(new { result = true });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, error = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult SwapComputer([FromBody] SwapModel data)
        {

            try
            {
                ComputerModel inputComputer = data.MyInput;

                ComputerModel swapComputer = new ComputerModel();


                PrincipalContext context = new PrincipalContext
                                           (ContextType.Domain, Domain, ComputerOU);

                ComputerPrincipal swapObject = ComputerPrincipal.FindByIdentity
                                 (context, IdentityType.Name, data.MySwap);

                //Retrieve swapcomputer attributes
                using (DirectoryEntry de = swapObject.GetUnderlyingObject() as DirectoryEntry)
                {
                    swapComputer.name = de.Properties["Name"].Value as string;
                    swapComputer.description = de.Properties["Description"].Value as string;
                    swapComputer.department = de.Properties["Department"].Value as string;
                    swapComputer.location = de.Properties["Location"].Value as string;
                    swapComputer.physicalDeliveryOfficeName = de.Properties["PhysicalDeliveryOfficeName"].Value as string;
                    swapComputer.extensionAttribute13 = de.Properties["extensionAttribute13"].Value as string;
                    swapComputer.extensionAttribute9 = de.Properties["extensionAttribute9"].Value as string;
                }


                ComputerPrincipal inputObject = ComputerPrincipal.FindByIdentity
                                 (context, IdentityType.Name, inputComputer.name);

                //Updates the inputcomputer with the swapcomputer values
                using (DirectoryEntry de2 = inputObject.GetUnderlyingObject() as DirectoryEntry)
                {

                    SetProperty(de2, "Department", swapComputer.department);
                    SetProperty(de2, "Location", swapComputer.location);
                    SetProperty(de2, "PhysicalDeliveryOfficeName", swapComputer.physicalDeliveryOfficeName);
                    SetProperty(de2, "extensionAttribute9", swapComputer.extensionAttribute9);
                    SetProperty(de2, "extensionAttribute13", swapComputer.extensionAttribute13);
                    SetProperty(de2, "Description", swapComputer.description);

                }

                context = new PrincipalContext(ContextType.Domain, Domain, ComputerOU);
                swapObject = ComputerPrincipal.FindByIdentity(context, IdentityType.Name, data.MySwap);

                //Updates the swapcomputer with the input values
                using (DirectoryEntry de3 = swapObject.GetUnderlyingObject() as DirectoryEntry)
                {
                    string newDescription = ($"{inputComputer.department}, {inputComputer.location}, {inputComputer.physicalDeliveryOfficeName}, {inputComputer.extensionAttribute9}, {inputComputer.extensionAttribute13}");

                    SetProperty(de3, "Department", inputComputer.department);
                    SetProperty(de3, "Location", inputComputer.location);
                    SetProperty(de3, "PhysicalDeliveryOfficeName", inputComputer.physicalDeliveryOfficeName);
                    SetProperty(de3, "extensionAttribute9", inputComputer.extensionAttribute9);
                    SetProperty(de3, "extensionAttribute13", inputComputer.extensionAttribute13);
                    SetProperty(de3, "Description", newDescription);

                }


                return Json(new { result = true });
            }
            catch (Exception ex)
            {
                return Json(new { result = false, error = ex.Message });
            }
        }


    }
}
