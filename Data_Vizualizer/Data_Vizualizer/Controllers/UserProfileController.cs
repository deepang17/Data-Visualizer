using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Data_Vizualizer.Models;

namespace Data_Vizualizer.Controllers
{
    public class UserProfileController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: UserProfile
        public ActionResult Index()
        {
            return View(db.UserTitles.ToList());
        }

        // GET: UserProfile/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            UserTitles userTitles = db.UserTitles.Find(id);
            if (userTitles == null)
            {
                return HttpNotFound();
            }
            return View(userTitles);
        }

        // GET: UserProfile/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: UserProfile/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Name,Title,Type,Description")] UserTitles userTitles)
        {
            if (ModelState.IsValid)
            {
                db.UserTitles.Add(userTitles);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(userTitles);
        }

        // GET: UserProfile/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            UserTitles userTitles = db.UserTitles.Find(id);
            if (userTitles == null)
            {
                return HttpNotFound();
            }
            return View(userTitles);
        }

        // POST: UserProfile/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,Title,Type,Description")] UserTitles userTitles)
        {
            if (ModelState.IsValid)
            {
                db.Entry(userTitles).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(userTitles);
        }

        // GET: UserProfile/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            UserTitles userTitles = db.UserTitles.Find(id);
            if (userTitles == null)
            {
                return HttpNotFound();
            }
            return View(userTitles);
        }

        // POST: UserProfile/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            UserTitles userTitles = db.UserTitles.Find(id);
            db.UserTitles.Remove(userTitles);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
        [HttpPost]
        public JsonResult SaveProfile()
        {
            for (int i = 0; i < Request.Files.Count; i++)
            {
                HttpPostedFileBase file = Request.Files[i]; //Uploaded file
                                                            //Use the following properties to get file's name, size and MIMEType
                





                if (file != null && file.ContentLength > 0)
                {
                    // ExcelDataReader works with the binary Excel file, so it needs a FileStream
                    // to get started. This is how we avoid dependencies on ACE or Interop:
                    Stream stream = file.InputStream;



                    string extension = System.IO.Path.GetExtension(file.FileName).ToLower();
                    string query = null;
                    string connString = "";

                    string[] validFileTypes = {".jpg"};
                    string path1 = string.Format("{0}/{1}", Server.MapPath("~/Content/Profile/"), User.Identity.Name+extension);
                    if (!Directory.Exists(path1))
                    {
                        Directory.CreateDirectory(Server.MapPath("~/Content/Profile"));
                    }
                    if (validFileTypes.Contains(extension))
                    {
                        if (System.IO.File.Exists(path1))
                        {
                            System.IO.File.Delete(path1);
                        }
                        Request.Files[i].SaveAs(path1);
                    }
                }
            }
            return Json("Uploaded");
        }
    }
}
