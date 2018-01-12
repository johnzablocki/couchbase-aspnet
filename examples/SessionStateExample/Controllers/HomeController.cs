using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SessionStateExample.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            Session["mySession"] = DateTime.Now;
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page." + Session["mySession"];

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}