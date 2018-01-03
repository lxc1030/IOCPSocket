using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class IOCPDataManager
{
    public static IOCPDataManager instance;

    public Dictionary<string, IOCPData> outLine;
    public Dictionary<IntPtr, IOCPData> pool;

    public IOCPDataManager()
    {
        instance = this;
        outLine = new Dictionary<string, IOCPData>();
        pool = new Dictionary<IntPtr, IOCPData>();
    }

    #region Pool操作相关
    internal IOCPData GetData(IntPtr handle)
    {
        if (pool.ContainsKey(handle))
        {
            return pool[handle];
        }
        return null;
    }

    internal List<IOCPData> GetUsedDatas()
    {
        lock (pool)
        {
            return pool.Values.ToList();
        }
    }

    internal void Push(IntPtr handle, IOCPData data)
    {
        lock (pool)
        {
            pool.Add(handle, data);
        }
    }


    #endregion

    #region outLine操作相关
    internal void OutLine(IntPtr handle)
    {
        IOCPData data = GetData(handle);
        outLine.Add("", data);
    }
    #endregion

}

