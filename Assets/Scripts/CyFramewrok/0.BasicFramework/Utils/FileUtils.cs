using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FileUtils
{
    public static void DeleteFilesInDir(string path)
    {
        try
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            FileSystemInfo[] fileInfo = dir.GetFileSystemInfos();
            foreach (FileSystemInfo info in fileInfo)
            {
                if (info is DirectoryInfo)
                {
                    DirectoryInfo subdir = new DirectoryInfo(info.FullName);
                    subdir.Delete(true);
                }
                else
                {
                    File.Delete(info.FullName);
                }
            }
        }
        catch(Exception e)
        {
            Debug.LogError(e);
        }
    }

    public static void CreateFile(string filePath, byte[] bytes)
    {
        if (File.Exists(filePath))
            File.Delete(filePath);
        FileInfo fileInfo = new FileInfo(filePath);
        using (Stream stream = fileInfo.Create())
        {
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
