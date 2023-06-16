using Microsoft.Win32;
using System.Text;
using System.Text.RegularExpressions;

namespace RegRecover
{
    internal class Program
    {
        async static Task Main(string[] args)
        {

            RegistryKey key = null;
            RegistryKey components = null;
            string[] arguments = new [] { "-components", "-log" };
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

                ArgumentException.ThrowIfNullOrEmpty(hivePath, "Hive Path");
                ArgumentException.ThrowIfNullOrEmpty(logPath, "Log Path");

                HiveLoader.GrantPrivileges();

                if (HiveLoader.LoadHive(hivePath) != 0) throw new Exception("Unable to load the COMPONENTS hive");

                components = HiveLoader.HKLM.OpenSubKey(@"REPAIR\DerivedData\Components");

                string[] lines = await File.ReadAllLinesAsync(logPath);
                string keyName = string.Empty;

                foreach (string line in lines)
                {
                    if (line.StartsWith("Key:"))
                    {
                        keyName = line.Split("\\", StringSplitOptions.TrimEntries)[3];
                        key = components?.OpenSubKey(keyName, true);
                    }

                    if (line.StartsWith("Suggestion:"))
                    {
                        string suggestion = line.Trim().Replace("Suggestion:", string.Empty);
                        suggestion = pattern.Replace(suggestion, " ");

                        byte[] data = Encoding.ASCII.GetBytes(suggestion);

                        key.SetValue("identity", data, RegistryValueKind.Binary);
                        key.Close();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                Console.WriteLine(e.Message);
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