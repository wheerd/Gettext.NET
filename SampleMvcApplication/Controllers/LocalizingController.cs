using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SampleMvcApplication.Controllers
{
    public abstract class LocalizingController : Controller
    {
        public static string _(string message, params object[] args)
        {
            // return String.Format(message, args);
            return "XXX";
        }

        public static string _n(string message, string plural, params object[] args)
        {
            // return String.Format(message, args);
            return "XXX";
        }

        public static string _c(string context, string message, params object[] args)
        {
            // return String.Format(message, args);
            return "XXX";
        }
    }
}
