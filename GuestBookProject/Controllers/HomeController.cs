using GuestBookProject.EntityModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GuestBookProject.Controllers
{

    public class HomeController : Controller
    {


        public ActionResult Index()
        {
            using (var context = new GuestBookProjectContext())
            {
                var tt = context.GuestBook.FirstOrDefault();

                ViewBag.Message = tt.Message;
            }
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}