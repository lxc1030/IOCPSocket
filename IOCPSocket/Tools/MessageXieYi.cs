using System;
using System.Collections.Generic;
using System.IO;
/// <summary>
/// 【消息协议】=【协议一级标志】+【协议二级标志】+【实际消息长度】+【实际消息内容】+【多于消息内容】
/// http://www.cnblogs.com/sungong1987/p/5267011.html
/// </summary>
public class MessageXieYi
{
    #region 自定义
    #region 协议一级标志，值 = (0 至 254 )
    private byte xieYiFirstFlag;
    /// <summary>
    /// 协议类别，值 = ( 0 直 254 )
    /// </summary>
    public byte XieYiFirstFlag
    {
        get { return xieYiFirstFlag; }
        set { xieYiFirstFlag = value; }
    }
    #endregion

    #region 协议二级标志，值 = (0 至 254 )
    private byte xieYiSecondFlag;
    /// <summary>
    /// 协议二级标志，值 = (0 至 254 )
    /// </summary>
    public byte XieYiSecondFlag
    {
        get { return xieYiSecondFlag; }
        set { xieYiSecondFlag = value; }
    }
    #endregion

    #region 协议字节长度
    //private int xieyilength = 6;
    public const int XieYiLength = 6;
    #endregion

    #region 协议开始结束标识

    public const byte markStart = 60;//<;
    public const byte markEnd = 62;//>;

    #endregion

    #region 实际消息长度
    private int messageContentLength;
    /// <summary>
    /// 实际消息长度
    /// </summary>
    public int MessageContentLength
    {
        get { return messageContentLength; }
        set { messageContentLength = value; }
    }
    #endregion

    #region 实际消息内容
    private byte[] messageContent = new byte[] { };
    /// <summary>
    /// 实际消息内容
    /// </summary>
    public byte[] MessageContent
    {
        get { return messageContent; }
        set { messageContent = value; }
    }
    #endregion


    #endregion

    #region 构造函数两个
    public MessageXieYi()
    {
        //
    }

    public MessageXieYi(byte _xieYiFirstFlage, byte _xieYiSecondFlage, byte[] _messageContent)
    {
        xieYiFirstFlag = _xieYiFirstFlage;
        xieYiSecondFlag = _xieYiSecondFlage;
        messageContentLength = _messageContent.Length;
        messageContent = _messageContent;
    }
    #endregion

    #region MessageXieYi 转换为 byte[]
    /// <summary>
    /// MessageXieYi 转换为 byte[]
    /// </summary>
    /// <returns></returns>
    public byte[] ToBytes()
    {
        byte[] _bytes; //自定义字节数组，用以装载消息协议

        using (MemoryStream memoryStream = new MemoryStream()) //创建内存流
        {
            BinaryWriter binaryWriter = new BinaryWriter(memoryStream); //以二进制写入器往这个流里写内容

            //if (isMark)
            {
                binaryWriter.Write(markStart);
            }

            binaryWriter.Write(xieYiFirstFlag); //写入协议一级标志，占1个字节
            binaryWriter.Write(xieYiSecondFlag); //写入协议二级标志，占1个字节
            binaryWriter.Write(messageContentLength); //写入实际消息长度，占4个字节
            if (messageContentLength > 0)
            {
                binaryWriter.Write(messageContent); //写入实际消息内容
            }

            //if (isMark)
            {
                binaryWriter.Write(markEnd);
            }

            _bytes = memoryStream.ToArray(); //将流内容写入自定义字节数组

            binaryWriter.Close(); //关闭写入器释放资源
        }

        return _bytes; //返回填充好消息协议对象的自定义字节数组
    }

    #endregion



    #region byte[] 转换为 MessageXieYi

