using Data_Vizualizer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Data_Vizualizer.Controllers
{
    public class AppUsersController : Controller
    {
        // GET: AppUsers
        public ActionResult Index()
        {
            ApplicationDbContext adb = new ApplicationDbContext();
            List<ApplicationUser> ls = adb.Users.ToList();
            ViewBag.ls = ls;
            return View();
        }
    }
}