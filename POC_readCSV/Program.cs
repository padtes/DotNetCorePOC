using System;
using ReadCSV;
using Microsoft.Extensions.Configuration;

namespace POC_readCSV
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
                .Build();

            string pgConStr = configuration["pgConnStr"];
            string pgSchema = configuration["pgSchema"];

            UtilCSV util = new UtilCSV();
            bool isReadOk = util.ReadCSV(pgConStr, pgSchema);
            if (isReadOk)
            {
                string nowStr = DateTime.UtcNow.ToString("yyyy_MM_dd_HH_mm_ss");

                Console.WriteLine("Done reading " + nowStr);
            }
            else
            {
                Console.WriteLine("See error log");
            }

        }
    }
}
