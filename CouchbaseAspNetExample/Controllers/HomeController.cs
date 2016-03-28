using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CouchbaseAspNetExample.Constants;

namespace CouchbaseAspNetExample.Controllers
{
    [OutputCache(Duration = 10)]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            Session[AppConstants.ApplicationName] = "My Couchbase ASP.NET App!";
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

        [OutputCache(Duration = 10, VaryByParam = "foo")]
        public ActionResult Time(string foo)
        {
            return Content(DateTime.Now.ToString());
        }
    }
}