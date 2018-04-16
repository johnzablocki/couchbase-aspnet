using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SessionStateAsyncExample.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            Session["test"] = "foo";
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page. " + Session["test"];

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}