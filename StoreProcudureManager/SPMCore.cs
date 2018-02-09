using StoreProcudureManager.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StoreProcudureManager
{
    public class SPMCore
    {
        public static void SaveAllStoreProcudures(string connection_string, string filename)
        {
            List<string> checksum = new List<string>();

            using (var conn = new SqlConnection(connection_string))
            {
                conn.Open();
                string sql = @"select object_name(object_id) as sp_name, definition
                                from sys.sql_modules where OBJECTPROPERTY(object_id, 'IsProcedure') = 1";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        using (var spmFile = new FileStream(filename, FileMode.Create))
                        {
                            using (ZipArchive archive = new ZipArchive(spmFile, ZipArchiveMode.Create))
                            {
                                ZipArchiveEntry entry;
                                while (reader.Read())
                                {
                                    if (reader["sp_name"].ToString().StartsWith("sp_"))
                                        continue;

                                    entry = archive.CreateEntry(string.Format("{0}.sql", reader["sp_name"].ToString()));
                                    using (StreamWriter sw = new StreamWriter(entry.Open()))
                                    {
                                        sw.Write(reader["definition"].ToString());
                                    }

                                    SHA256 sha256 = SHA256Managed.Create();
                                    byte[] hashValue = sha256.ComputeHash(Encoding.UTF8.GetBytes(reader["definition"].ToString()));

                                    checksum.Add(string.Format("{0}:{1}", reader["sp_name"].ToString(), BitConverter.ToString(hashValue).ToLower().Replace("-", "")));
                                }

                                entry = archive.CreateEntry("checksum.txt");
                                using (StreamWriter sw = new StreamWriter(entry.Open()))
                                {
                                    foreach (var line in checksum)
                                    {
                                        sw.WriteLine(line);
                                    }
                                }
                            }
                        }
                    }

                }

                conn.Close();
            }
        }

        public static List<string> GetSPListInPackFile(string filename)
        {
            List<string> lst = new List<string>();

            // Prepare Dict
            using(FileStream spmFile = new FileStream(filename, FileMode.Open)){
                using (var archive = new ZipArchive(spmFile))
                {
                    var entry = archive.GetEntry("checksum.txt");
                    if (entry == null)
                    {
                        return null;
                    }

                    using (StreamReader sr = new StreamReader(entry.Open()))
                    {
                        string line;
                        while((line = sr.ReadLine()) != null)
                        {
                            string[] token = line.Split(new string[] { ":" }, StringSplitOptions.None);
                            lst.Add(token[0]);
                        }
                    }
                }
            }

            return lst;
        }

        public static List<ValidateResultModel> ValidateStoreProcedures(string connection_string, string filename, out string error_string)
        {
            Dictionary<string, string> checksum_dict = new Dictionary<string, string>();
            List<ValidateResultModel> results = new List<ValidateResultModel>();

            if(!File.Exists(filename)){
                error_string = "Cannot find file " + filename;
                return null;
            }

            // Prepare Dict
            using(FileStream spmFile = new FileStream(filename, FileMode.Open)){
                using (var archive = new ZipArchive(spmFile))
                {
                    var entry = archive.GetEntry("checksum.txt");
                    if (entry == null)
                    {
                        error_string = "Cannot find Signature DB. The file might be corrupted";
                        return null;
                    }

                    using (StreamReader sr = new StreamReader(entry.Open()))
                    {
                        string line;
                        while((line = sr.ReadLine()) != null)
                        {
                            string[] token = line.Split(new string[] { ":" }, StringSplitOptions.None);
                            checksum_dict[token[0]] = token[1];
                        }
                    }
                }
            }
            
            using (var conn = new SqlConnection(connection_string))
            {
                conn.Open();
                string sql = @"select object_name(object_id) as sp_name, definition
                                from sys.sql_modules where OBJECTPROPERTY(object_id, 'IsProcedure') = 1";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader["sp_name"].ToString().StartsWith("sp_"))
                                continue;

                            SHA256 sha256 = SHA256Managed.Create();
                            byte[] hashValue = sha256.ComputeHash(Encoding.UTF8.GetBytes(reader["definition"].ToString()));
                            string hashString = BitConverter.ToString(hashValue).ToLower().Replace("-", "");

                            string sp_name = reader["sp_name"].ToString();
                            var result = new ValidateResultModel(){ ProcedureName = sp_name };

                            if (checksum_dict.ContainsKey(sp_name))
                            {
                                if (hashString == checksum_dict[sp_name])
                                    result.ResultType = RESULT_TYPE.MATCHED;
                                else
                                    result.ResultType = RESULT_TYPE.DIFFERENT;
                                checksum_dict.Remove(sp_name);
                            }
                            else
                                result.ResultType = RESULT_TYPE.NOT_FOUND;
                            
                            results.Add(result);
                        }
                    }
                }
                conn.Close();

                // List all New Store Procudures
                foreach (var item in checksum_dict)
                {
                    results.Add(new ValidateResultModel()
                    {
                        ProcedureName = item.Key,
                        ResultType = RESULT_TYPE.NEW
                    });
                }
            }

            error_string = "";
            return results;
        }

        public static void InstallStoreProcedures(string connection_string, string filename, InstallMode mode, out string error_string)
        {
            var result_list = ValidateStoreProcedures(connection_string, filename, out error_string);

            if(!string.IsNullOrEmpty(error_string))
                return;

            using(FileStream spmFile = new FileStream(filename, FileMode.Open))
            {
                using(ZipArchive archive = new ZipArchive(spmFile))
                {
                    using (var conn = new SqlConnection(connection_string))
                    {
                        conn.Open();
                        
                        foreach (var result in result_list)
                        {
                            if (result.ResultType != RESULT_TYPE.MATCHED && result.ResultType != RESULT_TYPE.NOT_FOUND)
                            {
                                if ((result.ResultType == RESULT_TYPE.NEW && mode != InstallMode.Replace) ||
                                    (result.ResultType == RESULT_TYPE.DIFFERENT && mode != InstallMode.New))
                                {
                                    var entry = archive.GetEntry(string.Format("{0}.sql", result.ProcedureName));
                                    if (entry != null)
                                    {
                                        using (StreamReader sr = new StreamReader(entry.Open()))
                                        {
                                            string sql = sr.ReadToEnd();
                                            if (result.ResultType != RESULT_TYPE.NEW)
                                            {
                                                sql = Regex.Replace(sql, "CREATE PROCEDURE", "ALTER PROCEDURE", RegexOptions.IgnoreCase);
                                            }

                                            try
                                            {
                                                var cmd = new SqlCommand(sql, conn);
                                                cmd.ExecuteNonQuery();

                                                Console.WriteLine("Installed: {0}", result.ProcedureName);
                                            }
                                            catch (Exception e)
                                            {
                                                Console.WriteLine("Failed: {0}", result.ProcedureName);
                                                Console.WriteLine(e.Message);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                                Console.WriteLine("Skip: {0}", result.ProcedureName);
                        }
                        conn.Close();
                    }

                }
            }
        }

        public static void InstallOneStoreProcedure(string connection_string, string filename, string store_procedure_name, RESULT_TYPE resultType)
        {
            using (FileStream spmFile = new FileStream(filename, FileMode.Open))
            {
                using (ZipArchive archive = new ZipArchive(spmFile))
                {
                    using (var conn = new SqlConnection(connection_string))
                    {
                        conn.Open();

                        var entry = archive.GetEntry(string.Format("{0}.sql", store_procedure_name));
                        if (entry != null)
                        {
                            using (StreamReader sr = new StreamReader(entry.Open()))
                            {
                                string sql = sr.ReadToEnd();
                                if (resultType != RESULT_TYPE.NEW)
                                {
                                    sql = Regex.Replace(sql, "CREATE PROCEDURE", "ALTER PROCEDURE", RegexOptions.IgnoreCase);
                                }

                                try
                                {
                                    var cmd = new SqlCommand(sql, conn);
                                    cmd.ExecuteNonQuery();

                                    Console.WriteLine("Installed", store_procedure_name);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Failed to Install", store_procedure_name);
                                    Console.WriteLine(e.Message);
                                }
                            }
                        }

                        conn.Close();
                    }

                }
            }
        }
    }
}
