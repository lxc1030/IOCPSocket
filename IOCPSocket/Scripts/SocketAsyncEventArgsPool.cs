using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

/// <summary>
/// 与每个客户Socket相关联，进行Send和Receive投递时所需要的参数
/// </summary>
internal sealed class SocketAsyncEventArgsPool
{
    List<SocketAsyncEventArgs> pool; //为每一个Socket客户端分配一个SocketAsyncEventArgs，用一个List管理，在程序启动时建立
    Int32 capacity; //pool对象池的容量
    Int32 boundary; //已分配和未分配对象的边界

    internal SocketAsyncEventArgsPool(Int32 capacity)
    {
        pool = new List<SocketAsyncEventArgs>();
        boundary = 0;
        this.capacity = capacity;
    }

    /// <summary>
    /// 往pool对象池中增加新建立的对象，因为这个程序在启动时会建立好所有对象，
    /// 故这个方法只在初始化时会被调用,因此，没有加锁。
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    internal bool Add(SocketAsyncEventArgs arg)
    {
        if (arg != null && pool.Count < capacity)
        {
            pool.Add(arg);
            boundary++;
            return true;
        }
        else
            return false;
    }

    /// <summary>
    /// 获取正在使用的 SocketAsyncEventArgs 对象集合
    /// </summary>
    /// <returns></returns>
    internal List<SocketAsyncEventArgs> GetUsedSAEA()
    {
        lock (pool)
        {
            List<SocketAsyncEventArgs> lUsedSAEA = new List<SocketAsyncEventArgs>();
            for (int i = boundary; i < capacity; i++)
                lUsedSAEA.Add(pool[i]);
            return lUsedSAEA;
        }
    }

    /// <summary>
    /// 获取已使用的 SocketAsyncEventArgs 对象总数
    /// </summary>
    /// <returns></returns>
    internal string GetUsedSAEACount()
    {
        return (capacity - boundary).ToString();
    }

    /// <summary>
    /// 从对象池中取出一个对象，交给一个socket来进行投递请求操作
    /// </summary>
    /// <returns></returns>
    internal SocketAsyncEventArgs Pull()
    {
        lock (pool)
        {
            if (boundary > 0)
            {
                --boundary;
                return pool[boundary];
            }
            else
                return null;
        }
    }

    /// <summary>
    /// 一个socket客户断开，与其相关的SocketAsyncEventArgs被释放，重新投入Pool中，以备用。
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    internal bool Push(SocketAsyncEventArgs arg)
    {
        if (arg != null)
        {
            lock (pool)
            {
                int index = pool.IndexOf(arg, boundary); //找出被断开的客户
                if (index >= 0)
                {
                    if (index == boundary) //正好是边界元素
                        boundary++;
                    else
                    {
                        pool[index] = pool[boundary]; //将断开客户移到边界上，边界右移
                        pool[boundary++] = arg;
                    }
                }
                else
                    return false;
            }
            return true;
        }
        else
            return false;
    }
}
