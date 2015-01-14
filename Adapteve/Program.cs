using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using EasyHook;
using QuestorDLL;

namespace Adapteve
{
    class Program
    {
        private static int pid = new int();
        static void Main(string[] args)
        {
            try
            {
                if (args.Count() == 0)
                {
                    args = new string[2];
                    args[0] = @"C:\Program Files (x86)\CCP\EVE\bin";
                    args[1] = @"setting.ini";
                }

                if (args.Count() < 2)
                {
                    Console.WriteLine("Usage: Adapteve.exe [ExeFile-path] [IniFile] [Optional-Questor-DLL] [Optional-Questor-Parameters]");
                    Console.ReadLine();
                    return;
                }

                var exefilePath = Utility.GetExefilePath(args[0]);
                var iniFile = Utility.VerifyIniFile(args[1]);
                var dll = Directory.GetCurrentDirectory() + "/AdapteveDLL.dll";
                RemoteHooking.CreateAndInject(exefilePath,
                    "", (int)InjectionOptions.Default,
                    dll,
                    dll,
                    out pid,
                    "", iniFile);

                if (args.Count() == 4)
                {
                    var qDll = args[2];
                    var qParam = args[3];
                    System.Threading.Thread.Sleep(500);
                    RemoteHooking.Inject(pid, qDll, qDll, qParam);
                }             
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
        }
    }
}
