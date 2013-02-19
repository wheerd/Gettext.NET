using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SampleMvcApplication.Controllers
{
    public abstract class LocalizingController : Controller
    {
        public string _(string message)
        {
            return message;
        }

        public string _n(string message, string plural)
        {
            return message;
        }

        public string _c(string context, string message)
        {
            return message;
        }
    }
}
