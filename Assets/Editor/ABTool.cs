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
    //ƽ̨ѡ�������
    private int nowSelIndex = 0;
    //ƽ̨ѡ��
    private string[] targetString = new String[] { "PC", "IOS", "Android" };
    //��Դ������Ĭ�ϵ�ַ
    private string serIP = "ftp://127.0.0.1";
    //�汾��
    private string version = "1.0.0";
    //AB��·��
    private string abPath = "ArtRes_AB/AB/PC";
    [MenuItem("Tools/AB������")]
    private static void OpenWindow()
    {
        //��ȡһ��ABTools �༭�����ڶ���
        ABTools window = EditorWindow.GetWindowWithRect(typeof(ABTools), new Rect(0, 0, 600, 400)) as ABTools;
        window.Show();
    }

    /// <summary>
    /// �����е�Ԫ��
    /// </summary>
    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 150, 30), "ƽ̨ѡ��");
        //ҳǩ��ʾ
        nowSelIndex = GUI.Toolbar(new Rect(80, 10, 300, 30), nowSelIndex, targetString);

        GUI.Label(new Rect(10, 60, 150, 30), "�汾�ţ�");
        version = GUI.TextField(new Rect(80, 70, 150, 30), version);
        if (GUI.Button(new Rect(250, 70, 200, 30), "�����汾�ļ�"))
        {
            BuildVersionFile();
        }

        GUI.Label(new Rect(10, 120, 150, 30), "AB��·����");
        abPath = GUI.TextField(new Rect(80, 120, 150, 30), abPath);
        if (GUI.Button(new Rect(250, 120, 200, 30), "����AB����Դ�Ա��ļ�"))
        {
            BuildABCompareFile();
        }

        GUI.Label(new Rect(10, 180, 150, 30), "��Դ��������ַ��");
        serIP = GUI.TextField(new Rect(120, 180, 150, 30), serIP);
        if (GUI.Button(new Rect(300, 180, 200, 30), "�ϴ�AB���ļ�����Դ������"))
        {
            UpLoadAllABFiles();
        }


        if (GUI.Button(new Rect(10, 240, 300, 40), "����Ĭ����Դ��StreamingAssets(����գ�������)"))
        {
            MoveABToSA();
        }
    }
    /// <summary>
    /// ����AB�Ա��ļ�
    /// </summary>
    public void BuildABCompareFile()
    {
        //������Ҫ������ab��·��
        DirectoryInfo directory = Directory.CreateDirectory(Application.dataPath + "/" + abPath);
        FileInfo[] fileInfos = directory.GetFiles();
        StringBuilder abcompareInfo = new StringBuilder();
        foreach (var fileinfo in fileInfos)
        {
            if (fileinfo.Extension == "")
            {
                //�� | �ֿ�
                abcompareInfo.Append(fileinfo.Name + " " + fileinfo.Length + " " + GetMD5(fileinfo.FullName) + " | ");
            }
        }
        //��0�ص�length-1����ȥ�����һ���ַ�
        abcompareInfo = abcompareInfo.Remove(abcompareInfo.Length - 2, 1);//��0�ص�length-1����ȥ�����һ���ַ�

        File.WriteAllText(Application.dataPath + "/ArtRes_AB/AB/PC/ABCompareInfo.txt", abcompareInfo.ToString());
        AssetDatabase.Refresh();
        Debug.Log("AB�Ա��ļ����ɳɹ�");
    }

    //����·�����õ�MD5��
    public string GetMD5(string filePath)
    {
        using (FileStream file = new FileStream(filePath, FileMode.Open))
        {
            MD5 md5 = new MD5CryptoServiceProvider();//����MD5����
            byte[] md5Info = md5.ComputeHash(file);//���������룬�õ��ֽ�����
            file.Close();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < md5Info.Length; i++)
            {
                sb.Append(md5Info[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }

    //����version�ļ����˴�ֻ��version�ַ�������Ҫ��������ַ��������Ϣ�����Զ�����ӡ�
    public void BuildVersionFile()
    {
        File.WriteAllText(Application.dataPath + "/ArtRes_AB/AB/" + targetString[nowSelIndex] + "/Version_" + targetString[nowSelIndex] + ".txt", version);
        AssetDatabase.Refresh();
        Debug.Log("�汾�ļ����ɳɹ�");
    }



    /// <summary>
    /// �ƶ�AB����StreamingAssets��
    /// </summary>
    private void MoveABToSA()
    {
        //���StreamingAssetPath
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
            //��ȡѡ����Դ·��
            string path = AssetDatabase.GetAssetPath(asset);
            //��ȡ�ļ���
            string fileName = path.Substring(path.LastIndexOf("/"));//�����ˡ�/��
            //ȥ������AB�ļ�
            if (fileName.IndexOf('.') != -1) continue;

            //�����ļ�
            AssetDatabase.CopyAsset(path, "Assets/StreamingAssets" + fileName);

            //��¼����ABCompareInfo�ļ�
            FileInfo fileInfo = new FileInfo(Application.streamingAssetsPath + fileName);

            abCompareInfo.Append(fileInfo.Name + " " + fileInfo.Length + " " + GetMD5(fileInfo.FullName) + " | ");
        }
        //ȥ�����ġ�|��
        abCompareInfo = abCompareInfo.Remove(abCompareInfo.Length - 2, 1);
        File.WriteAllText(Application.streamingAssetsPath + "/ABCompareInfo.txt", abCompareInfo.ToString());
        File.WriteAllText(Application.streamingAssetsPath + "/Version_" + targetString[nowSelIndex] + ".txt", version);
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// �ϴ�AB����Դ�ļ���������
    /// </summary>
    public void UpLoadAllABFiles()
    {
        //������Ҫ������ab��·��
        DirectoryInfo directory = Directory.CreateDirectory(Application.dataPath + "/ArtRes_AB/AB/" + targetString[nowSelIndex]);
        FileInfo[] fileInfos = directory.GetFiles();
        foreach (var fileinfo in fileInfos)
        {
            //�ϴ�AB����AB�Ա��ļ�
            if (fileinfo.Extension == "" || fileinfo.Extension == ".txt")
            {
                FtpUpLoadFile(fileinfo.FullName, fileinfo.Name);
            }
        }
    }

    //�첽����
    //���ʹ��http��������Э�飬�����Լ�����д����������button���´������¼��ͺ�
    public async void FtpUpLoadFile(string filePath, string fileName)
    {
        await Task.Run(() =>
        {
            try
            {
                FtpWebRequest req = FtpWebRequest.Create(new Uri(serIP + "/AB/" + targetString[nowSelIndex] + "/" + fileName)) as FtpWebRequest;
                //ͨ��ƾ֤������
                NetworkCredential n = new NetworkCredential("Tan", "Tan123");
                req.Credentials = n;
                req.Proxy = null;
                req.KeepAlive = false;
                req.UseBinary = true;
                req.Method = WebRequestMethods.Ftp.UploadFile;//��������
                Stream upLoadStream = req.GetRequestStream();//�õ��ϴ���
                //���������ļ�����Ȼ�����ϴ��ļ���д���ֽ�
                using (FileStream file = File.OpenRead(filePath))
                {
                    byte[] bytes = new byte[2048];
                    //�����ֽڵĳ��ȣ������ж��Ƿ����
                    int contentLength = file.Read(bytes, 0, bytes.Length);
                    //�ϴ�
                    while (contentLength != 0)
                    {
                        upLoadStream.Write(bytes, 0, contentLength);
                        contentLength = file.Read(bytes, 0, bytes.Length);
                    }
                    //���֮�󣬹ر���
                    file.Close();
                    upLoadStream.Close();
                }
                Debug.Log(fileName + "�ϴ��ɹ�");
            }
            catch (Exception e)
            {
                Debug.Log(fileName + "�ϴ�ʧ��,ԭ��" + e.Message);
            }
        });
    }
}