using System;
using System.IO;

public class LogManager
{
    public static string logPath = @"..\..\Log\";

    public static void WriteLog(string LogText, bool isCreate = false)
    {
        try
        {
            Console.WriteLine(LogText);
            string strLogFilePath = logPath + DateTime.Now.ToString("yyyy-MM-dd") + ".txt";
            StreamWriter logWriter;
            logWriter = File.AppendText(strLogFilePath);
            logWriter.Write(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " : ");
            logWriter.WriteLine(LogText);
            logWriter.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine("///////////////////////" + LogText);
        }
    }








    public static bool DeleteFiles()
    {
        string strPath = logPath;
        try
        {
            strPath = @strPath.Trim().ToString();// 清除空格
            if (Directory.Exists(strPath))// 判断文件夹是否存在
            {
                string[] strDirs = Directory.GetDirectories(strPath);// 获得文件夹数组
                string[] strFiles = Directory.GetFiles(strPath);// 获得文件数组
                foreach (string strFile in strFiles)// 遍历所有子文件夹
                {
                    System.Diagnostics.Debug.Write(strFile + "-deleted");
                    File.Delete(strFile);// 删除文件夹
                }
                foreach (string strdir in strDirs)// 遍历所有文件
                {
                    System.Diagnostics.Debug.Write(strdir + "-deleted");
                    Directory.Delete(strdir, true);// 删除文件
                }
            }
            return true;// 成功
        }
        catch (Exception Exp) // 异常处理
        {
            System.Diagnostics.Debug.Write(Exp.Message.ToString());// 异常信息
            return false;// 失败
        }
    }

}

