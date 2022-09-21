using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using GuestBookProject.EntityModel;
using Microsoft.AspNet.Identity;
using Newtonsoft.JsonResult;
using JsonResult = Newtonsoft.JsonResult.JsonResult;
using GuestBookProject.Identity;

namespace GuestBookProject.Controllers
{
    public class GuestBookController : Controller
    {
        private GuestBookProjectContext db = new GuestBookProjectContext();

        // GET: GuestBook
        public ActionResult Index()
        {
            return View(db.GuestBook.ToList());
        }

        // GET: GuestBook/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {                
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            GuestBook guestBook = db.GuestBook.Find(id);
            if (guestBook == null)
            {
                return HttpNotFound();
            }
            return View(guestBook);
        }

        // GET: GuestBook/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: GuestBook/Create
        // 若要免於大量指派 (overposting) 攻擊，請啟用您要繫結的特定屬性，
        // 如需詳細資料，請參閱 https://go.microsoft.com/fwlink/?LinkId=317598。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "UserName,Title,Message")] GuestBook guestBook)
        {
            if (ModelState.IsValid)
            {
                guestBook.CreateDateTime = DateTime.Now;

                if (User.Identity.IsAuthenticated)
                    guestBook.UserId = User.Identity.GetIntUserId();

                db.GuestBook.Add(guestBook);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(guestBook);
        }

        // GET: GuestBook/Edit/5
        [Authorize]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            GuestBook guestBook = db.GuestBook.Find(id);
            if (guestBook == null)
            {
                return HttpNotFound();
            }
            else if(guestBook.UserId != User.Identity.GetIntUserId() && !User.IsInRole("Admin"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }
            return View(guestBook);
        }

        // POST: GuestBook/Edit/5
        // 若要免於大量指派 (overposting) 攻擊，請啟用您要繫結的特定屬性，
        // 如需詳細資料，請參閱 https://go.microsoft.com/fwlink/?LinkId=317598。
        [Authorize]    
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,UserName,Title,Message,CreateDateTime,UserId")] GuestBook guestBook)
        {
            if (ModelState.IsValid)
            {
                if (guestBook == null)
                {
                    return HttpNotFound();
                }
                else if (guestBook.UserId != User.Identity.GetIntUserId() && !User.IsInRole("Admin"))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }

                db.Entry(guestBook).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(guestBook);
        }

        // GET: GuestBook/Delete/5
        [Authorize]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            GuestBook guestBook = db.GuestBook.Find(id);
            if (guestBook == null)
            {
                return HttpNotFound();
            }
            else if (guestBook.UserId != User.Identity.GetIntUserId() && !User.IsInRole("Admin"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }
            return View(guestBook);
        }

        // POST: GuestBook/Delete/5
        [Authorize]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            GuestBook guestBook = db.GuestBook.Find(id);

            if (guestBook == null)
            {
                return HttpNotFound();
            }
            else if (guestBook.UserId != User.Identity.GetIntUserId() && !User.IsInRole("Admin"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            db.Reply.RemoveRange(guestBook.Reply);
            db.GuestBook.Remove(guestBook);
            db.SaveChanges();
            return RedirectToAction("Index");
        }


        [HttpPost, ActionName("CreateReply")]
        public JsonResult Replay(Reply reply)
        {
            var jsonResult = new JsonResult();
            reply.CreateDateTime = DateTime.Now;
            db.Reply.Add(reply);
            db.SaveChanges();
            jsonResult.Data = db.Reply.Where(x => x.GuestBookId == reply.GuestBookId).OrderBy(x => x.CreateDateTime).ToList()
                                      .Select(x => new { ReplyUserName = x.ReplyUserName, ReplyMessage = x.ReplyMessage, CreateDateTime = x.CreateDateTime.ToString("yyyy/MM/dd HH:mm:ss") }).ToList();
            return jsonResult;
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
