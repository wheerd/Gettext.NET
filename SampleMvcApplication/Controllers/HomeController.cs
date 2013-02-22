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

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = _("Your contact page.");

            return View();
        }
    }
}
