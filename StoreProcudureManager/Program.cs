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
            if (args.Length < 0)
            {
                showUsage();
                return;
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
                        if (args[2] == "all")
                        {
                            SPMCore.InstallStoreProcedures(connection_string, args[3], InstallMode.All, out error);
                        }
                        else if (args[2] == "new")
                        {
                            SPMCore.InstallStoreProcedures(connection_string, args[3], InstallMode.New, out error);
                        }
                        else if (args[2] == "replace")
                        {
                            SPMCore.InstallStoreProcedures(connection_string, args[3], InstallMode.Replace, out error);
                        }
                        else
                        {
                            Console.WriteLine("-i command expected mode in (all, new, replace)");
                            break;
                        }
                        if (error != "")
                            Console.WriteLine(error);
                        break;
                    case "v":
                        var results = SPMCore.ValidateStoreProcedures(connection_string, args[2], out error);
                        if (error == "")
                        {
                            Helper.ShowValidateResult(results);
                        }
                        else
                        {
                            Console.WriteLine(error);
                        }
                        break;
                    case "con":
                        var con = new SPMConsole(connection_string, args[2]);
                        con.Start();
                        break;
                    default:
                        Console.WriteLine("Invalid mode use -p, -i, -v or just type \"spm\" to see the usage guide");
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
            Console.WriteLine("   spm <DBName> <-p|-i <mode>|-v> <packed file>");
            Console.WriteLine("");
            Console.WriteLine("Example:");
            Console.WriteLine("   spm MyDB -p webdb.zip");
            Console.WriteLine("");
            Console.WriteLine("will save all SPs in 'MyDB' to packed file 'webdb.zip'");
            Console.WriteLine("");
            Console.WriteLine("<DBName>         name of Database");
            Console.WriteLine("-p               to packed all SPs");
            Console.WriteLine("-i <mode>        to install (with mode = all|new|replace)");
            Console.WriteLine("-v               to validate what SPs in DB that diffent from SPs in packed file");
            Console.WriteLine("-con             interactive console mode");
        }
    }
}
