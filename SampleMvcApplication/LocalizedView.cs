using GettextDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;

namespace SampleMvcApplication
{
    public abstract class LocalizedView<TModel> : WebViewPage<TModel>
    {
        public HtmlString _(Func<dynamic,HelperResult> message, params object[] args)
        {
            var key = String.Join("\n", message(null).ToHtmlString().Split('\n').Select(s => s.Trim())).Trim();

            return new HtmlString(String.Format(Internationalization.GetText(key, "de"), args));
        }

        public string _(string message, params object[] args)
        {
            return String.Format(Internationalization.GetText(message, "de"), args);
        }

        public string _n(string message, string plural, int n, params object[] args)
        {
            object[] newArgs = new object[args.Length + 1];
            newArgs[0] = n;
            Array.Copy(args, 0, newArgs, 1, args.Length);

            return String.Format(Internationalization.GetTextPlural(message, plural, n, "de"), newArgs);
        }

        public string _c(string context, string message, params object[] args)
        {
            return String.Format(Internationalization.GetText(message, "de"), args);
        }
    }
}
