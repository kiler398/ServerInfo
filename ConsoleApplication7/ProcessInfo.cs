using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;

namespace ConsoleApplication7
{
    #region 委    托
    public delegate void DlgProceNew(MyProcess pProce);
    public delegate void DlgProceClosed(MyProcess pProce);
    public delegate void DlgProceRefresh(MyProcess pProce, double pCpuPercent);
    public delegate void DlgDetailList(MyProcess pProce);
    #endregion

    /// <summary>
    /// 该类提供获取进程信息的功能
    /// </summary>
    public class ProcessInfo
    {
        #region 私有成员
        /// <summary>
        /// 字典类，用于保存各进程信息
        /// </summary>
        private Dictionary<int, MyProcess> mDict;
        /// <summary>
        /// 当前总CPU时间
        /// </summary>
        private double mCurrentTotalCpuTime;
        /// <summary>
        /// CPU空闲比率
        /// </summary>
        private double mIdleCpuPercent;
        /// <summary>
        /// 当前进程
        /// </summary>
        private Process[] mCurrentAll;
        /// <summary>
        /// 性能计数器，用于获取CPU空闲百分比
        /// </summary>
        private static PerformanceCounter mIdle = new PerformanceCounter("Process", "% Processor Time", "Idle");
        /// <summary>
        /// 性能计数器，用于获取CPU总利用率
        /// </summary>
        private static PerformanceCounter mTotal = new PerformanceCounter("Process", "% Processor Time", "_Total");
        private object mLock = new object();
        #endregion

        #region 事    件
        /// <summary>
        /// 当出现新进程时触发该事件
        /// </summary>
        public event DlgProceNew HandleProceNew;
        /// <summary>
        /// 当进程关闭时触发该事件
        /// </summary>
        public event DlgProceClosed HandleProceClosed;
        /// <summary>
        /// 当进程信息更新时触发该事件
        /// </summary>
        public event DlgProceRefresh HandleProceRefresh;
        /// <summary>
        /// 当获取进程详细信息时触发该事件
        /// </summary>
        public event DlgDetailList HandleDetailList;
        #endregion

        /// <summary>
        /// 构造器
        /// </summary>
        public ProcessInfo()
        {
        }

        public Dictionary<int, MyProcess> MDict
        {
            get { return mDict; }
            set { mDict = value; }
        }

        /// <summary>
        /// 提供刷新进程列表的功能
        /// </summary>
        /// <param name="pReLoadAll">是否强制完全重新加载</param>
        public void RefreshProceList(bool pReLoadAll)
        {
            //当前进程列表
            this.mCurrentAll = Process.GetProcesses();

            #region 初始化或重新加载
            if (this.mDict == null || pReLoadAll)
            {
                //初始化字典类
                this.mDict = new Dictionary<int, MyProcess>();
                //获取基本信息
                this.FillBaseInfo(this.mCurrentAll);
                //获取需要更新的信息
                this.FillNeedRefreshInfo(this.mCurrentAll);
                return;
            }
            #endregion

            #region 进程关闭处理
            //使用lock避免多线程造成mDict不一致(?)
            lock (this.mLock)
            {
                //foreach枚举时不能删除原集合元素，记录相应的key，循环完成之后再删除
                List<int> list = new List<int>();
 
                MyProcess mp = null;
                foreach (int id in list)
                {
                    if (this.mDict.ContainsKey(id))
                    {
                        mp = this.mDict[id];
                        this.mDict.Remove(id);
                        //触发进程关闭事件
                        if (this.HandleProceClosed != null)
                        {
                            this.HandleProceClosed(mp);
                        }
                    }
                }
            }
            #endregion

            #region 进程信息刷新(包括新增)
            for (int i = 0; i < this.mCurrentAll.Length; i++)
            {
                //新增进程
                if (!this.mDict.ContainsKey(this.mCurrentAll[i].Id))
                {
                    this.FillBaseInfo(this.mCurrentAll[i]);
                }
            }
            this.FillNeedRefreshInfo(this.mCurrentAll);
            #endregion
        }

        /// <summary>
        /// 提供刷新进程其它详细信息的功能
        /// </summary>
        /// <param name="pID">进程ID</param>
        public void RefreshDetailList(int pID)
        {
            this.FillDetailUseWmi(pID);
        }

        /// <summary>
        /// 获取指定进程集合中各进程的基本信息
        /// </summary>
        /// <param name="pCurrentAll"></param>
        private void FillBaseInfo(params Process[] pCurrentAll)
        {
            MyProcess mp = null;
            for (int i = 0; i < pCurrentAll.Length; i++)
            {
  
                //211212
                mp = new MyProcess();
                mp.ProceID = pCurrentAll[i].Id;
                mp.ProceName = pCurrentAll[i].ProcessName;
               
                try
                {
                    //对于空闲进程idle等，无法获取其主模块文件名
                    mp.ExecPath = pCurrentAll[i].MainModule.FileName.ToLower();//
                    mp.CreateTime = pCurrentAll[i].StartTime.ToString();
                    mp.CompanyName = pCurrentAll[i].MainModule.FileVersionInfo.CompanyName;
                }
                catch
                {
                }

                //根据执行文件路径，判断进程是否为目标进程
                if (mp.ExecPath != null)
                {
                    mp.Target = true;
                }

                //初始化“上一次CPU时间”为0
                mp.OldCpuTime = 0;
                this.mDict.Add(mp.ProceID, mp);

                //触发新进程事件
                if (this.HandleProceNew != null)
                {
                    this.HandleProceNew(mp);
                }
            }
            mp = null;
        }

