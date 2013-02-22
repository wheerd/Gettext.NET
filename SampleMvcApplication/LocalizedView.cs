using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SampleMvcApplication
{
    public abstract class LocalizedView<TModel> : WebViewPage<TModel>
    {
        public string _(string message, params object[] args)
        {
            // return String.Format(message, args);
            return "XXX";
        }

        public string _n(string message, string plural, params object[] args)
        {
            // return String.Format(message, args);
            return "XXX";
        }

        public string _c(string context, string message, params object[] args)
        {
            // return String.Format(message, args);
            return "XXX";
        }
    }
}
