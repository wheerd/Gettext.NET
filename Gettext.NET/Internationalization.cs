using GettextDotNet.Formats;
using System;
using System.Collections.Generic;
using System.IO;
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

        public static string GetText(string message, string language)
        {
            if (!localizations.ContainsKey(language))
            {
                language = Settings.WorkingLanguage;
            }

            if (localizations.ContainsKey(language))
            {
                var localization = localizations[language];
                var m = localization.GetMessage(message);
                return m != null ? (String.IsNullOrEmpty(m.Translations[0]) ? message : m.Translations[0]) : message;
            }

            return message;
        }

        public static string GetTextPlural(string message, string plural, int n, string language)
        {
            // TODO
            return n == 1? plural : message;
        }
    }
}
