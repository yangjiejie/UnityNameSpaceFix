using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;

using UnityEditor.Experimental.GraphView;
using Unity.Plastic.Newtonsoft.Json;

public class FolderTreeEditor  
{
    private string treeSavePath { 
        get 
        {
            return Path.Combine(Application.dataPath, "Editor/EditorExpand/~config/folderNodeMap.json").ToLinuxPath();
        } 
    }
    private string customConfigPath;
    
    public class FolderNode
    {
        public string Name;

        public string fullPath;
        public string FullPath 
        { 
            get=> fullPath;
            set
            {
                fullPath = value;
                NowUnityPath = fullPath.ToUnityPath(false);
            }
        }
        public string originUnityPath;
        private bool initedOriginUnityPath;
        public string OriginUnityPath 
        {
            get=>originUnityPath;
            set
            {
                if (initedOriginUnityPath) return;
                initedOriginUnityPath = true;
                originUnityPath = value; 
            }
        }

        public string NowUnityPath;
        public List<FolderNode> Children { get; } = new List<FolderNode>();
        public bool IsExpanded;

        [JsonIgnore]
        public string RenameValue { get; set; }

        public FolderNode(string name, string fullPath)
        {
            Name = name;
            FullPath = fullPath;
            OriginUnityPath = fullPath.ToUnityPath(false);
        }
    }
    public FolderNode rootNode { get; set; }
    public string rootPath { get; set; } = "Assets";
    private Dictionary<string, bool> expandedStates = new Dictionary<string, bool>();
    private Dictionary<string, string> renameValues = new Dictionary<string, string>();
    private Vector2 scrollPosition;

    // 批量重名相关
    private bool showBatchRename = false;
    private string batchFindPattern = "";
    private string batchReplacePattern = "";
    private bool isInited;
    public void BuildDirectoryTree()
    {
        if(isInited)
        {
            return;
        }
        rootNode = new FolderNode(Path.GetFileName(rootPath), rootPath);
        BuildDirectoryTreeRecursive(rootNode);
        isInited = true;
    }

