using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication7
{
    class Program
    {
        static void Main(string[] args)
        {
            ProcessInfo processInfo = new ProcessInfo();

            processInfo.RefreshProceList(true);


 
             //获取本机运行的所有进程ID和进程名,并输出哥进程所使用的工作集和私有工作集
            foreach (var item in processInfo.MDict)
            {
                Console.WriteLine("-----------------------------------------------------------");
                Console.WriteLine("进程名:{0}", item.Value.ProceName);
                Console.WriteLine("公司名:{0}", item.Value.CompanyName);
                Console.WriteLine("CPU占用率:{0:N2}%", item.Value.CpuPercent);
                Console.WriteLine("内存占用:{0}", item.Value.MemoryKB);
                Console.WriteLine("执行路径:{0}", item.Value.ExecPath);
                
            }
 
 
             Console.ReadLine();
        }
    }
}
