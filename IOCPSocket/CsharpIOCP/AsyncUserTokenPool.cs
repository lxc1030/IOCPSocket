using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
namespace CsharpIOCP
{
    internal class AsyncUserTokenPool
    {
        Stack<AsyncUserToken> pool;//初始化时生成的，可用的所有的异步发接收对象池
        List<AsyncUserToken> used;

        // Initializes the object pool to the specified size
        //
        // The "capacity" parameter is the maximum number of 
        // AsyncUserToken objects the pool can hold
        public AsyncUserTokenPool(int capacity)
        {
            pool = new Stack<AsyncUserToken>(capacity);
            used = new List<AsyncUserToken>();
        }

        // Add a SocketAsyncEventArg instance to the pool
        //
        //The "item" parameter is the AsyncUserToken instance 
        // to add to the pool
        public bool Push(AsyncUserToken item)
        {
            if (item == null)
            {
                //throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null");
                return false;
            }
            lock (pool)
            {
                pool.Push(item);
                used.Remove(item);
            }
            return true;
        }

        // Removes a AsyncUserToken instance from the pool
        // and returns the object removed from the pool
        public AsyncUserToken Pop()
        {
            lock (pool)
            {
                AsyncUserToken temp = null;
                if (pool.Count > 0)
                {
                    temp = pool.Pop();
                    used.Add(temp);
                }
                return temp;
            }
        }
        public int Count()
        {
            return pool.Count;
        }

        public void CheckIsConnected(int iCheckInterval, Action<AsyncUserToken> closeAction)
        {
            lock (used)
            {
                for (int i = used.Count - 1; i >= 0; i--)
                {
                    if (used[i].userInfo.heartbeatTime.AddMilliseconds(iCheckInterval).CompareTo(DateTime.Now) < 0)
                    {
                        Console.WriteLine(used[i].userInfo.Rigiter + " the heartbeat timeout ！");
                        //
                        closeAction(used[i]);
                    }
                }
            }
        }

        public AsyncUserToken GetTokenByMemberID(string memberID)
        {
            lock (used)
            {
                AsyncUserToken userToken = null;
                for (int i = 0; i < used.Count; i++)
                {
                    if (used[i].userInfo.Register == null)
                    {
                        continue;
                    }
                    if (used[i].userInfo.Register.userID == memberID)
                    {
                        userToken = used[i];
                        break;
                    }
                }
                return userToken;
            }
        }
    }
}