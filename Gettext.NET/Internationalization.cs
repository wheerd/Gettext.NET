using GettextDotNet.Formats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Web;

namespace GettextDotNet
{
    public class Internationalization
    {
        public static class Settings
        {
            public static string WorkingLanguage = "en";

            public static string BasePath = "~/locale/";

            public static bool useSubfolders = false;

            public static string CookieName = "lang";

            public static string QueryName = "lang";

            public static string SessionKey = "lang";

            public static string RouteDataKey = "language";
        }

        private static Dictionary<string, Localization> localizations;

        static Internationalization()
        {
            localizations = new Dictionary<string, Localization>(StringComparer.OrdinalIgnoreCase);
            var _basePath = Settings.BasePath;

            if (HttpContext.Current != null && Settings.BasePath.StartsWith("~"))
            {
                _basePath = HttpContext.Current.Server.MapPath(Settings.BasePath);
            }

            if (Directory.Exists(_basePath))
            {
                foreach (string filename in Directory.GetFiles(_basePath, "*.po"))
                {
                    var lo = new Localization();

                    lo.LoadFromFile<POFormat>(filename);

				    if (lo.Count > 0)
				    {
                        var lang = lo.Language ?? Path.GetFileNameWithoutExtension(filename);
                        if (!localizations.ContainsKey(lang))
                        {
                            localizations.Add(lang, lo);
                        }
				    }
                }

			}

			if (!localizations.ContainsKey(Settings.WorkingLanguage))
			{
				localizations.Add(Settings.WorkingLanguage, new Localization{Language = Settings.WorkingLanguage});
			}
        }

        public static bool ContainsLanguage(string language)
        {
            return localizations.ContainsKey(language);
        }

        public static string GetText(string message, string language = null, string context = null)
        {
            if (language == null)
            {
                language = Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName;
            }

            if (!localizations.ContainsKey(language))
            {
                language = Settings.WorkingLanguage;
            }

            if (localizations.ContainsKey(language))
            {
                var localization = localizations[language];
                var m = localization.GetMessage(message, context);

                if (m != null && !String.IsNullOrEmpty(m.Translations[0]))
                {
                    return m.Translations[0];
                }
            }

            return message;
        }

        public static string GetTextPlural(string message, string plural, int n, string language = null, string context = null)
        {
            if (language == null)
            {
                language = Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName;
            }

            if (!localizations.ContainsKey(language))
            {
                language = Settings.WorkingLanguage;
            }

            if (localizations.ContainsKey(language))
            {
                var localization = localizations[language];
                var m = localization.GetMessage(message, context);

                if (m != null)
                {
                    var p = localization.PluralForms.Evaluate(n);

                    if (!String.IsNullOrEmpty(m.Translations[p]))
                    {
                        return m.Translations[p];
                    }
                }
            }

            return n != 1? plural : message;
        }
    }
}
