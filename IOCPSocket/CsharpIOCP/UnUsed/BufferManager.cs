using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace CsharpIOCP
{
    /// <summary>
    /// SocketAsyncEventArgs 缓冲区管理类，
    /// 创建一个大型缓冲区，该缓冲区可以进行分割并指定给 SocketAsyncEventArgs 对象以便用在每个套接字 I/O 操作中。
    /// 这样可以很方便地重用缓冲区，并防止堆内存碎片化。
    /// </summary>
    internal class BufferManager
    {
        int m_numBytes; //缓冲池的总容量
        byte[] m_buffer; //缓冲池
        Stack<int> m_freeIndexPool; //后进先出数据结构
        int m_currentIndex; //当前缓冲池索引
        int m_bufferSize; //单个缓冲区容量

        /// <summary>
        /// 缓冲池重载
        /// </summary>
        /// <param name="totalBytes">缓冲池的总容量</param>
        /// <param name="bufferSize">单个缓冲区容量</param>
        public BufferManager(int totalBytes, int bufferSize)
        {
            m_numBytes = totalBytes;
            m_currentIndex = 0;
            m_bufferSize = bufferSize;
            m_freeIndexPool = new Stack<int>();
            InitBuffer();
        }

        /// <summary>
        /// 初始化缓冲池
        /// </summary>
        private void InitBuffer()
        {            
            m_buffer = new byte[m_numBytes]; //创建大型缓冲池
        }

        /// <summary>
        /// 从缓冲池中分配一个缓冲区给指定SocketAsyncEventArgs对象
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            lock(m_freeIndexPool)
            {
                if (m_freeIndexPool.Count > 0)
                {
                    args.SetBuffer(m_buffer, m_freeIndexPool.Pop(), m_bufferSize);
                }
                else
                {
                    if ((m_numBytes - m_bufferSize) < m_currentIndex)
                        return false;
                    args.SetBuffer(m_buffer, m_currentIndex, m_bufferSize);
                    m_currentIndex += m_bufferSize;
                }
                return true;
            }
        }

        /// <summary>
        /// 将缓冲区释放到缓冲池中
        /// </summary>
        /// <param name="args"></param>
        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            lock (m_freeIndexPool)
            {
                m_freeIndexPool.Push(args.Offset);
                args.SetBuffer(null, 0, 0);
            }
        }

    }
}
