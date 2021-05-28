using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Threading;
using ITKostnad.Models;
using Microsoft.Extensions.Caching.Memory;

namespace ITKostnad.Helpers
{
    public class ADHelper
    {

        public static string Domain { get; set; }
        public static string ComputerOU { get; set; }
        public static string UserOU { get; set; }
        public static string ServiceAccount { get; set; }
        public static string ServiceAccountPassword { get; set; }
        public static IMemoryCache Cache { get; set; }

        private static List<ComputerModel> ADComputers;
        private static List<UserModel> ADUsers;
        private static Thread ServiceThread { get; set; }

        public static void start()
        {
            ADComputers = GetallComputers();
            ADUsers = GetallAdUsers();

            MemoryCacheEntryOptions entryOptions = new MemoryCacheEntryOptions();
            Cache.Set("Computers", ADComputers, entryOptions);
            Cache.Set("Users", ADUsers, entryOptions);


            ServiceThread = new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(TimeSpan.FromHours(12));
                    try
                    {
                        ADComputers = GetallComputers();
                        ADUsers = GetallAdUsers();
                        Cache.Set("Computers", ADComputers, entryOptions);
                        Cache.Set("Users", ADUsers, entryOptions);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }


                }
            });
            ServiceThread.Start();
        }

        //if you want to get Groups of Specific OU you have to add OU Name in Context
        private static List<ComputerModel> GetallComputers()
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
                        Name = p.Name,
                    });
                }
                ctx.Dispose();
                searcher.Dispose();
                results.Dispose();
                compPrin.Dispose();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return ADComputers;
        }


        //if you want to get Groups of Specific OU you have to add OU Name in Context        
        private static List<UserModel> GetallAdUsers()
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
                ctx.Dispose();
                searcher.Dispose();
                results.Dispose();
                userPrin.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return AdUsers;
        }


        private static void SetProperty(DirectoryEntry oDE, string sPropertyName, string sPropertyValue)
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

        public static ComputerModel GetComputer(string name)
        {
            ComputerModel myComputer = new ComputerModel();
            PrincipalContext context = new PrincipalContext(ContextType.Domain, Domain, ComputerOU, ServiceAccount, ServiceAccountPassword);

            try
            {
                ComputerPrincipal computer = ComputerPrincipal.FindByIdentity(context, IdentityType.Name, name);

                using (DirectoryEntry de = computer.GetUnderlyingObject() as DirectoryEntry)
                {

                    // Go for those attributes and do what you need to do...
                    myComputer.Name = de.Properties["Name"].Value as string;
                    myComputer.Description = de.Properties["Description"].Value as string;
                    myComputer.Department = de.Properties["Department"].Value as string;
                    myComputer.Location = de.Properties["Location"].Value as string;
                    myComputer.PhysicalDeliveryOfficeName = de.Properties["PhysicalDeliveryOfficeName"].Value as string;
                    myComputer.ExtensionAttribute13 = de.Properties["ExtensionAttribute13"].Value as string;
                    myComputer.ExtensionAttribute9 = de.Properties["ExtensionAttribute9"].Value as string;

                    de.Dispose();
                }
                computer.Dispose();
            }
            finally
            {
                context.Dispose();
            }

            return myComputer;
        }

        public static void SetComputer(UpdateModel data)
        {

            PrincipalContext context = new PrincipalContext
                                       (ContextType.Domain, Domain, ComputerOU, ServiceAccount, ServiceAccountPassword);
            try
            {

                ComputerPrincipal computer = ComputerPrincipal.FindByIdentity
                                 (context, IdentityType.Name, data.Name);

                using (DirectoryEntry de = computer.GetUnderlyingObject() as DirectoryEntry)
                {
                    string newDescription = ($"{data.Department}, {data.Location}, {data.PhysicalDeliveryOfficeName}, {data.ExtensionAttribute9}, {data.ExtensionAttribute13}");

                    SetProperty(de, "Department", data.Department);
                    SetProperty(de, "Location", data.Location);
                    SetProperty(de, "PhysicalDeliveryOfficeName", data.PhysicalDeliveryOfficeName);
                    SetProperty(de, "ExtensionAttribute9", data.ExtensionAttribute9);
                    SetProperty(de, "ExtensionAttribute13", data.ExtensionAttribute13);
                    SetProperty(de, "Description", newDescription);

                }
                computer.Dispose();
            }
            finally
            {
                context.Dispose();
            }
            return;
        }

        public static void ReplaceComputer(UpdateModel data)
        {
            PrincipalContext context = new PrincipalContext(ContextType.Domain, Domain, ComputerOU, ServiceAccount, ServiceAccountPassword);
            ComputerModel swapComputer = new ComputerModel();
            try
            {

                ComputerPrincipal swapObject = ComputerPrincipal.FindByIdentity(context, IdentityType.Name, data.ReplaceString);

                //Retrieve swapcomputer attributes
                using (DirectoryEntry de = swapObject.GetUnderlyingObject() as DirectoryEntry)
                {
                    swapComputer.Name = de.Properties["Name"].Value as string;
                    swapComputer.Description = de.Properties["Description"].Value as string;
                    swapComputer.Department = de.Properties["Department"].Value as string;
                    swapComputer.Location = de.Properties["Location"].Value as string;
                    swapComputer.PhysicalDeliveryOfficeName = de.Properties["PhysicalDeliveryOfficeName"].Value as string;
                    swapComputer.ExtensionAttribute13 = de.Properties["ExtensionAttribute13"].Value as string;
                    swapComputer.ExtensionAttribute9 = de.Properties["ExtensionAttribute9"].Value as string;
                }


                ComputerPrincipal inputObject = ComputerPrincipal.FindByIdentity
                                 (context, IdentityType.Name, data.Name);

                //Updates the inputcomputer with the swapcomputer values
                using (DirectoryEntry de2 = inputObject.GetUnderlyingObject() as DirectoryEntry)
                {

                    SetProperty(de2, "Department", swapComputer.Department);
                    SetProperty(de2, "Location", swapComputer.Location);
                    SetProperty(de2, "PhysicalDeliveryOfficeName", swapComputer.PhysicalDeliveryOfficeName);
                    SetProperty(de2, "ExtensionAttribute9", swapComputer.ExtensionAttribute9);
                    SetProperty(de2, "ExtensionAttribute13", swapComputer.ExtensionAttribute13);
                    SetProperty(de2, "Description", swapComputer.Description);

                }

                context = new PrincipalContext(ContextType.Domain, Domain, ComputerOU, ServiceAccount, ServiceAccountPassword);
                swapObject = ComputerPrincipal.FindByIdentity(context, IdentityType.Name, data.ReplaceString);

                //Updates the swapcomputer with the input values
                using (DirectoryEntry de3 = swapObject.GetUnderlyingObject() as DirectoryEntry)
                {
                    string newDescription = ($"{data.Department}, {data.Location}, {data.PhysicalDeliveryOfficeName}, {data.ExtensionAttribute9}, {data.ExtensionAttribute13}");

                    SetProperty(de3, "Department", data.Department);
                    SetProperty(de3, "Location", data.Location);
                    SetProperty(de3, "PhysicalDeliveryOfficeName", data.PhysicalDeliveryOfficeName);
                    SetProperty(de3, "ExtensionAttribute9", data.ExtensionAttribute9);
                    SetProperty(de3, "ExtensionAttribute13", data.ExtensionAttribute13);
                    SetProperty(de3, "Description", newDescription);

                }
                swapObject.Dispose();
                inputObject.Dispose();
            }
            finally
            {
                context.Dispose();
            }
            return;
        }
    }
}
