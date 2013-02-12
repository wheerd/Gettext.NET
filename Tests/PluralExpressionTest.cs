using GettextDotNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Tests
{
    [TestClass]
    public class PluralExpressionTest
    {
        delegate int PluralForm(int i);

        [TestMethod]
        public void TestParsing()
        {
            Dictionary<string, PluralForm> tests = new Dictionary<string, PluralForm>() {    
                {"n == 1 ? 0 : 1", n => n == 1 ? 0 : 1},
                {"0", n => 0},
                {"n != 1", n => n != 1? 1 : 0},
                {"n>1", n => n > 1? 1 : 0},
                {"n%10==1 && n%100!=11 ? 0 : n != 0 ? 1 : 2", n => n%10==1 && n%100!=11 ? 0 : n != 0 ? 1 : 2},
                {"n==1 ? 0 : n==2 ? 1 : 2", n => n==1 ? 0 : n==2 ? 1 : 2},
                {"n==1 ? 0 : (n==0 || (n%100 > 0 && n%100 < 20)) ? 1 : 2", n => n==1 ? 0 : (n==0 || (n%100 > 0 && n%100 < 20)) ? 1 : 2},
                {"n%10==1 && n%100!=11 ? 0 : n%10>=2 && (n%100<10 || n%100>=20) ? 1 : 2", n => n%10==1 && n%100!=11 ? 0 : n%10>=2 && (n%100<10 || n%100>=20) ? 1 : 2},
                {"n%10==1 && n%100!=11 ? 0 : n%10>=2 && n%10<=4 && (n%100<10 || n%100>=20) ? 1 : 2", n => n%10==1 && n%100!=11 ? 0 : n%10>=2 && n%10<=4 && (n%100<10 || n%100>=20) ? 1 : 2},
                {"(n==1) ? 0 : (n>=2 && n<=4) ? 1 : 2", n => (n==1) ? 0 : (n>=2 && n<=4) ? 1 : 2},
                {"n==1 ? 0 : n%10>=2 && n%10<=4 && (n%100<10 || n%100>=20) ? 1 : 2", n => n==1 ? 0 : n%10>=2 && n%10<=4 && (n%100<10 || n%100>=20) ? 1 : 2},
                {"n%100==1 ? 0 : n%100==2 ? 1 : n%100==3 || n%100==4 ? 2 : 3", n => n%100==1 ? 0 : n%100==2 ? 1 : n%100==3 || n%100==4 ? 2 : 3},
            };

            foreach (var kv in tests)
            {
                var expr = new PluralExpression(kv.Key);

                for (int n = 0; n < 150; n++)
                {
                    Assert.AreEqual(kv.Value(n), expr.Compiled(n));
                }
            }
        }
    }
}
