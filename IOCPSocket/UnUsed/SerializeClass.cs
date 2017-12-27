using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO.Packaging;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization;
using UnityEngine;



//public static string VectorToString(Vector2 Pos)
//{
//    string info =
//       (int)(Pos.x * 100) + "," +
//       (int)(Pos.y * 100) + "," +
//       "";
//    return info;
//}



[ProtoBuf.ProtoContract]
[Serializable]
public class Register
{
    [ProtoBuf.ProtoMember(1)]
    public string userID;
    [ProtoBuf.ProtoMember(2)]
    public string password;
    [ProtoBuf.ProtoMember(3)]
    public int iconID;
    [ProtoBuf.ProtoMember(4)]
    public int coin;
    [ProtoBuf.ProtoMember(5)]
    public string name;
    [ProtoBuf.ProtoMember(6)]
    public int winLast;
    [ProtoBuf.ProtoMember(7)]
    public int fightCount;
    [ProtoBuf.ProtoMember(8)]
    public int winCount;


    public void Init(SqlDataReader reader)
    {
        while (reader.Read())
        {
            userID = (string)reader[nameof(userID)];
            password = (string)reader[nameof(password)];
            iconID = (int)reader[nameof(iconID)];
            coin = (int)reader[nameof(coin)];
            name = (string)reader[nameof(name)];
            winLast = (int)reader[nameof(winLast)];
            fightCount = (int)reader[nameof(fightCount)];
            winCount = (int)reader[nameof(winCount)];
        }
    }

}

[ProtoBuf.ProtoContract]
public class HeartbeatTime
{
    [ProtoBuf.ProtoMember(1)]
    public int time;
}

[ProtoBuf.ProtoContract]
public class QuitInfo
{
    [ProtoBuf.ProtoMember(1)]
    public bool isQuit;
}

[ProtoBuf.ProtoContract]
public class FrameInfo
{
    [ProtoBuf.ProtoMember(1)]
    public int frameIndex;
    [ProtoBuf.ProtoMember(2)]
    public List<byte[]> frameData;
}

[ProtoBuf.ProtoContract]
public class NetVector3
{
    [ProtoBuf.ProtoMember(1)]
    public float x;
    [ProtoBuf.ProtoMember(2)]
    public float y;
    [ProtoBuf.ProtoMember(3)]
    public float z;
    public NetVector3(float _x, float _y, float _z)
    {
        x = _x;
        y = _y;
        z = _z;
    }

    public static explicit operator NetVector3(Vector3 v)
    {
        return new NetVector3(v.x, v.y, v.z);
    }
}


[ProtoBuf.ProtoContract]
public class ActorMoveDirection
{
    [ProtoBuf.ProtoMember(1)]
    public int userIndex;
    [ProtoBuf.ProtoMember(2)]
    public NetVector3 direction;
    [ProtoBuf.ProtoMember(3)]
    public float speed;
    public ActorMoveDirection()
    {
        direction = new NetVector3(0, 0, 0);
    }
}

[ProtoBuf.ProtoContract]
public class ActorRotateDirection
{
    [ProtoBuf.ProtoMember(1)]
    public int userIndex;
    [ProtoBuf.ProtoMember(2)]
    public int rotateY;
}

[ProtoBuf.ProtoContract]
public class GameModelData
{
    [ProtoBuf.ProtoMember(1)]
    public int userIndex;
    [ProtoBuf.ProtoMember(2)]
    public int frameIndex;
    [ProtoBuf.ProtoMember(3)]
    public NetVector3 pos;
    [ProtoBuf.ProtoMember(4)]
    public NetVector3 rotate;
    [ProtoBuf.ProtoMember(5)]
    public int animation;
    public GameModelData()
    {
        pos = new NetVector3(0, 0, 0);
        rotate = new NetVector3(0, 0, 0);
    }
}



[ProtoBuf.ProtoContract]
public class BulletInfo
{
    [ProtoBuf.ProtoMember(1)]
    public int userIndex;
    [ProtoBuf.ProtoMember(2)]
    public ShootTag shootTag;
    [ProtoBuf.ProtoMember(3)]
    public string shootInfo;

}



//[Serializable]
//public class ActorRotateDirection
//{
//    public int userIndex;
//    public Vector3 Rotation;

//    //*100就是保留2位小数
//    public string GetSendInfo()
//    {
//        string info =
//           userIndex + "," +
//           //
//           //(int)(Rotation.x) + "," +
//           (int)(Rotation.y) + "," +
//           //(int)(Rotation.z) + "," +
//           "";
//        return info;
//    }

