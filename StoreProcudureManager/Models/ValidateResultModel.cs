using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreProcudureManager.Models
{
    public enum RESULT_TYPE
    {
        MATCHED,
        DIFFERENT,
        NOT_FOUND,
        NEW
    }

    public class ValidateResultModel
    {
        public string ProcedureName { get; set; }
        public RESULT_TYPE ResultType { get; set; }

        public string ResultText
        {
            get
            {
                if (ResultType == RESULT_TYPE.MATCHED)
                    return "MATCHED";
                else if (ResultType == RESULT_TYPE.NOT_FOUND)
                    return "NOT IN PACK";
                else if (ResultType == RESULT_TYPE.DIFFERENT)
                    return "DIFFERENT";
                else
                    return "NEW PROCEDURE";
            }
        }
        public ConsoleColor ConsoleForegroundColor
        {
            get
            {
                if (ResultType == RESULT_TYPE.MATCHED)
                    return ConsoleColor.Green;
                else if (ResultType == RESULT_TYPE.NOT_FOUND)
                    return ConsoleColor.Yellow;
                else if (ResultType == RESULT_TYPE.DIFFERENT)
                    return ConsoleColor.Red;
                else
                    return ConsoleColor.Cyan;
            }
        }
        public ConsoleColor ConsoleBackgroundColor
        {
            get
            {
                return ConsoleColor.Black;
            }
        }
    }
}
