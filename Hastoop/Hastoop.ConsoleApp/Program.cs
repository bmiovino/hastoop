using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hastoop.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                NameNode N = new NameNode(
                        NameNodeDirectory: "search_" + ConfigurationManager.AppSettings["SearchFileName"],
                        SearchDirectory: ConfigurationManager.AppSettings["SearchDir"],
                        SearchString: ConfigurationManager.AppSettings["SearchString"]
                        );

                bool isRegex = args.Select(s => s == "-regex").Any();
                
                switch (ConfigurationManager.AppSettings["Mode"])
                {
                    case "SEARCH":
                        if (isRegex)
                            N.MakeRegex();
                        N.BuildDataFile();
                        break;
                    case "REDUCE":
                        break;
                    case "EXTRACT":
                        N.Extract();
                        break;
                    case "EXTRACTUNIQUE":
                        N.ExtractUnique();
                        break;
                    case "FILTER":
                        break;
                    case "SCAN":
                        N.Scan();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("Done.");
            Console.ReadLine();
        }
    }
}
