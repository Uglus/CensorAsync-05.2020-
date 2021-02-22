using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CensorAsyncStartConsole
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("Starting WpfApplication1.exe...");

            var domain = AppDomain.CreateDomain("CensorAsync_Exam");
            try
            {
                domain.ExecuteAssembly(@"..\..\..\CensorAsync_Exam\bin\Debug\CensorAsync_Exam.exe");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                AppDomain.Unload(domain);
            }

            Console.WriteLine("WpfApplication1.exe exited, exiting now.");
            Console.ReadLine();
        }
    }
}
