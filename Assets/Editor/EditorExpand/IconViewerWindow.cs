using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEditor.SearchService;

public class IconViewerWindow : EditorWindow
{
    private string iconDirectory = "Assets/Icon"; // 图标所在的目录，根据实际情况修改
    private Vector2 scrollPosition;

    [MenuItem("Tools/Icon Viewer")]
    public static void ShowWindow()
    {
        GetWindow<IconViewerWindow>("Icon Viewer");
    }
    public List<string> allAssetPath = new();

    public class CustomFileInfo
    {
        public long intSize;
        public string filePathName;
        public string assetPathName;
    }

    List<CustomFileInfo> fileList = new();

    private void OnGUI()
    {
        if (fileList.Count > 0)
        {
            long totalSize = 0;
            fileList.ForEach((xx) => totalSize += xx.intSize);

            var size = 1.0f * totalSize / (1024 * 1024);

            GUILayout.Label("All Icons : size = " + size + "M", EditorStyles.boldLabel);
        }
        else
        {
            GUILayout.Label("All Icons", EditorStyles.boldLabel);
        }

        if(GUILayout.Button("拷贝到外部"))
        {
            EasyUseEditorFuns.DelFolderAllContens(System.Environment.CurrentDirectory + "/../动态资源汇总");
            foreach (var item in fileList)
            {
                EasyUseEditorFuns.UnitySaveCopyFile(item.filePathName, System.Environment.CurrentDirectory + "/../动态资源汇总/" + System.IO.Path.GetFileName(item.assetPathName), true, false);
            }
        }

        if(allAssetPath.Count == 0)
        {
            allAssetPath = AssetDatabase.FindAssets("t:sprite", new string[] { "Assets/" }).Select((guid)=>AssetDatabase.GUIDToAssetPath(guid)).ToList<string>();

            allAssetPath =  allAssetPath.Where((xx) => Regex.IsMatch(xx, @"/icon/") || Regex.IsMatch(xx, @"/gameCommon/variation/") || Regex.IsMatch(xx, @"/global/props/")).ToList<string>();
        }



        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        if(fileList.Count == 0)
        {
            foreach (string iconFile in allAssetPath)
            {
                var cInfo = new CustomFileInfo();
                cInfo.assetPathName = iconFile;
                cInfo.filePathName = Path.Combine(System.Environment.CurrentDirectory, iconFile);


                FileInfo fileInfo = new FileInfo(cInfo.filePathName);
                cInfo.intSize = fileInfo.Length;
                fileList.Add(cInfo);
            }
            fileList.Sort((a, b) =>
            {
                return b.intSize.CompareTo(a.intSize);
            });
        }
        
        

        foreach (var info in fileList)
        {
            var iconFile = info.assetPathName;
            Texture2D iconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(iconFile);
            
            if (iconTexture != null)
            {
                float fileLength = 0;
                GUILayout.Label(iconFile);
                if (info.intSize > 1024 * 1024)
                {
                    fileLength = 1.0f * info.intSize / (1024 * 1024);
                    GUILayout.Label("size = " + fileLength + "M");
                }
                else
                {
                    fileLength = 1.0f * info.intSize / 1024;
                    GUILayout.Label("size = " + fileLength + "KB");
                }
                GUILayout.BeginHorizontal();
                GUILayout.Label(iconTexture, GUILayout.Width(100), GUILayout.Height(100));
                if (GUILayout.Button("ping"))
                {
                 //   EditorUtility.RevealInFinder(info.assetPathName);
                    EditorGUIUtility.PingObject(iconTexture);
                    Selection.activeObject = iconTexture;
                }
                GUILayout.EndHorizontal();
            }
        }

        GUILayout.EndScrollView();
    }
}