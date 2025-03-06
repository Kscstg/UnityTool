using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class ABTools : EditorWindow
{
    //平台选择的索引
    private int nowSelIndex = 0;
    //平台选择
    private string[] targetString = new String[] { "PC", "IOS", "Android" };
    //资源服务器默认地址
    private string serIP = "ftp://127.0.0.1";
    //版本号
    private string version = "1.0.0";
    //AB包路径
    private string abPath = "ArtRes_AB/AB/PC";
    [MenuItem("Tools/AB包工具")]
    private static void OpenWindow()
    {
        //获取一个ABTools 编辑器窗口对象
        ABTools window = EditorWindow.GetWindowWithRect(typeof(ABTools), new Rect(0, 0, 600, 400)) as ABTools;
        window.Show();
    }

    /// <summary>
    /// 窗口中的元素
    /// </summary>
    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 150, 30), "平台选择：");
        //页签显示
        nowSelIndex = GUI.Toolbar(new Rect(80, 10, 300, 30), nowSelIndex, targetString);

        GUI.Label(new Rect(10, 60, 150, 30), "版本号：");
        version = GUI.TextField(new Rect(80, 70, 150, 30), version);
        if (GUI.Button(new Rect(250, 70, 200, 30), "创建版本文件"))
        {
            BuildVersionFile();
        }

        GUI.Label(new Rect(10, 120, 150, 30), "AB包路径：");
        abPath = GUI.TextField(new Rect(80, 120, 150, 30), abPath);
        if (GUI.Button(new Rect(250, 120, 200, 30), "创建AB包资源对比文件"))
        {
            BuildABCompareFile();
        }

        GUI.Label(new Rect(10, 180, 150, 30), "资源服务器地址：");
        serIP = GUI.TextField(new Rect(120, 180, 150, 30), serIP);
        if (GUI.Button(new Rect(300, 180, 200, 30), "上传AB包文件到资源服务器"))
        {
            UpLoadAllABFiles();
        }


        if (GUI.Button(new Rect(10, 240, 300, 40), "保存默认资源到StreamingAssets(先清空，再生成)"))
        {
            MoveABToSA();
        }
    }
    /// <summary>
    /// 创建AB对比文件
    /// </summary>
    public void BuildABCompareFile()
    {
        //声明需要遍历的ab包路径
        DirectoryInfo directory = Directory.CreateDirectory(Application.dataPath + "/" + abPath);
        FileInfo[] fileInfos = directory.GetFiles();
        StringBuilder abcompareInfo = new StringBuilder();
        foreach (var fileinfo in fileInfos)
        {
            if (fileinfo.Extension == "")
            {
                //用 | 分开
                abcompareInfo.Append(fileinfo.Name + " " + fileinfo.Length + " " + GetMD5(fileinfo.FullName) + " | ");
            }
        }
        //从0截到length-1，即去除最后一个字符
        abcompareInfo = abcompareInfo.Remove(abcompareInfo.Length - 2, 1);//从0截到length-1，即去除最后一个字符

        File.WriteAllText(Application.dataPath + "/ArtRes_AB/AB/PC/ABCompareInfo.txt", abcompareInfo.ToString());
        AssetDatabase.Refresh();
        Debug.Log("AB对比文件生成成功");
    }

    //输入路径，得到MD5码
    public string GetMD5(string filePath)
    {
        using (FileStream file = new FileStream(filePath, FileMode.Open))
        {
            MD5 md5 = new MD5CryptoServiceProvider();//创建MD5对象
            byte[] md5Info = md5.ComputeHash(file);//把流对象传入，得到字节数组
            file.Close();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < md5Info.Length; i++)
            {
                sb.Append(md5Info[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }

    //生成version文件，此处只有version字符串，需要服务器地址等其他信息可以自定义添加。
    public void BuildVersionFile()
    {
        File.WriteAllText(Application.dataPath + "/ArtRes_AB/AB/" + targetString[nowSelIndex] + "/Version_" + targetString[nowSelIndex] + ".txt", version);
        AssetDatabase.Refresh();
        Debug.Log("版本文件生成成功");
    }



    /// <summary>
    /// 移动AB包到StreamingAssets中
    /// </summary>
    private void MoveABToSA()
    {
        //清空StreamingAssetPath
        DirectoryInfo directory = new DirectoryInfo(Application.streamingAssetsPath);
        FileInfo[] fileInfos = directory.GetFiles();
        if (fileInfos.Length != 0)
        {
            for (int i = 0; i < fileInfos.Length; i++)
            {
                fileInfos[i].Delete();
            }
        }
        UnityEngine.Object[] selectAssets = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets);
        if (selectAssets.Length == 0) return;
        StringBuilder abCompareInfo = new StringBuilder();
        foreach (var asset in selectAssets)
        {
            //获取选中资源路径
            string path = AssetDatabase.GetAssetPath(asset);
            //获取文件名
            string fileName = path.Substring(path.LastIndexOf("/"));//包含了”/“
            //去掉不是AB文件
            if (fileName.IndexOf('.') != -1) continue;

            //复制文件
            AssetDatabase.CopyAsset(path, "Assets/StreamingAssets" + fileName);

            //记录生成ABCompareInfo文件
            FileInfo fileInfo = new FileInfo(Application.streamingAssetsPath + fileName);

            abCompareInfo.Append(fileInfo.Name + " " + fileInfo.Length + " " + GetMD5(fileInfo.FullName) + " | ");
        }
        //去掉最后的”|“
        abCompareInfo = abCompareInfo.Remove(abCompareInfo.Length - 2, 1);
        File.WriteAllText(Application.streamingAssetsPath + "/ABCompareInfo.txt", abCompareInfo.ToString());
        File.WriteAllText(Application.streamingAssetsPath + "/Version_" + targetString[nowSelIndex] + ".txt", version);
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 上传AB包资源文件到服务器
    /// </summary>
    public void UpLoadAllABFiles()
    {
        //声明需要遍历的ab包路径
        DirectoryInfo directory = Directory.CreateDirectory(Application.dataPath + "/ArtRes_AB/AB/" + targetString[nowSelIndex]);
        FileInfo[] fileInfos = directory.GetFiles();
        foreach (var fileinfo in fileInfos)
        {
            //上传AB包和AB对比文件
            if (fileinfo.Extension == "" || fileinfo.Extension == ".txt")
            {
                FtpUpLoadFile(fileinfo.FullName, fileinfo.Name);
            }
        }
    }

    //异步加载
    //如果使用http等其他的协议，可以自己重新写个方法，在button改下触发的事件就好
    public async void FtpUpLoadFile(string filePath, string fileName)
    {
        await Task.Run(() =>
        {
            try
            {
                FtpWebRequest req = FtpWebRequest.Create(new Uri(serIP + "/AB/" + targetString[nowSelIndex] + "/" + fileName)) as FtpWebRequest;
                //通信凭证和设置
                NetworkCredential n = new NetworkCredential("Tan", "Tan123");
                req.Credentials = n;
                req.Proxy = null;
                req.KeepAlive = false;
                req.UseBinary = true;
                req.Method = WebRequestMethods.Ftp.UploadFile;//操作命令
                Stream upLoadStream = req.GetRequestStream();//得到上传流
                //创建本地文件流，然后往上传文件流写入字节
                using (FileStream file = File.OpenRead(filePath))
                {
                    byte[] bytes = new byte[2048];
                    //读出字节的长度，用于判断是否读完
                    int contentLength = file.Read(bytes, 0, bytes.Length);
                    //上传
                    while (contentLength != 0)
                    {
                        upLoadStream.Write(bytes, 0, contentLength);
                        contentLength = file.Read(bytes, 0, bytes.Length);
                    }
                    //完成之后，关闭流
                    file.Close();
                    upLoadStream.Close();
                }
                Debug.Log(fileName + "上传成功");
            }
            catch (Exception e)
            {
                Debug.Log(fileName + "上传失败,原因：" + e.Message);
            }
        });
    }
}