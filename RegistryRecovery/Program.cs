using Microsoft.Win32;
#if NET45
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Expressions;
#endif
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace RegRecover
{
#if NET45
    class Program
    {

#else
    internal class Program
    {
#endif


#if NET45
        static void Main(string[] args)
#else
        static async Task Main(string[] args)
#endif
        {
            RegistryKey key = null;
            RegistryKey components = null;
            string[] arguments = new[] { "-components", "-log" };
            Regex pattern = new Regex("\\s+");

            try
            {
                string hivePath = string.Empty;
                string logPath = string.Empty;

                if (args.Length == 4 && args.Intersect(arguments).Count() == 2)
                {
                    int hiveIndex = Array.FindIndex(args, p => p.Equals("-components"));
                    int logIndex = Array.FindIndex(args, p => p.Equals("-log"));

                    hivePath = args[hiveIndex + 1];
                    logPath = args[logIndex + 1];
                }

#if NET45
                if (string.IsNullOrEmpty(hivePath) | (File.Exists(hivePath) == false))
                    throw new ArgumentException("Argument error: -components not specified or file does not exist.");
                if (string.IsNullOrEmpty(logPath) | (File.Exists(logPath) == false))
                    throw new ArgumentException("Arguemnt error: -log file not specified or file does not exist");
#else
                ArgumentException.ThrowIfNullOrEmpty(hivePath, "Hive Path");
                ArgumentException.ThrowIfNullOrEmpty(logPath, "Log Path");
#endif

                HiveLoader.GrantPrivileges();

                if (HiveLoader.LoadHive(hivePath) != 0) throw new Exception("Unable to load the COMPONENTS hive");

                components = HiveLoader.HKLM.OpenSubKey(@"REPAIR\DerivedData\Components");

#if NET45
                string[] lines = File.ReadAllLines(logPath);
#else
                string[] lines = await File.ReadAllLinesAsync(logPath);
#endif
                string keyName = string.Empty;
                int processedKeys = 0;

                foreach (string line in lines)
                {
                    if (line.StartsWith("Key:"))
                    {
#if NET45
                        string[] splits = line.Split('\\');
                        foreach (string split in splits)
                            split.Trim();
                        keyName = splits[3];
#else
                        keyName = line.Split("\\", StringSplitOptions.TrimEntries)[3];
#endif
                        key = components?.OpenSubKey(keyName, true);
                    }

                    if (line.StartsWith("Suggestion:"))
                    {
                        string suggestion = line.Replace("Suggestion:", string.Empty).Trim();
                        suggestion = pattern.Replace(suggestion, " ");

                        byte[] data = Encoding.ASCII.GetBytes(suggestion);

                        key.SetValue("identity", data, RegistryValueKind.Binary);
                        Console.WriteLine(key);
                        key.Close();

                        processedKeys++;
                    }
                }
                Console.WriteLine(processedKeys + " keys processed.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                Console.WriteLine(e.Message);
                Console.WriteLine("Usage: RegRecover -components \"\\path\\to\\COMPONENTS\" -log \"\\path\\to\\ComponentsScanner.\"");
                Environment.Exit(1);
            }
            finally
            {
                key?.Close();
                components?.Close();
                HiveLoader.UnloadHive("REPAIR");
                HiveLoader.RevokePrivileges();
            }
        }
    }
}