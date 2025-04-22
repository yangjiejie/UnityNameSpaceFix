using UnityEditor;
﻿using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;

public class GenCodeUsingFixer : EditorWindow
{
    private DefaultAsset targetFolder;
    private Vector2 scrollPosition;
    private int processedFiles;
    private bool showDetails;

    // 需要添加的命名空间列表
    private static readonly string[] requiredUsings = 
    {
        "HotFix.Manager.Window.Scene",
        "HotFix.Manager.Window"
    };

    [MenuItem("Tools/GenCode Using Fixer")]
    public static void ShowWindow()
    {
        GetWindow<GenCodeUsingFixer>("GenCode工具");
    }

    void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.HelpBox("1. 选择包含GenCode文件的文件夹\n2. 点击开始处理", MessageType.Info);

        GUILayout.Space(10);
        targetFolder = (DefaultAsset)EditorGUILayout.ObjectField("目标文件夹", targetFolder, typeof(DefaultAsset), false);

        GUILayout.Space(20);
        if (GUILayout.Button("开始处理", GUILayout.Height(40)))
        {
            if (targetFolder != null)
            {
                ProcessFolder();
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "请先选择目标文件夹", "确定");
            }
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField($"已处理文件: {processedFiles}");
        showDetails = EditorGUILayout.ToggleLeft("显示详细日志", showDetails);

        if (showDetails)
        {
            EditorGUILayout.BeginVertical("box");
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            // 此处可添加详细日志显示
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
    }

    void ProcessFolder()
    {
        processedFiles = 0;
        string path = AssetDatabase.GetAssetPath(targetFolder);
        
        if (!Directory.Exists(path))
        {
            Debug.LogError("无效文件夹路径: " + path);
            return;
        }

        string[] allScripts = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
        foreach (string scriptPath in allScripts)
        {
            if (ShouldProcessFile(scriptPath))
            {
                ProcessFile(scriptPath);
                processedFiles++;
            }
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("处理完成", $"已处理 {processedFiles} 个文件", "确定");
    }

    bool ShouldProcessFile(string filePath)
    {
        string fileName = Path.GetFileName(filePath);
        return fileName.StartsWith("GenCode_") && 
               !fileName.EndsWith(".meta") &&
               !filePath.Contains("/Editor/");
    }

    void ProcessFile(string filePath)
    {
        string originalContent = File.ReadAllText(filePath);
        string newContent = AddRequiredUsings(originalContent);

        if (newContent != originalContent)
        {
            File.WriteAllText(filePath, newContent);
            if (showDetails) Debug.Log($"已更新: {filePath}");
        }
    }

    string AddRequiredUsings(string content)
    {
        // 检测是否已包含所有需要的using
        bool allUsingsPresent = true;
        foreach (string ns in requiredUsings)
        {
            if (!HasUsingStatement(content, ns))
            {
                allUsingsPresent = false;
                break;
            }
        }

        if (allUsingsPresent) return content;

        // 生成新的using块（保留原有格式）
        StringBuilder sb = new StringBuilder();
        foreach (string ns in requiredUsings)
        {
            sb.AppendLine($"using {ns};");
        }

        // 插入位置处理
        if (HasExistingUsings(content))
        {
            // 插入到现有using语句之后
            return Regex.Replace(content, 
                @"(^\s*using\s+[\w\.]+\s*;\r?\n)+",
                m => m.Value + sb.ToString(),
                RegexOptions.Multiline);
        }
        else
        {
            // 插入到文件开头
            return sb.ToString() + "\n" + content;
        }
    }

    bool HasUsingStatement(string content, string namespaceName)
    {
        return Regex.IsMatch(content, 
            $@"^\s*using\s+{Regex.Escape(namespaceName)}\s*;",
            RegexOptions.Multiline);
    }

    bool HasExistingUsings(string content)
    {
        return Regex.IsMatch(content, 
            @"^\s*using\s+[\w\.]+\s*;",
            RegexOptions.Multiline);
    }
}