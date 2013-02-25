using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace GettextDotNet.MVC
{
    public class LocalizationHTTPModule : IHttpModule
    {
        public void Dispose()
        {
        }

        public void Init(HttpApplication context)
        {
            context.BeginRequest += context_BeginRequest;
        }

        void context_BeginRequest(object sender, EventArgs e)
        {
            var cn = Internationalization.Settings.CookieName;
            var qn = Internationalization.Settings.QueryName;
            string lang = null;

            // Grab from cookie
            if (cn != null && HttpContext.Current.Request.Cookies[cn] != null)
            {
                lang = HttpContext.Current.Request.Cookies[cn].Value;
            }

            // Grab from query parameter            
            if (HttpContext.Current.Request.QueryString[qn] != null)
            {
                lang = HttpContext.Current.Request.QueryString[qn];

                HttpContext.Current.Response.Cookies.Add(new HttpCookie(cn, lang)
                {
                    Expires = DateTime.Now.AddMonths(1)
                });
            }

            if (lang == null)
            {
                foreach (var l in HttpContext.Current.Request.UserLanguages)
                {
                    if (Internationalization.ContainsLanguage(l.Substring(0, 2)))
                    {
                        lang = l.Substring(0, 2);
                        break;
                    }
                }
            }

            if (lang != null)
            {
                var culture = new System.Globalization.CultureInfo(lang);

                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
            }
        }
    }
}
