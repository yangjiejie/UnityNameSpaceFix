using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using UnityEditor;
using UnityEngine;

public class EditorLogWindow : EditorWindow
{
    private static Vector2 logWindowSize = new Vector2(500, 300);
    public static EditorLogWindow instance;

    private Vector2 scrollPosition; // 滚动位置
    private static List<string> textList = new List<string>(); // 文本列表
    private static float totalResLength = 0; 
    public static void   WriteLog(string str)
    {
        if (string.IsNullOrEmpty(str)) return;
        if (textList.Count == 0 || !textList.Contains(str))
        {
            str = str.Replace("\\", "/");
            int index = str.IndexOf("Assets/");
            if(index < 0)
            {
                return;
            }
            var realPath = str.Substring(index >=0 ? index : 0);
            if(!File.Exists(realPath))
            {
                return;
            }
            FileInfo fileInfo = new FileInfo(System.Environment.CurrentDirectory +"/" +realPath);
            float singleSize = fileInfo.Length / (1024.0f );
            totalResLength += singleSize;
            textList.Add(realPath + $"  size :{singleSize}kb");
        }
            
    }

    public static  void ClearLog()
    {
        totalResLength = 0;
        textList.Clear();
    }

    public static EditorLogWindow GetInstance()
    {
        return instance;
    }
    public static void CloseWindow()
    {
        instance?.Close();
        instance = null;
    }
    public static void OpenWindow(EditorWindow mainWindow)
    {
        instance = GetWindow<EditorLogWindow>(true, "Log Window", false);
        
        // 设置日志窗口大小
        instance.minSize = logWindowSize;

        // 计算日志窗口位置
        Rect mainWindowRect = mainWindow.position;
        float logWindowX = mainWindowRect.x + mainWindowRect.width;
        float logWindowY = mainWindowRect.y;

        // 设置日志窗口位置
        instance.position = new Rect(logWindowX, logWindowY, logWindowSize.x, logWindowSize.y);

        
        //for (int i = 0; i < 100; i++)
        //{
        //    instance.textList.Add("测试文本" + i);
        //}

    }

    public void OnGUI()
    {
        if (instance == null) instance = this;
        GUILayout.BeginVertical();
        // scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(this.position.height - 30));

        for (int i = 0; i < textList.Count; i++)
        {
            GUILayout.Label(textList[i], EditorStyles.boldLabel);
        }
        GUILayout.EndScrollView();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("拷贝", GUILayout.Height(30),GUILayout.Width(200)))
        {
            var sb = new StringBuilder();
            for (int i = 0; i < textList.Count; i++)
            {
                sb.AppendLine(textList[i]);
            }
            GUIUtility.systemCopyBuffer = sb.ToString();
        }
        instance.titleContent = new GUIContent( $"log window 共计资源{textList.Count}个" ) ;
        GUILayout.Label($"总资源大小{totalResLength}Kb == " + (totalResLength / 1024.0f) + "M", GUILayout.Height(30), GUILayout.Width(250));
        
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }

}
