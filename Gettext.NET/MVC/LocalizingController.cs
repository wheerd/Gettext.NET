using System;
using System.Threading;
using System.Web.Mvc;

namespace GettextDotNet.MVC
{
    public abstract class LocalizingController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var request = filterContext.RequestContext;
            var routeValues = request.RouteData.Values;

            if (!routeValues.ContainsKey(Internationalization.Settings.RouteDataKey))
            {
                routeValues.Add(Internationalization.Settings.RouteDataKey, Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName);

                var url = new UrlHelper(filterContext.RequestContext).RouteUrl(routeValues);

                new RedirectResult(url).ExecuteResult(filterContext);
            }
        }

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
            var lang = Thread.CurrentThread.CurrentCulture.ToString();

            filterContext.HttpContext.Response.AppendHeader("Content-Language", lang);
        }

        public static string _(string message, params object[] args)
        {
            return String.Format(Internationalization.GetText(message), args);
        }

        public static string _n(string message, string plural, int n, params object[] args)
        {
            return String.Format(Internationalization.GetTextPlural(message, plural, n), args.Prepend(n));
        }

        public static string _c(string context, string message, params object[] args)
        {
            return String.Format(Internationalization.GetText(message, context: context), args);
        }

        public static string _nc(string context, string message, string plural, int n, params object[] args)
        {
            return String.Format(Internationalization.GetTextPlural(message, plural, n, context: context), args.Prepend(n));
        }
    }
}
