using System;
using System.Web.Mvc;

namespace GettextDotNet.MVC
{
    public abstract class LocalizingController : Controller
    {
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
