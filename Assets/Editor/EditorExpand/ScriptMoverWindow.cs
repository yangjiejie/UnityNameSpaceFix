using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;



public class BatchScriptMoverWindow : EditorWindow
{
    private List<string> scriptPaths = new List<string>();
    private string targetPath = "";
    private Vector2 scrollPos;

    [MenuItem("Assets/Move Scripts with Meta", true)]
    static bool ValidateMultiMove()
    {
        return Selection.objects.All(obj => obj is MonoScript);
    }

    [MenuItem("Assets/Move Scripts with Meta", false, 32)]
    static void Init()
    {
        var window = GetWindow<BatchScriptMoverWindow>();
        window.scriptPaths = Selection.objects
            .Select(obj => AssetDatabase.GetAssetPath(obj))
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();
        window.titleContent = new GUIContent("Batch Script Mover");
        window.minSize = new Vector2(450, 300);
        window.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Batch Script Moving Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 显示选中文件信息
        EditorGUILayout.LabelField($"Selected Scripts ({scriptPaths.Count}):");
        DrawFileList();

        // 目标路径区域
        DrawPathSelector();

        // 操作按钮
        EditorGUILayout.Space();
        if (GUILayout.Button($"Move {scriptPaths.Count} Scripts", GUILayout.Height(40)))
        {
            MoveAllScripts();
        }
    }

    void DrawFileList()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(100));
        {
            foreach (var path in scriptPaths.Take(5))
            {
                EditorGUILayout.LabelField($"• {Path.GetFileName(path)}", 
                    EditorStyles.miniLabel);
            }
            if (scriptPaths.Count > 5)
            {
                EditorGUILayout.LabelField(
                    $"...and {scriptPaths.Count - 5} more", 
                    EditorStyles.miniLabel);
            }
        }
        EditorGUILayout.EndScrollView();
    }

    void DrawPathSelector()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        {
            EditorGUILayout.LabelField("Destination Folder:");
            EditorGUILayout.BeginHorizontal();
            {
                if (string.IsNullOrEmpty(targetPath))
                {
                    targetPath = GetPrefs(this.name);
                }

                targetPath = EditorGUILayout.TextField(targetPath);
                if (GUILayout.Button("Browse", GUILayout.Width(80)))
                {
                    BrowseFolder();
                }
            }
            EditorGUILayout.EndHorizontal();

            // 拖拽处理
            HandleDragAndDrop(GUILayoutUtility.GetLastRect());
        }
        EditorGUILayout.EndVertical();
    }

    internal string GetBaseKey(string key)
    {
        List<string> keyList = new();
        keyList.Add(Application.productName);
        keyList.Add(Application.identifier);
        keyList.Add(key);
        string totalKey = string.Join("-", keyList);
        return totalKey;
    }

    void SavePrefs(string key, string value)
    {
        var finalKey = GetBaseKey(key);
        EditorPrefs.SetString(finalKey, value);
    }

    string GetPrefs(string key)
    {
        var finalKey = GetBaseKey(key);
        var rst = EditorPrefs.GetString(finalKey, "");
        return rst;
    }

    void BrowseFolder()
    {
        if(string.IsNullOrEmpty(targetPath))
        {
            targetPath = "Assets/hot_fix";
        }
        string path = EditorUtility.OpenFolderPanel("Select Folder",
           targetPath, "");
        if (!string.IsNullOrEmpty(path))
        {
            targetPath = ConvertToAssetPath(path);
            SavePrefs(this.name, targetPath);
        }
    }

    void HandleDragAndDrop(Rect dropArea)
    {
        Event evt = Event.current;

        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Link;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (var obj in DragAndDrop.objectReferences
                        .Where(IsValidFolder))
                    {
                        targetPath = AssetDatabase.GetAssetPath(obj);
                        break;
                    }
                }
                evt.Use();
                break;
        }
    }

    bool IsValidFolder(Object obj)
    {
        string path = AssetDatabase.GetAssetPath(obj);
        return AssetDatabase.IsValidFolder(path);
    }

    string ConvertToAssetPath(string systemPath)
    {
        if (systemPath.StartsWith(Application.dataPath))
        {
            return "Assets" + systemPath.Substring(Application.dataPath.Length);
        }
        return systemPath;
    }

    void MoveAllScripts()
    {
        try
        {
            if (!AssetDatabase.IsValidFolder(targetPath))
            {
                EditorUtility.DisplayDialog("Error", 
                    "Invalid destination folder!", "OK");
                return;
            }

            List<string> movedFiles = new List<string>();
            List<string> errors = new List<string>();

            // 批量移动
            for (int i = 0; i < scriptPaths.Count; i++)
            {
                string oldPath = scriptPaths[i];
                string fileName = Path.GetFileName(oldPath);
                string newPath = Path.Combine(targetPath, fileName).Replace("\\", "/");

                // 跳过相同路径
                if (oldPath == newPath) continue;

                // 显示进度条
                EditorUtility.DisplayProgressBar("Moving Scripts", 
                    $"{fileName}...", (float)i / scriptPaths.Count);

                // 执行移动
                string error = AssetDatabase.MoveAsset(oldPath, newPath);
                if (!string.IsNullOrEmpty(error))
                {
                    errors.Add($"{fileName}: {error}");
                }
                else
                {
                    movedFiles.Add(newPath);
                }
            }

            // 批量移动之后需要对命名空间进行处理 
            for (int i = 0; i < scriptPaths.Count; i++)
            {
                string oldPath = scriptPaths[i];
                string fileName = Path.GetFileName(oldPath);
                string newPath = Path.Combine(targetPath, fileName).Replace("\\", "/");

                if(File.Exists(newPath))
                {
                    //修改命名空间 
                }
            }

            EditorUtility.ClearProgressBar();

            // 刷新资源
            if (movedFiles.Count > 0)
            {
                AssetDatabase.ForceReserializeAssets(movedFiles);
                AssetDatabase.Refresh();
            }

            // 显示结果报告
            ShowResultReport(movedFiles.Count, errors);
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Critical Error", 
                $"Operation aborted: {e.Message}", "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    void MatchNameSpace(string csFilePath)
    {
        var content = File.ReadAllText(csFilePath);


    }

    void ShowResultReport(int successCount, List<string> errors)
    {
        if (errors.Count == 0)
        {
            EditorUtility.DisplayDialog("Completed", 
                $"Successfully moved {successCount} scripts", "OK");
            Close();
            return;
        }

        string message = $"Success: {successCount}\n" +
                        $"Failures: {errors.Count}\n\n" +
                        string.Join("\n", errors.Take(3));

        if (errors.Count > 3)
        {
            message += $"\n...and {errors.Count - 3} more errors";
        }

        EditorUtility.DisplayDialog("Partial Success", message, "OK");
    }

   
}