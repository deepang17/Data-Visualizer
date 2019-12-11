using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Data_Vizualizer.Models;
using Microsoft.AspNet.Identity;
namespace Data_Vizualizer.Controllers
{
    public class savingController : Controller
    {
        // GET: saving
        public ActionResult Index()
        {
            return View();
        }
        //post saving
        [HttpPost]
        public ActionResult Index(string imageData,string inText)
        {
            ApplicationDbContext db = new ApplicationDbContext();
            UserTitles ut = new UserTitles();
            
            ut.Name = User.Identity.Name;
            ut.Title = inText;
            db.UserTitles.Add(ut);
            db.SaveChanges();
            //string fileNameWitPath = @"D:\" + DateTime.Now.ToString().Replace("/", "-").Replace(" ", "- ").Replace(":", "") + ".png";
            string fileNameWitPath = @"C:\Uploads\"+User.Identity.Name+@"\"+inText+".png";
            if (!Directory.Exists(@"C:\Uploads"))
            {
                Directory.CreateDirectory(@"C:\Uploads");
            }
            if (!Directory.Exists(@"C:\Uploads\"+ User.Identity.Name))
            {
                Directory.CreateDirectory(@"C:\Uploads\"+ User.Identity.Name);
            }
            using (FileStream fs = new FileStream(fileNameWitPath, FileMode.Create))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))

                {
                    byte[] data = Convert.FromBase64String(imageData);
                    bw.Write(data);
                    bw.Close();
                }
            }
            return View();
        }
    }
}