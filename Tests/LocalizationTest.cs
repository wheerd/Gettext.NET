using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GettextDotNET.Formats;

namespace GettextDotNET.Tests
{
    [TestClass]
    public class LocalizationTest
    {
        const string TEST_PO = @"
msgid """"
msgstr """"
""Plural-Forms: nplurals=2; plural=(n!=1);\n""
""Language: de_DE""

#  Fubar
#. A welcome message for the user
#: file.cs
#, csharp-format, fuzzy
#| msgctxt ""oldcontext""
#| msgid ""Welcome, {0}.""
msgctxt ""context""
msgid ""Welcome, {0}!""
msgstr ""Willkommen, {0}!""

msgid ""One User""
msgid_plural ""{0} Users""
msgstr[0] ""Ein Benutzer""
msgstr[1] ""{0} Benutzer""";

        [TestMethod]
        public void TestLoading()
        {
            var lo = new Localization();

            lo.LoadFromString<POFormat>(TEST_PO, true);

            // Test headers
            // ============
            Assert.AreEqual(lo.Headers.Count, 2);
            Assert.IsTrue(lo.Headers.ContainsKey("Plural-Forms"));
            Assert.AreEqual(lo.Headers["Plural-Forms"], "nplurals=2; plural=(n!=1);");
            Assert.IsTrue(lo.Headers.ContainsKey("Language"));
            Assert.AreEqual(lo.Headers["Language"], "de_DE");

            Assert.AreEqual(lo.Messages.Count, 2);
            Assert.IsTrue(lo.Messages.ContainsKey("Welcome, {0}!"));
            Assert.IsTrue(lo.Messages.ContainsKey("One User"));

            // Test message meta data
            // ======================

            // Comments
            Assert.AreEqual(lo.Messages["Welcome, {0}!"].Comments.Count, 1);
            Assert.AreEqual(lo.Messages["Welcome, {0}!"].Comments[0], "A welcome message for the user");

            // Flags
            Assert.AreEqual(lo.Messages["Welcome, {0}!"].Flags.Count, 2);
            Assert.IsTrue(lo.Messages["Welcome, {0}!"].Flags.Contains("fuzzy"));
            Assert.IsTrue(lo.Messages["Welcome, {0}!"].Flags.Contains("csharp-format"));

            // Translator comments
            Assert.AreEqual(lo.Messages["Welcome, {0}!"].TranslatorComments.Count, 1);
            Assert.AreEqual(lo.Messages["Welcome, {0}!"].TranslatorComments[0], "Fubar");

            // References
            Assert.AreEqual(lo.Messages["Welcome, {0}!"].References.Count, 1);
            Assert.AreEqual(lo.Messages["Welcome, {0}!"].References[0], "file.cs");

            // Previous id
            Assert.AreEqual(lo.Messages["Welcome, {0}!"].PreviousId, "Welcome, {0}.");

            // Previous context
            Assert.AreEqual(lo.Messages["Welcome, {0}!"].PreviousContext, "oldcontext");

            // Test message data
            // =================

            // Context
            Assert.AreEqual(lo.Messages["Welcome, {0}!"].Context, "context");
            Assert.AreEqual(lo.Messages["One User"].Context, "");

            // Id
            Assert.AreEqual(lo.Messages["Welcome, {0}!"].Id, "Welcome, {0}!");
            Assert.AreEqual(lo.Messages["One User"].Id, "One User");

            // Plural
            Assert.AreEqual(lo.Messages["Welcome, {0}!"].Plural, null);
            Assert.AreEqual(lo.Messages["One User"].Plural, "{0} Users");

            // Translations
            Assert.AreEqual(lo.Messages["Welcome, {0}!"].Translations.Length, 1);
            Assert.AreEqual(lo.Messages["Welcome, {0}!"].Translations[0], "Willkommen, {0}!");

            Assert.AreEqual(lo.Messages["One User"].Translations.Length, 2);
            Assert.AreEqual(lo.Messages["One User"].Translations[0], "Ein Benutzer");
            Assert.AreEqual(lo.Messages["One User"].Translations[1], "{0} Benutzer");
        }

        [TestMethod]
        public void TestLoadFromPOString()
        {
            var lo = new Localization();

            lo.LoadFromString<POFormat>(TEST_PO);

            Assert.AreEqual(lo.Headers.Count, 2);
            Assert.AreEqual(lo.Messages.Count, 2);
        }

        [TestMethod]
        public void TestLoadFromFile()
        {
            var fileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".po";

            try
            {
                System.IO.File.WriteAllText(fileName, TEST_PO);

                var lo = new Localization();

                lo.LoadFromFile<POFormat>(fileName);

                Assert.AreEqual(lo.Headers.Count, 2);
                Assert.AreEqual(lo.Messages.Count, 2);
            }
            finally
            {
                File.Delete(fileName);
            }
        }

        [TestMethod]
        public void TestSaveToFile()
        {
            var lo = new Localization();

            lo.LoadFromString<POFormat>(TEST_PO, true);

            var fileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".po";

            try
            {
                lo.SaveToFile<POFormat>(fileName);

                using (var reader = new StreamReader(fileName))
                {
                    Assert.AreEqual(reader.ReadToEnd().Trim(), TEST_PO.Trim());
                }
            }
            finally
            {
                File.Delete(fileName);
            }
        }

        [TestMethod]
        public void TestToPOBlock()
        {
            var lo = new Localization();

            lo.LoadFromString<POFormat>(TEST_PO, true);

            Assert.AreEqual(lo.ToString<POFormat>().Trim(), TEST_PO.Trim());
        }

        [TestMethod]
        public void TestLoadFromMOFile()
        {
            var lo = new Localization();

            lo.LoadFromFile<MOFormat>("locale/test.mo");

            Assert.AreEqual(lo.Headers.Count, 11);
            Assert.AreEqual(lo.Messages.Count, 4);
            Assert.AreEqual(lo.Headers["Language"], "de_DE");
            Assert.AreEqual(lo.Messages["context2\x04User"].Context, "context2");
            Assert.AreEqual(lo.Messages["context2\x04User"].Id, "User");
            Assert.AreEqual(lo.Messages["context2\x04User"].Plural, "Users");
            Assert.AreEqual(lo.Messages["context2\x04User"].Translations.Length, 2);
            Assert.AreEqual(lo.Messages["context2\x04User"].Translations[0], "Benutzer");
            Assert.AreEqual(lo.Messages["context2\x04User"].Translations[1], "Benutzer");
        }


        static bool FileEquals(string path1, string path2)
        {
            var info1 = new FileInfo(path1);
            var info2 = new FileInfo(path2);

            if (info1.Length != info2.Length)
            {
                return false;
            }

            byte[] file1 = File.ReadAllBytes(path1);
            byte[] file2 = File.ReadAllBytes(path2);

            for (int i = 0; i < file1.Length; i++)
            {
                if (file1[i] != file2[i])
                {
                    return false;
                }
            }
            return true;
        }

        [TestMethod]
        public void TestSaveToMOFile()
        {
            var lo = new Localization();

            var fileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".mo";

            lo.LoadFromFile<MOFormat>("locale/test.mo");
            lo.SaveToFile<MOFormat>(fileName);

            Assert.IsTrue(FileEquals("locale/test.mo", fileName));

            File.Delete(fileName);
        }
    }
}
