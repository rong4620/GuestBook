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
using MoreLinq;
using System.Data.SqlClient;
using System.Configuration;
using Dapper;
using System.Threading.Tasks;

namespace GuestBookProject.Controllers
{
    public class GuestBookController : Controller
    {
        private GuestBookProjectContext db = new GuestBookProjectContext();

        private string strConnection = ConfigurationManager.ConnectionStrings["GuestBookProjectConnectionString"].ToString();

        // GET: GuestBook
        public async Task<ActionResult> Index(string SelectedUserName)
        {
            List<GuestBook> guestBooksList = null;

            //Dapper
            using (SqlConnection conn = new SqlConnection(strConnection))
            {
                string sql = @"SELECT distinct G.UserName FROM GuestBook G";

                var guestBookUsers = (await conn.QueryAsync<string>(sql, null)).ToList();
                ViewBag.SelectedUserName = new SelectList(guestBookUsers, SelectedUserName);

                var parameters = new DynamicParameters();

                sql = @"SELECT * FROM GuestBook G 
                               LEFT JOIN Reply R on G.Id = R.GuestBookId
                               WHERE 1=1";

                if (!string.IsNullOrEmpty(SelectedUserName))
                {
                    sql += " AND G.UserName =  @UserName";
                    parameters.Add("UserName", SelectedUserName);
                }

                var guestBookDictionary = new Dictionary<int, GuestBook>();
                guestBooksList = (await conn.QueryAsync<GuestBook, Reply, GuestBook>(sql, (guestBook, reply) =>
                {
                    GuestBook guestBookEntry;
                    if (!guestBookDictionary.TryGetValue(guestBook.Id, out guestBookEntry))
                    {
                        guestBookEntry = guestBook;
                        guestBookEntry.Reply = new List<Reply>();
                        guestBookDictionary.Add(guestBook.Id, guestBook);
                    }

                    if (reply != null)
                    {
                        guestBookEntry.Reply.Add(reply);
                    }
                    return guestBookEntry;
                }, parameters)).Distinct().OrderByDescending(x => x.CreateDateTime).ToList();
            }

            // EF
            //var guestBookUsers = await db.GuestBook.Select(x => x.UserName).ToListAsync();
            //var selectGuestBookUsers = guestBookUsers.DistinctBy(x => x);
            //ViewBag.SelectedUserName = new SelectList(selectGuestBookUsers, SelectedUserName);
            //if (!string.IsNullOrEmpty(SelectedUserName))
            //{
            //    IQueryable<GuestBook> courses = db.GuestBook
            //        .Where(c => c.UserName.Contains(SelectedUserName));

            //    guestBooksList = await courses.OrderByDescending(x => x.CreateDateTime).ToListAsync();
            //}
            //else
            //    guestBooksList = await db.GuestBook.OrderByDescending(x => x.CreateDateTime).ToListAsync();

            return View(guestBooksList);
        }

        // GET: GuestBook/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            //EF
            //GuestBook guestBook = db.GuestBook.Find(id);

            GuestBook guestBook = await findGuestBookByIdAsync(id);

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
        public async Task<ActionResult> Create([Bind(Include = "UserName,Title,Message")] GuestBook guestBook)
        {
            if (ModelState.IsValid)
            {
                guestBook.CreateDateTime = DateTime.Now;

                if (User.Identity.IsAuthenticated)
                    guestBook.UserId = User.Identity.GetIntUserId();

                //Dapper
                using (SqlConnection conn = new SqlConnection(strConnection))
                {
                    string sql = "INSERT INTO GuestBook VALUES (@UserName,@Title,@Message,@CreateDateTime,@UserId);";
                    await conn.ExecuteAsync(sql, guestBook);
                }


                //EF
                //db.GuestBook.Add(guestBook);
                //db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(guestBook);
        }

        // GET: GuestBook/Edit/5
        [Authorize]
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            //EF
            //GuestBook guestBook = db.GuestBook.Find(id);

            GuestBook guestBook = await findGuestBookByIdAsync(id);

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

        // POST: GuestBook/Edit/5
        // 若要免於大量指派 (overposting) 攻擊，請啟用您要繫結的特定屬性，
        // 如需詳細資料，請參閱 https://go.microsoft.com/fwlink/?LinkId=317598。
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,UserName,Title,Message,CreateDateTime,UserId")] GuestBook guestBook)
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

                //Dapper
                using (SqlConnection conn = new SqlConnection(strConnection))
                {
                    string sql = @"UPDATE GuestBook SET 
                                   UserName = @UserName,
                                   Title = @Title,
                                   Message = @Message,
                                   CreateDateTime = @CreateDateTime,
                                   UserId = @UserId
                                   WHERE Id=@Id";
                    await conn.ExecuteAsync(sql, guestBook);
                }


                //EF
                //db.Entry(guestBook).State = EntityState.Modified;
                //db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(guestBook);
        }

