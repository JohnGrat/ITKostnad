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
using ITKostnad.Net;
using ITKostnad.Net.OptionEnums;

namespace ITKostnad.Controllers
{

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private static IOptions<AppSettingsModel> _appSettings;

        private static string Domain;
        private static string ComputerOU;
        private static string UserOU;
        private static string ServiceAccount;
        private static string ServiceAccountPassword;

        public HomeController(ILogger<HomeController> logger, IOptions<AppSettingsModel> app)
        {
            _logger = logger;
            _appSettings = app;

            Domain = _appSettings.Value.Domain;
            ComputerOU = _appSettings.Value.ComputerOU;
            ServiceAccount = _appSettings.Value.ServiceAccount;
            ServiceAccountPassword = _appSettings.Value.ServiceAccountPassword;
        }

        public IActionResult Index(string searchString)
        {
            ToPageModel Page = new ToPageModel();
            Page.Computer = GetallComputers();
            Page.User = GetallAdUsers();
            Page.SelectedComputer = GetComputer(searchString);

            ViewBag.Message = TempData["shortMessage"];



            return View(Page);
        }

        public IActionResult UpdateComputer([Bind("name,department,location,physicalDeliveryOfficeName,extensionAttribute9,extensionAttribute13,ReplaceString")] UpdateModel data)
        {

            if (string.IsNullOrEmpty(data.ReplaceString))
            {
                SetComputer(data);
            }
            else
            {
                ReplaceComputer(data);
            }


            return RedirectToAction("Index", new { searchString = data.name });



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
        public List<UserModel> GetallAdUsers()
        {

            List<UserModel> AdUsers = new List<UserModel>();
            try
            {

            
                //MBS.com My Domain Controller which i created 
                //OU=DevOU --Organizational Unit which i created 
                //and create users and groups inside it 
                var ctx = new PrincipalContext(ContextType.Domain, Domain, UserOU, ServiceAccount, ServiceAccountPassword);
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
            } 
            catch (Exception ex)
            {
                TempData["shortMessage"] = Notification.Show(ex.Message, position: Position.BottomCenter, type: ToastType.Error, timeOut: 7000);
            }
            return AdUsers;
        }

        //if you want to get Groups of Specific OU you have to add OU Name in Context
        public List<ComputerModel> GetallComputers()
        {
            List<ComputerModel> ADComputers = new List<ComputerModel>();
            try
            {

                var ctx = new PrincipalContext(ContextType.Domain, Domain, ComputerOU, ServiceAccount, ServiceAccountPassword);
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

                
            } 
            catch (Exception ex)
            {
                TempData["shortMessage"] = Notification.Show(ex.Message, position: Position.BottomCenter, type: ToastType.Error, timeOut: 7000);
            }
            return ADComputers;
        }


        public ComputerModel GetComputer(string name)
        {
            ComputerModel myComputer = new ComputerModel();

            try
            {

                PrincipalContext context = new PrincipalContext(ContextType.Domain, Domain, ComputerOU, ServiceAccount, ServiceAccountPassword);
                ComputerPrincipal computer = ComputerPrincipal.FindByIdentity(context, IdentityType.Name, name);

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


            }
            catch (Exception ex)
            {
                TempData["shortMessage"] = Notification.Show(ex.Message, position: Position.BottomCenter, type: ToastType.Error, timeOut: 7000);
                
            }
            return myComputer;
        }

        public void SetComputer(UpdateModel data)
        {
            try
            {
                PrincipalContext context = new PrincipalContext
                                       (ContextType.Domain, Domain, ComputerOU, ServiceAccount, ServiceAccountPassword);
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

                TempData["shortMessage"] = Notification.Show($"{data.name} update successful", position: Position.BottomCenter, type: ToastType.Success, timeOut: 3000);
                
            }
            catch (Exception ex)
            {
                TempData["shortMessage"] = Notification.Show(ex.Message, position: Position.BottomCenter, type: ToastType.Error, timeOut: 7000);
                
            }
            return;

        }

        public void ReplaceComputer(UpdateModel data)
        {
            ComputerModel swapComputer = new ComputerModel();
            try
            {

                PrincipalContext context = new PrincipalContext(ContextType.Domain, Domain, ComputerOU, ServiceAccount, ServiceAccountPassword);

                ComputerPrincipal swapObject = ComputerPrincipal.FindByIdentity(context, IdentityType.Name, data.ReplaceString);

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
                                 (context, IdentityType.Name, data.name);

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

                context = new PrincipalContext(ContextType.Domain, Domain, ComputerOU, ServiceAccount, ServiceAccountPassword);
                swapObject = ComputerPrincipal.FindByIdentity(context, IdentityType.Name, data.ReplaceString);

                //Updates the swapcomputer with the input values
                using (DirectoryEntry de3 = swapObject.GetUnderlyingObject() as DirectoryEntry)
                {
                    string newDescription = ($"{data.department}, {data.location}, {data.physicalDeliveryOfficeName}, {data.extensionAttribute9}, {data.extensionAttribute13}");

                    SetProperty(de3, "Department", data.department);
                    SetProperty(de3, "Location", data.location);
                    SetProperty(de3, "PhysicalDeliveryOfficeName", data.physicalDeliveryOfficeName);
                    SetProperty(de3, "extensionAttribute9", data.extensionAttribute9);
                    SetProperty(de3, "extensionAttribute13", data.extensionAttribute13);
                    SetProperty(de3, "Description", newDescription);

                }

                TempData["shortMessage"] = Notification.Show($"Replacement {data.ReplaceString} successful", position: Position.BottomCenter, type: ToastType.Success, timeOut: 10000);
                
            }
            catch (Exception ex)
            {
                TempData["shortMessage"] = Notification.Show(ex.Message, position: Position.BottomCenter, type: ToastType.Error, timeOut: 7000);
               
            }
            return;
        }


    }
}
