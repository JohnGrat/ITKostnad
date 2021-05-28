using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ITKostnad.Models;
using ITKostnad.Net;
using ITKostnad.Net.OptionEnums;
using Microsoft.Extensions.Caching.Memory;
using ITKostnad.Helpers;

namespace ITKostnad.Controllers
{
    [Authorize("ITK_Group")]
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
            if (!string.IsNullOrEmpty(searchString))
            {
                NotificationModel notification = new NotificationModel();
                try
                {
                    Page.SelectedComputer = ADHelper.GetComputer(searchString);
                }
                catch (Exception ex)
                {
                    notification.Message = ex.Message;
                    notification.TimeOut = 10000;
                    notification.ToastType = ToastType.Error;
                    notification.Position = Position.BottomCenter;
                    TempData["shortMessage"] = Notification.Show(notification.Message, position: notification.Position, type: notification.ToastType, timeOut: notification.TimeOut);
                }


            }
            ViewBag.Computers = Computers;
            ViewBag.Users = Users;
            ViewBag.Message = TempData["shortMessage"];

            return View(Page);
        }

        public IActionResult UpdateComputer([Bind("Name,Department,Location,PhysicalDeliveryOfficeName,ExtensionAttribute9,ExtensionAttribute13,ReplaceString")] UpdateModel data)
        {
            NotificationModel notification = new NotificationModel();

            if (string.IsNullOrEmpty(data.ReplaceString))
            {
                try
                {
                    ADHelper.SetComputer(data);
                    notification.Message = $"Update {data.Name} successful";
                    notification.TimeOut = 3000;
                    notification.ToastType = ToastType.Success;
                    notification.Position = Position.BottomCenter;
                }
                catch (Exception ex)
                {
                    notification.Message = ex.Message;
                    notification.TimeOut = 10000;
                    notification.ToastType = ToastType.Error;
                    notification.Position = Position.BottomCenter;
                }

            }
            else
            {
                try
                {
                    ADHelper.ReplaceComputer(data);
                    notification.Message = $"Replacement {data.Name} successful";
                    notification.TimeOut = 3000;
                    notification.ToastType = ToastType.Success;
                    notification.Position = Position.BottomCenter;
                }
                catch (Exception ex)
                {
                    notification.Message = ex.Message;
                    notification.TimeOut = 10000;
                    notification.ToastType = ToastType.Error;
                    notification.Position = Position.BottomCenter;
                }
            }

            TempData["shortMessage"] = Notification.Show(notification.Message, position: notification.Position, type: notification.ToastType, timeOut: notification.TimeOut);

            return RedirectToAction("Index", new { searchString = data.Name });

        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}
