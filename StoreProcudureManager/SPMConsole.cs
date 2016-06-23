using StoreProcudureManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StoreProcudureManager
{
    public class SPMConsole
    {
        private string connection_string;
        private string file_path;
        private List<string> sp_name_list;
        private List<ValidateResultModel> validate_results;

        public SPMConsole(string connection_string, string file_path)
        {
            this.connection_string = connection_string;
            this.file_path = file_path;
        }

        public void Start()
        {
            string error = "";
            validate_results = SPMCore.ValidateStoreProcedures(connection_string, file_path, out error);
            if(error != "")
            {
                Console.WriteLine(error);
                return;
            }

            Helper.ShowValidateResult(validate_results);
            sp_name_list = SPMCore.GetSPListInPackFile(file_path);

            Console.WriteLine("\nType 'help' to see list of command");

            while (true)
            {
                Console.Write("SPM > ");
                string cmd = smartConsoleReadline();
                Console.WriteLine("");
                cmd = new Regex("\\s{2,}").Replace(cmd,"");
                string[] tokens = cmd.Split(' ');

                if (tokens[0] == "help")
                {
                    showHelp();
                }
                else if (tokens[0] == "install")
                {
                    if (tokens.Length > 1)
                    {
                        if (!string.IsNullOrWhiteSpace(tokens[1]))
                        {
                            SPMCore.InstallOneStoreProcedure(connection_string, file_path, tokens[1], validate_results.Where(r => r.ProcedureName == tokens[1]).First().ResultType);
                        }
                    }
                }
                else if (tokens[0] == "quit" || tokens[0] == "q")
                {
                    break;
                }
                else if (tokens[0] == "list")
                {
                    validate_results = SPMCore.ValidateStoreProcedures(connection_string, file_path, out error);
                    Helper.ShowValidateResult(validate_results);
                    Console.WriteLine("");
                }
            }
        }

        private class FilterList
        {
            private List<string> list;
            public bool Invalidate { get; set; }
            private int index;
            public int Size
            {
                get
                {
                    return list.Count;
                }
            }

            public FilterList()
            {
                list = new List<string>();
                Invalidate = true;
            }

            public void SetList(List<string> lst)
            {
                list = lst;
                Invalidate = lst.Count == 0;
                index = -1;
            }
            public string Next()
            {
                if (Invalidate)
                    return "";
                else
                {
                    index++;
                    if (index == list.Count)
                        index = 0;
                    return list[index];
                }
            }
        }

        private string smartConsoleReadline()
        {
            StringBuilder command = new StringBuilder(5000);
            FilterList filter_list = new FilterList();
            int last_index = 0;

            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Tab)
                {
                    if (filter_list.Invalidate)
                    {
                        for (last_index = command.Length - 1; last_index >= 0; last_index--)
                        {
                            if (command[last_index] == ' ')
                            {
                                last_index++;
                                break;
                            }
                        }

                        if (last_index > 0)
                        {
                            filter_list.SetList(sp_name_list.Where(n => n.StartsWith(command.ToString().Substring(last_index))).ToList());
                        }

                        if (filter_list.Size > 0)
                        {
                            var name = filter_list.Next();
                            Console.CursorLeft -= command.Length - last_index;
                            Console.Write(name);
                            command.Insert(last_index, name);
                            command.Length = last_index + name.Length;
                        }
                    }
                    else
                    {
                        var name = filter_list.Next();
                        Console.CursorLeft -= command.Length - last_index;
                        Console.Write(name);    
                        command.Insert(last_index, name);
                        command.Length = last_index + name.Length;
                    }
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    Console.Write("\n");
                    filter_list.Invalidate = true;
                    break;
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if(command.Length > 0)
                    {
                        command.Length = command.Length - 1;
                        Console.CursorLeft--;
                        Console.Write(" ");
                        Console.CursorLeft--;
                        filter_list.Invalidate = true;
                    }
                }
                else if(key.KeyChar >= 32 && key.KeyChar < 128)
                {
                    Console.Write(key.KeyChar);
                    command.Append(key.KeyChar);
                    filter_list.Invalidate = true;
                }
            }

            return command.ToString();
        }

        private void showHelp()
        {
            Console.WriteLine("list                 list result of SP validation");
            Console.WriteLine("install <SPName>     install SP");
            Console.WriteLine("quit                 quit console");
            Console.WriteLine("");
        }
    }
}