        // GET: GuestBook/Delete/5
        [Authorize]
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            //EF
            //GuestBook guestBook = db.GuestBook.Find(id);

            GuestBook guestBook = await findGuestBookByIdAsync(id);

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
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            //EF
            //GuestBook guestBook = db.GuestBook.Find(id);

            GuestBook guestBook = await findGuestBookByIdAsync(id);

            if (guestBook == null)
            {
                return HttpNotFound();
            }
            else if (guestBook.UserId != User.Identity.GetIntUserId() && !User.IsInRole("Admin"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            using (SqlConnection conn = new SqlConnection(strConnection))
            {
                string strSql = @"DELETE Reply WHERE GuestBookId = @Id;
                                  DELETE GuestBook WHERE Id = @Id";
                await conn.ExecuteAsync(strSql, guestBook);
            }

            //EF
            //db.Reply.RemoveRange(guestBook.Reply);
            //db.GuestBook.Remove(guestBook);
            //db.SaveChanges();
            return RedirectToAction("Index");
        }


        [HttpPost, ActionName("CreateReply")]
        public async Task<JsonResult> CreateReplayAsync(Reply reply)
        {
            var jsonResult = new JsonResult();
            reply.CreateDateTime = DateTime.Now;

            //Dapper
            using (SqlConnection conn = new SqlConnection(strConnection))
            {
                string sql = "INSERT INTO Reply VALUES (@ReplyUserName,@ReplyMessage,@CreateDateTime,@GuestBookId);";
                await conn.ExecuteAsync(sql, reply);

                sql = "SELECT * FROM Reply WHERE Reply.GuestBookId = @GuestBookId;";
                jsonResult.Data = (await conn.QueryAsync<Reply>(sql, new { GuestBookId = reply.GuestBookId }))
                                  .Select(x => new { ReplyUserName = x.ReplyUserName, ReplyMessage = x.ReplyMessage, CreateDateTime = x.CreateDateTime.ToString("yyyy/MM/dd HH:mm:ss") }).ToList();
            }

            //EF
            //db.Reply.Add(reply);
            //db.SaveChanges();
            //jsonResult.Data = (await db.Reply.Where(x => x.GuestBookId == reply.GuestBookId).OrderBy(x => x.CreateDateTime).ToListAsync())
            //                          .Select(x => new { ReplyUserName = x.ReplyUserName, ReplyMessage = x.ReplyMessage, CreateDateTime = x.CreateDateTime.ToString("yyyy/MM/dd HH:mm:ss") }).ToList();
            return jsonResult;
        }

        private async Task<GuestBook> findGuestBookByIdAsync(int? id)
        {
            GuestBook result = null;

            using (SqlConnection conn = new SqlConnection(strConnection))
            {
                var parameters = new DynamicParameters();
                parameters.Add("Id", id);

                var sql = @"SELECT G.* , 
                            R.Id as Reply_Id ,
                            R.ReplyUserName as Reply_ReplyUserName ,
                            R.ReplyMessage as Reply_ReplyMessage ,
                            R.CreateDateTime as Reply_CreateDateTime 
                            FROM GuestBook G 
                            LEFT JOIN Reply R on G.Id = R.GuestBookId
                            WHERE G.Id = @Id";

                var dy = await conn.QueryAsync<dynamic>(sql, parameters);
                result = Slapper.AutoMapper.MapDynamic<GuestBook>(dy, false).FirstOrDefault();
            }

            return result;
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
