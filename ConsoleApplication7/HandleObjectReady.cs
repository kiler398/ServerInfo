using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication7
{
    /// <summary>
    /// 该类用于监测Wmi异步调用方法是否已经返回
    /// </summary>
    public class HandleObjectReady
    {
        private bool complete = false;
        private ManagementBaseObject obj;


        public void Done(object sender, ObjectReadyEventArgs e)
        {
            complete = true;
            obj = e.NewObject;
        }

        public bool Complete
        {
            get
            {
                return complete;
            }
        }

        public ManagementBaseObject Obj
        {
            get
            {
                return obj;
            }
        }
    }
}
