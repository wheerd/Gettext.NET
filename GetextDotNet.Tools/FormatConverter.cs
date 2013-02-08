using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GettextDotNet.Formats;
using GettextDotNet;
using System.IO;

namespace GetextDotNet.Tools
{
    public abstract class FormatConverter<From,To>
        where From : ILocalizationFormat, new()
        where To : ILocalizationFormat, new()
    {
        public void Run(string[] args)
        {
            var from = new From();
            var to = new To();

            if (args.Length > 2)
            {
                Console.Error.WriteLine("Usage: {0} input{1} output{2}", args[0], from.FileExtensions[0], to.FileExtensions[0]);
                return;
            }
            
            Stream input = null, output = null;

            try
            {
                if (args.Length == 0)
                {
                    input = Console.OpenStandardInput();
                }
                else
                {
                    input = File.OpenRead(args[0]);
                }

                if (args.Length < 2)
                {
                    output = Console.OpenStandardOutput();
                }
                else
                {
                    output = File.Create(args[1]);
                }

                Localization loc = new Localization();

                from.Read(loc, input, true);
                to.Write(loc, output, true);
            }
            catch(Exception e)
            {
                Console.Error.WriteLine("Error: {0}", e.Message);
                return;            
            }
            finally
            {            
                if (input != null)
                {
                    input.Close();
                }       
                if (output != null)
                {
                    output.Close();
                }
            }
        }
    }
}
