using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Linq;
using System;
using System.Globalization;

namespace GettextDotNet.MVC
{
    public static class MVCLocalization
    {
        public static LocalizedRoute LocalizeRoute(this RouteCollection routes, Route route, string[] translate_values)
        {
            LocalizedRoute newroute = null;

            using (routes.GetReadLock())
            {
                var i = routes.IndexOf(route);

                newroute = new LocalizedRoute(route, translate_values);

                if (i != -1)
                {
                    routes[i] = newroute;
                }
            }

            return newroute;
        }

        public static LocalizedRoute MapLocalizedRoute(this RouteCollection routes, string name, string url, string[] translate = null)
        {
            return routes.LocalizeRoute(routes.MapRoute(name, url), translate);
        }

        public static LocalizedRoute MapLocalizedRoute(this RouteCollection routes, string name, string url, object defaults, string[] translate = null)
        {
            return routes.LocalizeRoute(routes.MapRoute(name, url, defaults), translate);
        }

        public static LocalizedRoute MapLocalizedRoute(this RouteCollection routes, string name, string url, string[] namespaces, string[] translate)
        {
            return routes.LocalizeRoute(routes.MapRoute(name, url, namespaces), translate);
        }

        public static LocalizedRoute MapLocalizedRoute(this RouteCollection routes, string name, string url, object defaults, object constraints, string[] translate = null)
        {
            return routes.LocalizeRoute(routes.MapRoute(name, url, defaults, constraints), translate);
        }

        public static LocalizedRoute MapLocalizedRoute(this RouteCollection routes, string name, string url, object defaults, string[] namespaces, string[] translate)
        {
            return routes.LocalizeRoute(routes.MapRoute(name, url, defaults, namespaces), translate);
        }

        public static LocalizedRoute MapLocalizedRoute(this RouteCollection routes, string name, string url, object defaults, object constraints, string[] namespaces, string[] translate)
        {
            return routes.LocalizeRoute(routes.MapRoute(name, url, defaults, constraints, namespaces), translate);
        }
    }

    public class LocalizedRoute : RouteBase
    {
        private RouteBase _base;

        public readonly string[] TranslatedValues;

        public LocalizedRoute(RouteBase route, string[] translate_values)
        {
            _base = route;
            TranslatedValues = translate_values ?? new string[0];
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

                foreach (var key in TranslatedValues)
                {
                    if (data.Values.ContainsKey(key))
                    {
                        var id = Internationalization.GetTextReverse(data.Values[key].ToString(), lang, "URL");

                        if (id != null)
                        {
                            data.Values[key] = id;
                        }
                    }
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

            foreach (var key in TranslatedValues)
            {
                if (values.ContainsKey(key))
                {
                    values[key] = Internationalization.GetText(values[key].ToString(), lang, "URL");
                }
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
