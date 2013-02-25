using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;

namespace GettextDotNet.MVC
{
    public abstract class LocalizedView<TModel> : WebViewPage<TModel>
    {
        public HtmlString _(Func<dynamic, HelperResult> message, params object[] args)
        {
            return _r(message.AsString(), args);
        }

        public HtmlString _r(string message, params object[] args)
        {
            return new HtmlString(_(message, args));
        }

        public static string _(string message, params object[] args)
        {
            return String.Format(Internationalization.GetText(message), args);
        }

        public HtmlString _n(Func<dynamic, HelperResult> message, Func<dynamic, HelperResult> plural, int n, params object[] args)
        {
            return _rn(message.AsString(), plural.AsString(), n, args);
        }

        public HtmlString _rn(string message, string plural, int n, params object[] args)
        {
            return new HtmlString(_n(message, plural, n, args));
        }

        public static string _n(string message, string plural, int n, params object[] args)
        {
            return String.Format(Internationalization.GetTextPlural(message, plural, n), args.Prepend(n));
        }

        public HtmlString _c(string context, Func<dynamic, HelperResult> message, params object[] args)
        {
            return _rc(context, message.AsString(), args);
        }

        public HtmlString _rc(string context, string message, params object[] args)
        {
            return new HtmlString(_c(context, message, args));
        }

        public static string _c(string context, string message, params object[] args)
        {
            return String.Format(Internationalization.GetText(message, context: context), args);
        }

        public HtmlString _nc(string context, Func<dynamic, HelperResult> message, Func<dynamic, HelperResult> plural, int n, params object[] args)
        {
            return _rnc(context, message.AsString(), plural.AsString(), n, args);
        }

        public HtmlString _rnc(string context, string message, string plural, int n, params object[] args)
        {
            return new HtmlString(_nc(context, message, plural, n, args));
        }

        public static string _nc(string context, string message, string plural, int n, params object[] args)
        {
            return String.Format(Internationalization.GetTextPlural(message, plural, n, context: context), args.Prepend(n));
        }
    }
}
