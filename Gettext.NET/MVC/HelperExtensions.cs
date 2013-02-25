using System;
using System.Linq;
using System.Web.WebPages;

namespace GettextDotNet.MVC
{
    public static class HelperExtensions
    {
        public static T[] Prepend<T>(this T[] a, params T[] b)
        {
            T[] newArray = new T[a.Length + b.Length];
            Array.Copy(b, 0, newArray, 0, b.Length);
            Array.Copy(a, 0, newArray, b.Length, a.Length);

            return newArray;
        }

        public static string AsString(this Func<dynamic, HelperResult> message)
        {
            return String.Join("\n", message(null).ToHtmlString().Split('\n').Select(s => s.Trim())).Trim();
        }
    }
}
