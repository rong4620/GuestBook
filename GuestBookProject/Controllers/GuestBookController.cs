using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
//using GuestBookProject.EntityModel;
using JsonResult = Newtonsoft.JsonResult.JsonResult;
using GuestBookProject.Identity;
using MoreLinq;
using System.Data.SqlClient;
using System.Configuration;
using Dapper;
using System.Threading.Tasks;
using X.PagedList;
using GuestBookModel.Model;
using Dapper.Contrib.Extensions;

namespace GuestBookProject.Controllers
{
    public class GuestBookController : Controller
    {
        //private GuestBookProjectContext db = new GuestBookProjectContext();

        private string strConnection = ConfigurationManager.ConnectionStrings["GuestBookProjectConnectionString"].ToString();

        // GET: GuestBook
        public async Task<ActionResult> Index(string currentFilter, string searchString, string sortOrder, int? page, int pageSize = 5)
        {

            ViewBag.CurrentPageSize = pageSize;
            ViewBag.CurrentSort = sortOrder;
            ViewBag.ReplyCountSortParm = sortOrder == "ReplyCount" ? "ReplyCount_Desc" : "ReplyCount";
            ViewBag.DateSortParm = String.IsNullOrEmpty(sortOrder) ? "Date" : "";

            ViewBag.PageSize = new List<SelectListItem>()
            {                
                new SelectListItem() { Value="5", Text= "5" },
                new SelectListItem() { Value="10", Text= "10" },
                new SelectListItem() { Value="15", Text= "15" },
                new SelectListItem() { Value="25", Text= "25" },
                new SelectListItem() { Value="50", Text= "50" },
             };

            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewBag.CurrentFilter = searchString;

            IEnumerable<GuestBook> queryReslut = null;

            using (SqlConnection conn = new SqlConnection(strConnection))
            {
                var parameters = new DynamicParameters();

                string sql = @"SELECT  t1.* , t2.ReplyCount FROM
                                (SELECT G.* from GuestBook G) as t1
                                JOIN
                                (SELECT G.Id ,COUNT(DISTINCT R.Id) ReplyCount FROM GuestBook G 
                                LEFT JOIN Reply R on G.Id = R.GuestBookId
                                GROUP BY G.Id)  as t2
                                ON t1.Id = t2.Id
                                WHERE 1=1";

                if (!string.IsNullOrEmpty(searchString))
                {
                    sql += " AND t1.UserName LIKE  @UserName";
                    parameters.Add("UserName", "%" + searchString + "%");
                }

                var guestBookDictionary = new Dictionary<int, GuestBook>();
                var query = (await conn.QueryAsync<GuestBook>(sql, parameters));

                switch (sortOrder)
                {
                    case "ReplyCount":
                        queryReslut = query.OrderBy(x => x.ReplyCount).ThenByDescending(x => x.CreateDateTime);
                        break;
                    case "ReplyCount_Desc":
                        queryReslut = query.OrderByDescending(x => x.ReplyCount).ThenByDescending(x => x.CreateDateTime);
                        break;
                    case "Date":
                        queryReslut = query.OrderBy(x => x.CreateDateTime);
                        break;
                    default:
                        queryReslut = query.OrderByDescending(x => x.CreateDateTime);
                        break;
                }
            }

            int pageNumber = page ?? 1;

            return View(queryReslut.ToPagedList(pageNumber, pageSize));

        }

        // GET: GuestBook/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

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

                using (SqlConnection conn = new SqlConnection(strConnection))
                {
                    string sql = "INSERT INTO GuestBook VALUES (@UserName,@Title,@Message,@CreateDateTime,@UserId);";
                    await conn.ExecuteAsync(sql, guestBook);
                }

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

                using (SqlConnection conn = new SqlConnection(strConnection))
                {
                    //string sql = @"UPDATE GuestBook SET 
                    //               UserName = @UserName,
                    //               Title = @Title,
                    //               Message = @Message,
                    //               CreateDateTime = @CreateDateTime,
                    //               UserId = @UserId
                    //               WHERE Id=@Id";
                    //await conn.ExecuteAsync(sql, guestBook);

                    await conn.UpdateAsync<GuestBook>(guestBook);
                }

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

            return RedirectToAction("Index");
        }


        [HttpPost, ActionName("CreateReply")]
        public async Task<JsonResult> CreateReplayAsync(Reply reply)
        {
            var jsonResult = new JsonResult();
            reply.CreateDateTime = DateTime.Now;

            using (SqlConnection conn = new SqlConnection(strConnection))
            {
                string sql = "INSERT INTO Reply VALUES (@ReplyUserName,@ReplyMessage,@CreateDateTime,@GuestBookId);";
                await conn.ExecuteAsync(sql, reply);

                sql = "SELECT * FROM Reply WHERE Reply.GuestBookId = @GuestBookId;";
                jsonResult.Data = (await conn.QueryAsync<Reply>(sql, new { GuestBookId = reply.GuestBookId }))
                                  .Select(x => new { ReplyUserName = x.ReplyUserName, ReplyMessage = x.ReplyMessage, CreateDateTime = x.CreateDateTime.ToString("yyyy/MM/dd HH:mm:ss") }).ToList();
            }

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

        private async Task<IEnumerable<GuestBook>> GetGuestBooksWithReplyAsync(string searchString, string sortOrder)
        {
            IEnumerable<GuestBook> result = null;
            using (SqlConnection conn = new SqlConnection(strConnection))
            {
                var parameters = new DynamicParameters();

                string sql = @"SELECT * FROM GuestBook G 
                               LEFT JOIN Reply R on G.Id = R.GuestBookId
                               WHERE 1=1";

                if (!string.IsNullOrEmpty(searchString))
                {
                    sql += " AND G.UserName LIKE  @UserName";
                    parameters.Add("UserName", "%" + searchString + "%");
                }

                var guestBookDictionary = new Dictionary<int, GuestBook>();
                var query = (await conn.QueryAsync<GuestBook, Reply, GuestBook>(sql, (guestBook, reply) =>
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
                }, parameters)).Distinct();

                switch (sortOrder)
                {
                    case "ReplyCount":
                        result = query.OrderBy(x => x.Reply.Count).ThenByDescending(x => x.CreateDateTime);
                        break;
                    case "ReplyCount_Desc":
                        result = query.OrderByDescending(x => x.Reply.Count).ThenByDescending(x => x.CreateDateTime);
                        break;
                    case "Date":
                        result = query.OrderBy(x => x.CreateDateTime);
                        break;
                    default:
                        result = query.OrderByDescending(x => x.CreateDateTime);
                        break;
                }
            }

            return result;
        }

        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing)
        //    {
        //        db.Dispose();
        //    }
        //    base.Dispose(disposing);
        //}
    }
}
