using System;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using UnityEngine;

public class GameUnZipInfo
{
    public string FileName;
    public long Size;
}
    
/// <summary>
/// Description: zip压缩/解压工具类：提供Json等文件压缩/解压的相关功能
/// </summary>
public static class ZipUtils 
{
    /// <summary>
    /// 反序列化zip数据
    /// </summary>
    /// <param name="ZipByte"></param>
    /// <returns></returns>
    public static Dictionary<string, byte[]> UnpackFiles(byte[] ZipByte)
    {
        Dictionary<string,byte[]> allFiles = new Dictionary<string, byte[]>();
        //bool result = true;
        if(ZipByte == null || ZipByte.Length == 0)
        {
            return allFiles;
        }
        ZipInputStream zipStream = null;
        ZipEntry ent = null;
        string fileName = "";
        string fullPath = "";
        try
        {
            //直接使用 将byte转换为Stream，省去先保存到本地在解压的过程
            Stream stream = new MemoryStream(ZipByte);
            zipStream = new ZipInputStream(stream);
            LoggerUtils.Log("zipStream = " + zipStream);
            while ((ent = zipStream.GetNextEntry()) != null)
            {
                if (!string.IsNullOrEmpty(ent.Name))
                {
                    using (MemoryStream fs = new MemoryStream())
                    {
                        int size = 2048;
                        byte[] data = new byte[size];
                        while (true)
                        {
                            size = zipStream.Read(data, 0, data.Length);
                            if (size > 0)
                            {
                                fs.Write(data, 0, size); //解决读取不完整情况 
                            }
                            else
                            {
                                break;
                            }
                        }
                        var bytes = fs.GetBuffer();
                        allFiles.Add(ent.Name, bytes);
                    }
                }
            }
            if (allFiles.ContainsKey("fileList.json"))
            {
                allFiles.Remove("fileList.json");
            }
            return allFiles;
        }
        catch (Exception e)
        {
            LoggerUtils.Log(e.ToString());
            //result = false;
            return null;
        }
    }

    public static List<GameUnZipInfo> UnpackFiles(byte[] ZipByte, string desFolder)
    {
        List<GameUnZipInfo> unZipInfos = new List<GameUnZipInfo>();
        if(ZipByte == null || ZipByte.Length == 0)
        {
            return unZipInfos;
        }
        
        ZipInputStream zipStream = null;
        ZipEntry entry = null;
        try
        {
            if (!Directory.Exists(desFolder))
                Directory.CreateDirectory(desFolder);

            Stream stream = new MemoryStream(ZipByte);
            zipStream = new ZipInputStream(stream);

            // 解压
            while ((entry = zipStream.GetNextEntry()) != null)
            {
                if (!string.IsNullOrEmpty(entry.Name))
                {
                    var filePathName = Path.Combine(desFolder, entry.Name);
                    // 写入文件
                    using (FileStream fs = File.Create(filePathName))
                    {
                        byte[] data = new byte[2048];

                        while (true)
                        {
                            int count = zipStream.Read(data, 0, data.Length);
                            if (count > 0) {
                                fs.Write(data, 0, count);
                            }
                            else {
                                break;
                            }
                        }
                    }

                    unZipInfos.Add(new GameUnZipInfo()
                    {
                        FileName = entry.Name,
                        Size = entry.Size,
                    });
                }
            }

            LoggerUtils.Log($"UnpackFiles Success Path: {desFolder}" );
            return unZipInfos;
        }
        catch (Exception e)
        {
            LoggerUtils.LogError($"UnpackFiles Error {e.ToString()}" );
            return unZipInfos;
        }
    }
    
    /// <summary>
    /// 解压目录文件
    /// </summary>
    /// <param name="sourceZipPath">原压缩文件</param>
    /// <param name="desPath">目标路径</param>
    public static void UnZipDirectory(string sourceZipPath, string desPath)
    {
        FastZip fast = new FastZip();
        try
        {
            fast.ExtractZip(sourceZipPath, desPath, null);
        }
        catch (Exception ex)
        {
            LoggerUtils.LogError("UnZipDirectory Failed! -- " + ex.Message);
        }
    }
}