    public static MessageXieYi BackMessageType(byte[] buffer)
    {
        if (buffer.Length < MessageXieYi.XieYiLength)//如果长度不足6  就无法转换 return null
        {
            return null;
        }

        MessageXieYi messageXieYi = new MessageXieYi();
        using (MemoryStream memoryStream = new MemoryStream(buffer)) //将字节数组填充至内存流
        {
            BinaryReader binaryReader = new BinaryReader(memoryStream); //以二进制读取器读取该流内容

            messageXieYi.xieYiFirstFlag = binaryReader.ReadByte(); //读取协议一级标志，读1个字节
            messageXieYi.xieYiSecondFlag = binaryReader.ReadByte(); //读取协议二级标志，读1个字节
            messageXieYi.messageContentLength = binaryReader.ReadInt32(); //读取实际消息长度，读4个字节     

            binaryReader.Close();
        }
        return messageXieYi;
    }


    /// <summary>
    /// byte[] 转换为 MessageXieYi
    /// </summary>
    /// <param name="buffer">字节数组缓冲器。</param>
    /// <returns></returns>
    public static MessageXieYi FromBytes(byte[] buffer)
    {
        int bufferLength = buffer.Length;

        int limet = 2;
        if (bufferLength < XieYiLength + limet)
        {
            return null;
        }

        MessageXieYi messageXieYi = new MessageXieYi();
        using (MemoryStream memoryStream = new MemoryStream(buffer)) //将字节数组填充至内存流
        {
            BinaryReader binaryReader = new BinaryReader(memoryStream); //以二进制读取器读取该流内容
            
            byte start = binaryReader.ReadByte();//把开头的标识符去掉
            if (start != markStart)
            {
                Console.WriteLine("开头：" + start);
                return null;
            }

            messageXieYi.xieYiFirstFlag = binaryReader.ReadByte(); //读取协议一级标志，读1个字节
            messageXieYi.xieYiSecondFlag = binaryReader.ReadByte(); //读取协议二级标志，读1个字节
            messageXieYi.messageContentLength = binaryReader.ReadInt32(); //读取实际消息长度，读4个字节                

            //如果【进来的Bytes长度】大于【一个完整的MessageXieYi长度】
            if ((bufferLength - 6) > messageXieYi.messageContentLength)
            {
                messageXieYi.messageContent = binaryReader.ReadBytes(messageXieYi.messageContentLength); //读取实际消息内容，从第7个字节开始读
                //messageXieYi.duoYvBytes = binaryReader.ReadBytes(bufferLength - 6 - messageXieYi.messageContentLength);
            }

            //如果【进来的Bytes长度】等于【一个完整的MessageXieYi长度】
            if ((bufferLength - 6) == messageXieYi.messageContentLength)
            {
                messageXieYi.messageContent = binaryReader.ReadBytes(messageXieYi.messageContentLength); //读取实际消息内容，从第7个字节开始读
            }
            //如果【进来的Bytes长度】小于【一个完整的MessageXieYi长度】
            if ((bufferLength - 6) < messageXieYi.messageContentLength)
            {
                Console.WriteLine("数据接收不齐：" + (bufferLength - 6) + "/" + messageXieYi.messageContentLength);
                return null;
            }

            byte end = binaryReader.ReadByte();
            if (end != markEnd)
            {
                Console.WriteLine("结尾：" + end + "消息长度：" + messageXieYi.messageContentLength);
                return null;
            }

            binaryReader.Close(); //关闭二进制读取器，是否资源
        }
        return messageXieYi; //返回消息协议对象
    }
    #endregion





}

/// <summary>
/// 按照先后顺序合并字节数组类
/// </summary>
public class CombineBytes
{
    /// <summary>
    /// 按照先后顺序合并字节数组，并返回合并后的字节数组。
    /// </summary>
    /// <param name="firstBytes">第一个字节数组</param>
    /// <param name="firstIndex">第一个字节数组的开始截取索引</param>
    /// <param name="firstLength">第一个字节数组的截取长度</param>
    /// <param name="secondBytes">第二个字节数组</param>
    /// <param name="secondIndex">第二个字节数组的开始截取索引</param>
    /// <param name="secondLength">第二个字节数组的截取长度</param>
    /// <returns></returns>
    public static byte[] ToArray(byte[] firstBytes, int firstIndex, int firstLength, byte[] secondBytes, int secondIndex, int secondLength)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write(firstBytes, firstIndex, firstLength);
            bw.Write(secondBytes, secondIndex, secondLength);

            bw.Close();
            bw.Dispose();

            return ms.ToArray();
        }
    }
}