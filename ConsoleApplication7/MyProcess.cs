using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication7
{
    /// <summary>
    /// 进程类
    /// </summary>
    public class MyProcess
    {

        /// <summary>
        /// 进程ID
        /// </summary>
        public int ProceID;
        /// <summary>
        /// 进程名称
        /// </summary>
        public string ProceName;
        /// <summary>
        /// 执行路径
        /// </summary>
        public string ExecPath;
        /// <summary>
        /// 创建者
        /// </summary>
        public string Creator;
        /// <summary>
        /// 创建时间
        /// </summary>
        public string CreateTime;
        /// <summary>
        /// 线程数
        /// </summary>
        public int ThreadsCount;
        /// <summary>
        /// CPU占用率
        /// </summary>
        public double CpuPercent;
        /// <summary>
        /// 上次CPU时间
        /// </summary>
        public long OldCpuTime;
        /// <summary>
        /// 物理内存(KB)
        /// </summary>
        public long MemoryKB;
        /// <summary>
        /// 虚拟内存(KB)
        /// </summary>
        public long VirtualKB;
        /// <summary>
        /// 父进程名
        /// </summary>
        public string ParentProce;
        /// <summary>
        /// 公司信息
        /// </summary>
        public string CompanyName;
        /// <summary>
        /// 是否是目标进程
        /// </summary>
        public bool Target;
        /// <summary>
        /// 是否活动
        /// </summary>
        public bool Active;
    }
}
