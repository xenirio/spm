using StoreProcudureManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreProcudureManager
{
    public class Helper
    {
        public static void ShowValidateResult(List<ValidateResultModel> results)
        {
            foreach (var result in results)
            {
                Console.Write("{0} : ", result.ProcedureName);
                writeColor(result.ResultText + "\n", result.ConsoleForegroundColor);
            }
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
