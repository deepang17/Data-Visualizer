using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Data_Vizualizer.Controllers
{
    public class AdminController : Controller
    {
        // GET: Admin
        public ActionResult Index()
        {
            ViewBag.Starter = 0;
            ViewBag.Show = 0;
            return View();
        }
        [HttpPost]
        public ActionResult Index(FormCollection fc)
        {
            ViewBag.Starter = 1;
            if (fc["uname"] == "dvm" && fc["pass"] == "dvm17") {
                ViewBag.Show = 1;
            }
            else {
                ViewBag.ErrMes = "Invalid Username Or Password";
                ViewBag.Starter = 0;
                ViewBag.Show = 0;
            }

            return View();
        }
    }
}