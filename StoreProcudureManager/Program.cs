using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreProcudureManager
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                showUsage();
            }

            var conf = ConfigurationManager.ConnectionStrings["DefaultConnection"];
            var connection_string = conf.ConnectionString + ";Initial Catalog=" + args[0];

            try
            {
                var cmd = args[1].Substring(1);
                string error = "";
                switch (cmd)
                {
                    case "p":
                        SPMCore.SaveAllStoreProcudures(connection_string, args[2]);
                        break;
                    case "i":
                        SPMCore.InstallStoreProcedures(connection_string, args[2], true, out error);
                        if (error != "")
                            Console.WriteLine(error);

                        break;
                    case "si":
                        SPMCore.InstallStoreProcedures(connection_string, args[2], false, out error);
                        if (error != "")
                            Console.WriteLine(error);
                        break;
                    case "v":
                        var results = SPMCore.ValidateStoreProcedures(connection_string, args[2], out error);
                        if (error == "")
                        {
                            foreach (var result in results)
                            {
                                Console.Write("{0} : ", result.ProcedureName);
                                writeColor(result.ResultText + "\n", result.ConsoleForegroundColor);
                            }
                        }
                        else
                            Console.WriteLine(error);
                        break;
                    default:
                        Console.WriteLine("Invalid mode use -p, -i, -si, -v or just type \"spm\" to see the usage guide");
                        return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }

        static void showUsage()
        {
            Console.WriteLine("Use:");
            Console.WriteLine("   spm <DBName> <-p|-i|-v> <packed file>");
            Console.WriteLine("");
            Console.WriteLine("Example:");
            Console.WriteLine("   spm MyDB -p webdb.zip");
            Console.WriteLine("");
            Console.WriteLine("will save all SPs in 'MyDB' to packed file 'webdb.zip'");
            Console.WriteLine("");
            Console.WriteLine("<DBName>         name of Database");
            Console.WriteLine("-p               to packed all SPs");
            Console.WriteLine("-i               to install all SPs (replace if exist)");
            Console.WriteLine("-si              to safety install (won't replace at all)");
            Console.WriteLine("-v               to validate what SPs in DB that diffent from SPs in packed file");
        }

        private static void writeColor(string msg, ConsoleColor foreground_color, ConsoleColor background_color = ConsoleColor.Black, bool newline = false)
        {
            Console.ForegroundColor = foreground_color;
            Console.BackgroundColor = background_color;
            if (newline)
                Console.WriteLine(msg);
            else
                Console.Write(msg);
            Console.ResetColor();
        }
    }
}
