using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CouchbaseAspNetSample.Constants;

namespace CouchbaseAspNetSample.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            Session[SessionConstants.MESSAGE] = "Hello, Couchbase ASP.NET!";

			return View();
        }

		public ActionResult About()
		{			

			return View();
		}

	}
}
