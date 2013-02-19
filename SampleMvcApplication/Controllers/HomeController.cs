using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GettextDotNet;

namespace SampleMvcApplication.Controllers
{
    public class HomeController : LocalizingController
    {
        public ActionResult Index()
        {
            // Test!
            ViewBag.Message = _("Modify this template to jump-start your ASP.NET MVC application."); // Test 2?
            // Not extracted

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = _("Your app description page.");

            // Test...
            var test = _("a" + false + "b" + ("c" + "d") + 5);
            test = _n("Singular", "Plural");
            test = _c("Context", "Message");

            return View();
        }

        public ActionResult Contact()
        {
            // Test 3
            _("Your app description page.");
            ViewBag.Message = _("Your contact page.");

            return View();
        }
    }
}
