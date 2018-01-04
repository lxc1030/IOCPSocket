using System;
using System.Data.SqlClient;
using System.Text;
using Newtonsoft.Json.Linq;

public class ClassGroup
{
    public static string ErrorSplit = nameof(ErrorType) + ":";
    public static byte[] ErrorBackByType(ErrorType type)
    {
        return SerializeHelper.ConvertToByte(ErrorSplit + (int)type);
    }
    public static ErrorType CheckIsError(MessageXieYi xieyi)
    {
        string message = SerializeHelper.ConvertToString(xieyi.MessageContent);
        string[] sp = message.Split(new string[] { ErrorSplit }, StringSplitOptions.None);
        if (sp.Length > 1)
        {
            return (ErrorType)int.Parse(sp[1]);
        }
        return ErrorType.none;
    }
  




}

/// <summary>
/// Http消息类型
/// </summary>
public enum Comm
{
    error = -1,
    register = 0,
    login,
    createRoom,
    enterRoom,
    outRoom,
    updateUserInfo,
}






#region 以下为客户端也需要的变量类型

//SQL表名称
public class TableName
{
    public static string register = "Register";
    public static string room = "Room";
}

//socket 消息类型
public enum MessageConvention
{
    error = -1,
    login,
    getHeartBeatTime,
    reConnect,//重连
    heartBeat,
    updateName,//更新用户昵称
    createRoom,
    joinRoom,
    updateRoom,
    quitRoom,
    getRoommateInfo,
    updateActorAnimation,
    updateActorState,
    prepareLocalModel,
    updateModelInfo,//发送玩家坐标，旋转和动画当前值
    getPreGameData,
    startGaming,
    gamingTime,
    shootBullet,
    bulletInfo,
    endGaming,
    moveDirection,
    rotateDirection,
    frameData,
}

#endregion