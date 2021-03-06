using System;
using System.Collections;
using System.IO;

public abstract class DownLoadItem
{
    /// <summary>
    /// 网络资源URL路径
    /// </summary>
    protected string url;
    public string Url
    {
        get { return url; }
    }
    /// <summary>
    /// 资源下载存放路径，不包含文件名
    /// </summary>
    protected string savePath;
    public string SavePath
    {
        get { return savePath; }
    }
    /// <summary>
    /// 文件名，不包含后缀
    /// </summary>
    protected string fileNameWithoutExt;
    public string FileNameWithoutExt
    {
        get { return fileNameWithoutExt; }
    }
    /// <summary>
    /// 文件后缀
    /// </summary>
    protected string fileExt;
    public string FileExt
    {
        get { return fileExt; }
    }
    /// <summary>
    /// 文件名，包含后缀
    /// </summary>
    protected string fileName;
    public string FileName
    {
        get { return fileName; }
    }
    /// <summary>
    /// 下载文件全路径，路径+文件名+后缀
    /// </summary>
    protected string saveFilePath;
    public string SaveFilePath
    {
        get { return saveFilePath; }
    }
    /// <summary>
    /// 原文件大小
    /// </summary>
    protected long fileLength;
    public long FileLength
    {
        get { return fileLength; }
    }
    /// <summary>
    /// 当前下载的大小
    /// </summary>
    protected long curLength;
    public long CurLength
    {
        get { return curLength; }
    }
    /// <summary>
    /// 是否开始下载
    /// </summary>
    protected bool startDownLoad;
    public bool StartDownLoad
    {
        get { return startDownLoad; }
    }

    public DownLoadItem(string url, string path)
    {
        this.url = url;
        savePath = path;
        startDownLoad = false;
        fileNameWithoutExt = Path.GetFileNameWithoutExtension(url);
        fileExt = Path.GetExtension(url);
        fileName = string.Format("{0}{1}", fileNameWithoutExt, fileExt);
        saveFilePath = string.Format("{0}/{1}{2}", savePath, fileNameWithoutExt, fileExt);
    }

    public virtual IEnumerator Download(Action callback = null)
    {
        yield return null;
    }

    /// <summary>
    /// 获取下载进度
    /// </summary>
    /// <returns></returns>
    public abstract float GetProcess();

    /// <summary>
    /// 获取当前下载的文件大小
    /// </summary>
    /// <returns></returns>
    public abstract long GetCurLength();

    /// <summary>
    /// 获取下载的文件大小
    /// </summary>
    /// <returns></returns>
    public abstract long GetLength();

    public abstract void Destroy();
}