//    public void SetSendInfo(string info)
//    {
//        Rotation = Vector3.zero;
//        string[] cs = info.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
//        try
//        {
//            userIndex = int.Parse(cs[0]);
//            //
//            //Rotation.x = float.Parse(cs[1]);
//            //Rotation.y = float.Parse(cs[2]);
//            //Rotation.z = float.Parse(cs[3]);
//            Rotation.y = float.Parse(cs[1]);
//        }
//        catch
//        {
//            Debug.LogError("错误,请检查" + nameof(ActorRotateDirection) + "解析长度。");//nameof別忘记修改
//        }
//    }
//}





[Serializable]
public class ActorNetAnimation
{
    public int userIndex;
    public int animationIndex;
    public string GetSendInfo()
    {
        string info =
          userIndex + "," +
          animationIndex + "," +
          "";
        return info;
    }
    public void SetSendInfo(string info)
    {
        string[] cs = info.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        try
        {
            userIndex = int.Parse(cs[0]);
            animationIndex = int.Parse(cs[1]);
            //
        }
        catch
        {
            Console.WriteLine("错误,请检查" + nameof(ActorNetAnimation) + "解析长度。");//nameof別忘记修改
        }
    }
}

[Serializable]
public class RoomActorUpdate
{
    public int userIndex;
    public string update;

    public string GetSendInfo()
    {
        string info =
          userIndex + "," +
          update + "," +
          "";
        return info;
    }
    public void SetSendInfo(string info)
    {
        string[] cs = info.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        try
        {
            userIndex = int.Parse(cs[0]);
            update = cs[1];
            //
        }
        catch
        {
            Console.WriteLine("错误,请检查" + nameof(RoomActorUpdate) + "解析长度。");//nameof別忘记修改
        }
    }
}


//[Serializable]
//public class ShootInfo
//{
//    public int userIndex;
//    public Vector3 pos;
//    public Vector3 speed;
//    public string GetSendInfo()
//    {
//        string info =
//           userIndex + "," +
//           //
//           (int)(pos.x * 100) + "," +
//           (int)(pos.y * 100) + "," +
//           (int)(pos.z * 100) + "," +
//           (int)(speed.x * 100) + "," +
//           (int)(speed.y * 100) + "," +
//           (int)(speed.z * 100) + "," +

//           //
//           "";
//        return info;
//    }
//    public void SetSendInfo(string info)
//    {
//        string[] cs = info.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
//        try
//        {
//            userIndex = int.Parse(cs[0]);
//            //
//            pos.x = float.Parse(cs[1]) / 100;
//            pos.y = float.Parse(cs[2]) / 100;
//            pos.z = float.Parse(cs[3]) / 100;
//            speed.x = float.Parse(cs[4]) / 100;
//            speed.y = float.Parse(cs[5]) / 100;
//            speed.z = float.Parse(cs[6]) / 100;
//            //
//        }
//        catch
//        {
//            Debug.LogError("错误,请检查" + nameof(ShootInfo) + "解析长度。");//nameof別忘记修改
//        }
//    }
//}




//[Serializable]
//public sealed class Vector3SerializationSurrogate : ISerializationSurrogate
//{
//    public void GetObjectData(object obj, System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
//    {
//        Vector3 v3 = (Vector3)obj;
//        info.AddValue("x", v3.x);
//        info.AddValue("y", v3.y);
//        info.AddValue("z", v3.z);
//    }

//    public object SetObjectData(object obj, System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context, System.Runtime.Serialization.ISurrogateSelector selector)
//    {
//        Vector3 v3 = (Vector3)obj;
//        v3.x = (float)info.GetValue("x", typeof(float));
//        v3.y = (float)info.GetValue("y", typeof(float));
//        v3.z = (float)info.GetValue("z", typeof(float));

//        return (object)v3;
//    }
//}




/// <summary>
/// 房间中人员状态
/// </summary>
public enum RoomActorState
{
    Offline,
    Online,
    ReConnect,
    NoReady,
    Ready,
    PrepareModel,
    ModelPrepared,
    WaitForStart,
    Gaming,
    Invincible,//无敌
    Dead,
    GameEnd,
}

public enum TeamType
{
    Both,
    Blue,
    Red,
}

public enum ShootTag
{
    Wall,
    Box,
    Character,
}

//错误类型
public enum ErrorType
{
    none = -1,//没有错误--用来判断
    format = 0,//格式错误
    parameter = 1,//参数错误
    userOnline,//用户在线
    passwordWrong,//密码错误
    userIDExist,//账号已存在
    userIDNotExist,//账号不存在
    userIDShort,//账号长度太短
    passwordShort,//密码长度太短
    roomMateFull,//该房间满员了
    roomNotExist,//该房间不存在
    curStateNotSync,//当前游戏状态和服务器不同步
}