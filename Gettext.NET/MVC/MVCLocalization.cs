using System.Web.Routing;
using System.Linq;
using System;
using System.Globalization;

namespace GettextDotNet.MVC
{
    public static class MVCLocalization
    {
        public static void WrapRoutes()
        {
            var routes = RouteTable.Routes;
            using (routes.GetReadLock())
            {
                for (var i = 0; i < routes.Count; i++)
                {
                    routes[i] = new LocalizedRoute(routes[i]);
                }
            }
        }
    }

    public class LocalizedRoute : RouteBase
    {
        private RouteBase _base;

        public LocalizedRoute(RouteBase route)
        {
            _base = route;
        }

        public override RouteData GetRouteData(System.Web.HttpContextBase httpContext)
        {
            var path = httpContext.Request.AppRelativeCurrentExecutionFilePath;
            var lang = path.Split('/')[1];

            if (!String.IsNullOrEmpty(lang))
            {
                CultureInfo culture = null;
                try
                {
                    culture = CultureInfo.GetCultureInfo(lang);
                }
                catch { }

                if (culture != null)
                {
                    System.Threading.Thread.CurrentThread.CurrentCulture = culture;

                    var newpath = "~/" + String.Join("/", path.Split('/').Skip(2));

                    httpContext.RewritePath(newpath);
                }
            }

            var data = _base.GetRouteData(httpContext);

            if (data != null)
            {
                if (!String.IsNullOrEmpty(lang))
                {
                    data.Values.Add(Internationalization.Settings.RouteDataKey, lang);
                }
            }
            else
            {
                httpContext.RewritePath(path);
            }

            return data;
        }

        public override VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary values)
        {
            string lang = null;

            if (values.ContainsKey(Internationalization.Settings.RouteDataKey))
            {
                lang = (string)values[Internationalization.Settings.RouteDataKey];

                var t = values.ToDictionary(kv => kv.Key, kv => kv.Value);
                t.Remove(Internationalization.Settings.RouteDataKey);

                values = new RouteValueDictionary(t);
            }
            else if (requestContext.RouteData.Values.ContainsKey(Internationalization.Settings.RouteDataKey))
            {
                lang = (string)requestContext.RouteData.Values[Internationalization.Settings.RouteDataKey];
            }
            else
            {
                lang = System.Threading.Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName;
            }

            var data = _base.GetVirtualPath(requestContext, values);

            if (data != null)
            {                
                data.VirtualPath = lang + "/" + data.VirtualPath;
            }

            return data;
        }
    }
}
