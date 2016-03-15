﻿using System;
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
            ViewData[ViewDataConstants.MESSAGE] = "Hello, Couchbase ASP.NET!";

			return View();
        }

		public ActionResult About()
		{			

			return View();
		}

		[OutputCache(Duration = 10, VaryByParam="foo")]
		public ActionResult Time(string foo)
		{
			return Content(DateTime.Now.ToString());
		}

	}
}