    private void BuildDirectoryTreeRecursive(FolderNode parentNode)
    {
        try
        {
            foreach (var dir in Directory.GetDirectories(parentNode.FullPath))
            {
                var dirName = Path.GetFileName(dir);
                var childNode = new FolderNode(dirName, dir);

                // 恢复展开状态
                if (expandedStates.ContainsKey(dir))
                {
                    childNode.IsExpanded = expandedStates[dir];
                }

                parentNode.Children.Add(childNode);
                BuildDirectoryTreeRecursive(childNode);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Access denied to directory: {parentNode.FullPath}");
        }
    }

    public void DrawUI()
    {
        BuildDirectoryTree();
        EditorGUILayout.BeginHorizontal();
        rootPath = EditorGUILayout.TextField("Root Path:", rootPath);
        if (GUILayout.Button("刷新文件夹", GUILayout.Width(80)))
        {
            RefreshTree();
        }
        if(GUILayout.Button("加载文件夹配置..."))
        {
            ReadJson();
            
        }
        if (GUILayout.Button("保存文件夹配置..."))
        {
            SaveJson();
            
        }
        EditorGUILayout.EndHorizontal();

        // 批量重名按钮
        if (GUILayout.Button("Batch Rename", GUILayout.Width(100)))
        {
            showBatchRename = !showBatchRename;
        }

        // 批量重命名面板
        if (showBatchRename)
        {
            DrawBatchRenamePanel(); 
        }

        // 树形视图
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        if (rootNode != null)
        {
            DrawFolderTree(rootNode);
        }
        EditorGUILayout.EndScrollView();
    }
    public Action<string> logAction { get; set; }
    public void Log(string str)
    {
        if(logAction != null)
        {
            logAction(str);
        }
    }
   /// <summary>
   ///  更新所有节点 
   /// </summary>
   /// <param name="node"></param>
    public void  UpdateAllNode(FolderNode node)
    {
        var findNode = FindNodeByPath(node.OriginUnityPath);
        if(findNode != null)
        {
            findNode.Name = node.Name;
            findNode.FullPath = node.FullPath;
        }
        foreach (var item in node.Children)
        {
            UpdateAllNode(item);
        }
    }
    public FolderNode FindNodeByPath(string path,FolderNode currentNode = null)
    {
        if (currentNode == null)
            currentNode = this.rootNode;
        if (currentNode.OriginUnityPath == path)
            return currentNode;

        foreach (var child in currentNode.Children)
        {
            var found = FindNodeByPath(path,child);
            if (found != null)
                return found;
        }

        return null;
    }

    private void DrawBatchRenamePanel()
    {
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField("Batch Rename Settings", EditorStyles.boldLabel);

        batchFindPattern = EditorGUILayout.TextField("Find Pattern:", batchFindPattern);
        batchReplacePattern = EditorGUILayout.TextField("Replace With:", batchReplacePattern);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Preview"))
        {
            PreviewBatchRename();
        }
        if (GUILayout.Button("Apply"))
        {
            ApplyBatchRename();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawFolderTree(FolderNode node)
    {
        EditorGUILayout.BeginHorizontal();

        // 展开/折叠按钮
        if (node.Children.Count > 0)
        {
            if (GUILayout.Button(node.IsExpanded ? "▼" : "▶", GUILayout.Width(20)))
            {
                node.IsExpanded = !node.IsExpanded;
                expandedStates[node.FullPath] = node.IsExpanded;
            }
        }
        else
        {
            GUILayout.Space(24);
        }

        // 文件夹名称显示和重命名
        EditorGUILayout.BeginHorizontal();

        if (!string.IsNullOrEmpty(node.RenameValue))
        {
            node.RenameValue = EditorGUILayout.TextField(node.RenameValue);
            if (GUILayout.Button("✓", GUILayout.Width(20)))
            {
                RenameFolder(node.FullPath, node.RenameValue);
                node.Name = node.RenameValue;
                node.FullPath = Path.Combine(Path.GetDirectoryName(node.FullPath), node.RenameValue);
                node.RenameValue = null;
            }
            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                node.RenameValue = null;
            }
        }
        else
        {
            EditorGUILayout.LabelField(node.Name);
            if (GUILayout.Button("Rename", GUILayout.Width(60)))
            {
                node.RenameValue = node.Name;
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndHorizontal();

        // 子文件夹
        if (node.IsExpanded && node.Children.Count > 0)
        {
            EditorGUI.indentLevel++;
            foreach (var child in node.Children)
            {
                DrawFolderTree(child);
            }
            EditorGUI.indentLevel--;
        }
    }

    private void RenameFolder(string oldPath, string newName)
    {
        try
        {
            string parentPath = Path.GetDirectoryName(oldPath);
            string newPath = Path.Combine(parentPath, newName);

            // 检查新名称是否有效
            if (string.IsNullOrWhiteSpace(newName))
            {
                EditorUtility.DisplayDialog("Error", "Folder name cannot be empty!", "OK");
                return;
            }
           

            // 更新展开状态字典
            if (expandedStates.ContainsKey(oldPath))
            {
                expandedStates[newPath] = expandedStates[oldPath];
                expandedStates.Remove(oldPath);
            }

          

            
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to rename folder: {e.Message}");
            EditorUtility.DisplayDialog("Error", $"Failed to rename folder: {e.Message}", "OK");
        }
    }

    private void PreviewBatchRename()
    {
        if (string.IsNullOrEmpty(batchFindPattern))
        {
            EditorUtility.DisplayDialog("Error", "Find pattern cannot be empty!", "OK");
            return;
        }

        var allFolders = Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories);
        foreach (var folder in allFolders)
        {
            var dirInfo = new DirectoryInfo(folder);
            string newName = Regex.Replace(dirInfo.Name, batchFindPattern, batchReplacePattern);

            if (newName != dirInfo.Name)
            {
                renameValues[folder] = newName;
            }
        }
    }

    private void ApplyBatchRename()
    {
        if (string.IsNullOrEmpty(batchFindPattern))
        {
            EditorUtility.DisplayDialog("Error", "Find pattern cannot be empty!", "OK");
            return;
        }

        var foldersToRename = new List<string>(renameValues.Keys);
        foreach (var folder in foldersToRename)
        {
            RenameFolder(folder, renameValues[folder]);
        }

        renameValues.Clear();
    }

    public void ReadJson(bool withFolderSelect = true)
    {
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        if (string.IsNullOrEmpty(customConfigPath))
        {
            customConfigPath = Path.Combine(Application.dataPath, "Editor/EditorExpand/~config").ToLinuxPath();


        }
        if(withFolderSelect)
        {
            customConfigPath = EditorUtility.OpenFilePanel("", customConfigPath, "json");
        }
        else
        {

            customConfigPath =  EditorPrefs.GetString(Application.productName + "FolderRenameWindow:renameJson", "");
        }
        
        if(string.IsNullOrEmpty(customConfigPath))
        {
            Log("文件空异常");
            return;
        }
        try
        {
            if(!File.Exists(customConfigPath))
            {
                Log("文件不存在");
                return;
            }
            string json = File.ReadAllText(customConfigPath);
            var node =  JsonConvert.DeserializeObject<FolderNode>(json, settings);
            UpdateAllNode(node);
            EditorPrefs.SetString(Application.productName + "FolderRenameWindow:renameJson", customConfigPath);
            Log("加载配置完毕");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载目录树失败: {e.Message}");
            
        }
    }
    public void SaveJson()
    {
        if(string.IsNullOrEmpty(customConfigPath))
        {
            customConfigPath = Path.Combine(Application.dataPath, "Editor/EditorExpand/~config").ToLinuxPath();
        }
        customConfigPath = EditorUtility.SaveFilePanel("", customConfigPath, "test","json");
        if(string.IsNullOrEmpty(customConfigPath))
        {
            return;
        }
        if(!File.Exists(customConfigPath))
        {
            if(customConfigPath.EndsWith(".json"))
            {
                File.Create(customConfigPath);
            }
            else
            {
                Log("文件不存在且不合法");   
            }
        }
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        try
        {
            string json = JsonConvert.SerializeObject(rootNode, settings);
            File.WriteAllText(customConfigPath, json);
            Log($"已保存配置{treeSavePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存目录树失败: {e.Message}");
        }
    }
    public void RefreshTree()
    {
        isInited = false;
        expandedStates.Clear();
        renameValues.Clear();
        showBatchRename = false;
        this.rootNode = null;
        AssetDatabase.Refresh();
        BuildDirectoryTree();
    }

    private string GetRelativePath(string fullPath)
    {
        return "Assets" + fullPath.Substring(Application.dataPath.Length);
    }
}