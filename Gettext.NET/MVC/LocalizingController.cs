using System;
using System.Web.Mvc;

namespace GettextDotNet.MVC
{
    public abstract class LocalizingController : Controller
    {
        public static string _(string message, params object[] args)
        {
            return String.Format(Internationalization.GetText(message, "de"), args);
        }

        public static string _n(string message, string plural, int n, params object[] args)
        {
            object[] newArgs = new object[args.Length + 1];
            newArgs[0] = n;
            Array.Copy(args, 0, newArgs, 1, args.Length);

            return String.Format(Internationalization.GetTextPlural(message, plural, n, "de"), newArgs);
        }

        public static string _c(string context, string message, params object[] args)
        {
            return String.Format(Internationalization.GetText(message, "de"), args);
        }
    }
}