        /// <summary>
        /// 获取指定进程集合中各进程需要刷新的信息
        /// </summary>
        /// <param name="pCurrentAll"></param>
        private void FillNeedRefreshInfo(params Process[] pCurrentAll)
        {
            for (int i = 0; i < pCurrentAll.Length; i++)
            {
  
                this.mDict[pCurrentAll[i].Id].MemoryKB = pCurrentAll[i].WorkingSet64 / 1024;
                this.mDict[pCurrentAll[i].Id].VirtualKB = pCurrentAll[i].VirtualMemorySize64 / 1024;
                this.mDict[pCurrentAll[i].Id].ThreadsCount = pCurrentAll[i].Threads.Count;
            }

            //以下计算CPU利用率，不放在同一for循环中是为了避免循环时间间隔造成CPU利用率的误差
            this.mCurrentTotalCpuTime = this.CalCurrentTotalCpuTime();
            for (int i = 0; i < pCurrentAll.Length; i++)
            {
                //空闲进程idle
                if (pCurrentAll[i].Id == 0)
                {
                    this.mDict[pCurrentAll[i].Id].CpuPercent = this.mIdleCpuPercent;
                }
                else
                {
                    try
                    {
                        //无法保证进程不会中途退出，此时无法获取其CUP时间
                        long ms = (long)pCurrentAll[i].TotalProcessorTime.TotalMilliseconds;
                        double d = (ms - this.mDict[pCurrentAll[i].Id].OldCpuTime) * 1.0 / this.mCurrentTotalCpuTime;
                        this.mDict[pCurrentAll[i].Id].CpuPercent = d;
                        this.mDict[pCurrentAll[i].Id].OldCpuTime = ms;
                    }
                    catch
                    {
                    }
                }

                //调用刷新事件
                if (this.HandleProceRefresh != null)
                {
                    this.HandleProceRefresh(this.mDict[pCurrentAll[i].Id], 100 - this.mIdleCpuPercent);
                }
            }
        }

        /// <summary>
        /// 使用Wmi获取指定进程的创建者等信息
        /// </summary>
        /// <param name="pID">进程ID</param>
        private void FillDetailUseWmi(int pID)
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ProcessID=" + pID);
            ManagementObjectCollection moc = searcher.Get();

            ManagementOperationObserver observer = new ManagementOperationObserver();
            HandleObjectReady hor = new HandleObjectReady();
            //监测异步方法是否已成功返回
            observer.ObjectReady += new ObjectReadyEventHandler(hor.Done);

            foreach (ManagementObject mo in moc)
            {
                //异步调用该对象的GetOwner方法，获取进程创建者
                mo.InvokeMethod(observer, "GetOwner", null);
                //等待异步调用返回
                while (!hor.Complete)
                {
                    System.Threading.Thread.Sleep(500);
                }

                string user = "";
                //判断获取用户名的操作是否成功
                if (hor.Obj["returnValue"].ToString() == "0")
                {
                    user = hor.Obj.Properties["User"].Value.ToString();
                }
                //判断字典中是否已移除该项
                if (!this.mDict.ContainsKey(pID))
                {
                    return;
                }
                if (mo["ParentProcessID"] != null && this.mDict.ContainsKey(Convert.ToInt32(mo["ParentProcessID"])))
                {
                    //根据父进程ID获取父进程名称
                    this.mDict[pID].ParentProce = this.mDict[Convert.ToInt32(mo["ParentProcessID"])].ProceName;
                }
                this.mDict[pID].Creator = user;

                //触发刷新进程详细信息事件
                if (this.HandleDetailList != null)
                {
                    this.HandleDetailList(this.mDict[pID]);
                }
            }

            //释放资源
            searcher.Dispose();
            searcher = null;
            moc.Dispose();
            moc = null;
            observer = null;
            hor = null;
        }

        /// <summary>
        /// 计算当前总CPU时间
        /// </summary>
        /// <returns></returns>
        private double CalCurrentTotalCpuTime()
        {
            double d = 0;
            //获取性能计数器值
            double idlePercent = mIdle.NextValue();
            double totalPercent = mTotal.NextValue();
            //避免除0异常
            if (totalPercent == 0)
            {
                this.mIdleCpuPercent = 0;
            }
            else
            {
                //可能遇到多核或超线程CPU，CPU空闲进程比率不能直接使用计数器的值
                this.mIdleCpuPercent = idlePercent * 100 / totalPercent;
            }

            //以下获取上一次计算至当前总的非空闲CPU时间
            foreach (Process p in this.mCurrentAll)
            {
                //对空闲进程及中途退出的进程不做处理
                if (p.Id == 0)
                {
                    continue;
                }

                if (this.mDict == null || !this.mDict.ContainsKey(p.Id))
                {
                    d += p.TotalProcessorTime.TotalMilliseconds;
                }
                else
                {
                    d += p.TotalProcessorTime.TotalMilliseconds - this.mDict[p.Id].OldCpuTime;
                }
            }

            //当前非空闲CPU时间/当前非空闲时间所占比率=当前总CPU时间
            //return d / (totalPercent - idlePercent);
            return d / (100 - mIdleCpuPercent);
        }

 

 
    }
}
