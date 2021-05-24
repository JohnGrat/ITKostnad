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
using System.Security.Cryptography;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using ITKostnad.Helpers;

namespace ITKostnad.Controllers
{

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly IMemoryCache _cache;

        private static List<UserModel> Users;
        private static List<ComputerModel> Computers;

        public HomeController(ILogger<HomeController> logger, IMemoryCache cache)
        {

            _logger = logger;
            _cache = cache;

            Users = (List<UserModel>)_cache.Get("Users");
            Computers = (List<ComputerModel>)_cache.Get("Computers");

        }


        public IActionResult Index(string searchString)
        {
            ToPageModel Page = new ToPageModel();
            Page.Computer = Computers;
            Page.User = Users;

            if (!string.IsNullOrEmpty(searchString))
            {
                NotificationModel notification = new NotificationModel();
                try
                {
                    Page.SelectedComputer = ADHelper.GetComputer(searchString);
                }
                catch (Exception ex)
                {
                    notification.message = ex.Message;
                    notification.timeOut = 10000;
                    notification.toastType = ToastType.Error;
                    notification.position = Position.BottomCenter;
                    TempData["shortMessage"] = Notification.Show(notification.message, position: notification.position, type: notification.toastType, timeOut: notification.timeOut);
                }


            }

            ViewBag.Message = TempData["shortMessage"];

            return View(Page);
        }

        public IActionResult UpdateComputer([Bind("name,department,location,physicalDeliveryOfficeName,extensionAttribute9,extensionAttribute13,ReplaceString")] UpdateModel data)
        {
            NotificationModel notification = new NotificationModel();

            if (string.IsNullOrEmpty(data.ReplaceString))
            {
                try
                {
                    ADHelper.SetComputer(data);
                    notification.message = $"Update {data.name} successful";
                    notification.timeOut = 3000;
                    notification.toastType = ToastType.Success;
                    notification.position = Position.BottomCenter;
                }
                catch (Exception ex)
                {
                    notification.message = ex.Message;
                    notification.timeOut = 10000;
                    notification.toastType = ToastType.Error;
                    notification.position = Position.BottomCenter;
                }

            }
            else
            {
                try
                {
                    ADHelper.ReplaceComputer(data);
                    notification.message = $"Replacement {data.name} successful";
                    notification.timeOut = 3000;
                    notification.toastType = ToastType.Success;
                    notification.position = Position.BottomCenter;
                }
                catch (Exception ex)
                {
                    notification.message = ex.Message;
                    notification.timeOut = 10000;
                    notification.toastType = ToastType.Error;
                    notification.position = Position.BottomCenter;
                }
            }

            TempData["shortMessage"] = Notification.Show(notification.message, position: notification.position, type: notification.toastType, timeOut: notification.timeOut);

            return RedirectToAction("Index", new { searchString = data.name });

        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}
