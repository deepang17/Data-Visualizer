using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Data_Vizualizer.Models;

namespace Data_Vizualizer.Controllers
{
    public class UserTitlesController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: UserTitles
        public ActionResult Index()
        {
            return View(db.UserTitles.ToList());
        }

        // GET: UserTitles/Details/5
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

        // GET: UserTitles/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: UserTitles/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Name,Title,Type")] UserTitles userTitles)
        {
            if (ModelState.IsValid)
            {
                db.UserTitles.Add(userTitles);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(userTitles);
        }

        // GET: UserTitles/Edit/5
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

        // POST: UserTitles/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,Title,Type")] UserTitles userTitles)
        {
            if (ModelState.IsValid)
            {
                db.Entry(userTitles).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(userTitles);
        }

        // GET: UserTitles/Delete/5
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

        // POST: UserTitles/Delete/5
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
    }
}
