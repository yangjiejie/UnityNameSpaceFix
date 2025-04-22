using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using System;
using System.Linq.Expressions;


public class FolderCreatorWindow : EditorWindow
{
    private Vector2 scrollPosition; // 滚动位置
    private string basePath = "";
    private string folderName = "NewFolder";

    private Assembly hotFixAssembly;
    private Assembly HotFixAssembly // 热更程序集 
    {
        get
        {
            if(hotFixAssembly == null)
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                // 遍历每个程序集
                foreach (Assembly assembly in assemblies)
                {
                    if (assembly.GetName().Name == "hot_fix")
                    {
                        hotFixAssembly = assembly;
                        break;
                    }
                }
            }
            return hotFixAssembly;
        }
    }
    private Rect dropRect;
    private Dictionary<string, bool> subFolders = new Dictionary<string, bool>()
    {
        { "interface", true },
        { "ctr", true },
        { "help", true },
        { "mgr", true },
        { "util", true }
    };
    private int selectedIndex;
    private bool isTest = false;
    private string[] options = { "hot_ui", "runtime" };

    public static List<UnityEngine.Object> selectUnityReses = new List<UnityEngine.Object>();
    
    public static Dictionary<int, string> customModule = new();

    public Dictionary<string, bool> csFuncMatch = new() {
        { "ui",false},
        { "ctrl",false },
        { "mgr",false}
    };
    public struct MoveCsModuleContext
    {
        public string modulePath;  // 模块路径 
        public string filePath;
        public string fileFolder;
        public List<string> nameArray;
        public string targetFolder;
    }

    public List<MoveCsModuleContext> checkCSFiles = new();
    
    public Dictionary<string, string> allHotFixCsFiles = new();

    [MenuItem("Tools/Advanced Folder Creator")]
    public static void ShowWindow()
    {
        GetWindow<FolderCreatorWindow>("Folder Creator Pro");
    }

    [InitializeOnLoadMethod]
    static void RemoveCache()
    {
        selectUnityReses.Clear();
        customModule.Clear();
    }
    private void OnDestroy()
    {
        Selection.selectionChanged -= OnSelectResChange;
    }

    private void OnEnable()
    {
        Selection.selectionChanged -= OnSelectResChange;
        Selection.selectionChanged += OnSelectResChange;
        BuildCstoAssemblyMap();
    }
    void OnGUI()
    {
        DrawMainInterface();
        HandleDragAndDrop();
    }

    private void OnSelectResChange()
    {
        selectUnityReses.Clear();
        customModule.Clear();
    }

    void DrawMainInterface()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        isTest = GUILayout.Toggle(isTest, isTest ? "测试" : "正式", EditorStyles.toolbarButton);

        EditorGUILayout.EndHorizontal();
        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        {
            EasyUseEditorFuns.baseVersion = EditorGUILayout.TextField("版本号：", EasyUseEditorFuns.baseVersion);
            if (GUILayout.Button("清理本地库"))
            {
                EasyUseEditorFuns.DelFolderAllContens(EasyUseEditorFuns.baseCustomTmpCache, false);
                this.ShowNotification(new GUIContent("清理本地库完成"));
                
            }
            if (GUILayout.Button("回滚"))
            {
                FindRepeatRes.ReverseLocalSvn();
            }
        }
        
        EditorGUILayout.EndHorizontal();
       
        for (int i = 0; i < options.Length; i++)
        {
            bool isSelected = (selectedIndex == i);
            bool newSelected = EditorGUILayout.Toggle(options[i], isSelected);

            if (newSelected != isSelected && newSelected)
            {
                selectedIndex = i;

                Repaint(); // 确保UI立即更新
            }
        }
       
        // 基础路径输入
        EditorGUILayout.BeginHorizontal();

        

        GUILayout.Label("Base Path:", GUILayout.Width(70));
        if(string.IsNullOrEmpty(basePath))
        {
            basePath = GetPrefs($"{this.name}{selectedIndex}");
        }
        basePath = EditorGUILayout.TextField(basePath);
        if (GUILayout.Button("Browse", GUILayout.Width(80)))
        {
            BrowseFolder();
        }
        EditorGUILayout.EndHorizontal();

        // 拖拽区域提示
        dropRect = GUILayoutUtility.GetLastRect();
        GUI.Box(dropRect, GUIContent.none);

        // 文件夹名称输入
        folderName = EditorGUILayout.TextField("Folder Name", folderName);

        // 子文件夹选项
        GUILayout.Label("Subfolders:");
        EditorGUILayout.BeginVertical("box");
        foreach (var key in new List<string>(subFolders.Keys))
        {
            subFolders[key] = EditorGUILayout.ToggleLeft(key, subFolders[key]);
        }
        EditorGUILayout.EndVertical();

        // 创建按钮
        if (GUILayout.Button("Create Folder Structure", GUILayout.Height(40)))
        {
            CreateFolderStructure();
        }
        //选中预设 
        if(selectUnityReses.Count == 0)
        {
            Array.ForEach(Selection.objects ,(xx) => selectUnityReses.Add(xx));
            selectUnityReses.ForEach((xx) => customModule.Add(xx.GetInstanceID(), ""));
            
        }
       
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("选中的预设：");
        GUILayout.EndHorizontal();
        GUILayout.BeginVertical();
        checkCSFiles.Clear();
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.MinHeight(200),GUILayout.ExpandHeight(true));


        for (int i = 0; i < selectUnityReses.Count; i++)
        {
            var prefabPath = AssetDatabase.GetAssetPath(selectUnityReses[i]);
            if (string.IsNullOrEmpty(prefabPath)) continue;

            EditorGUILayout.BeginHorizontal();
            selectUnityReses[i] = EditorGUILayout.ObjectField(selectUnityReses[i],
                typeof(DefaultAsset), false, GUILayout.Width(200));

            

            var editorInsId = selectUnityReses[i].GetInstanceID();
            

            

            
            prefabPath = EasyUseEditorFuns.GetLinuxPath(prefabPath);
            var prefabName = Path.GetFileNameWithoutExtension(prefabPath);
            var moduleName = "";
            if (Regex.IsMatch(prefabPath, @"/hall/"))
            {
                moduleName = GetModuleName(prefabPath);
                moduleName = EasyUseEditorFuns.GetLinuxPath(moduleName);
                moduleName = moduleName.Substring(moduleName.LastIndexOf("/") + 1);
            }
            else
            {
                moduleName = GetModuleName(prefabPath,2);
                moduleName = EasyUseEditorFuns.GetLinuxPath(moduleName);
                moduleName = moduleName.Substring(moduleName.LastIndexOf("/") + 1);
            }
            // 显示普通模块 
            EditorGUILayout.LabelField("default module：" + moduleName);
            if (!string.IsNullOrEmpty(customModule[editorInsId]))
            {
                moduleName = customModule[editorInsId];
            }
            foreach(var item in csFuncMatch.Keys.ToList())
            {
                csFuncMatch[item] = false;
            }
            // 显示是否是ctrl   
            if (prefabName.EndsWith("Ctrl") || prefabName.EndsWith("Ctl") || prefabName.EndsWith("Control"))
            {
                csFuncMatch["ctrl"] = true;
                EditorGUILayout.LabelField($"func：ctrl");
            }
            else if(prefabName.ToLower().EndsWith("mgr") || prefabName.ToLower().EndsWith("manager"))
            {
                csFuncMatch["mgr"] = true;
                EditorGUILayout.LabelField($"func：mgr");
            }
            else
            {
                csFuncMatch["ui"] = true;
                EditorGUILayout.LabelField($"func：ui");
            }
            // 显示自定义模块  
            customModule[editorInsId] = EditorGUILayout.TextField("custom Module：", customModule[editorInsId]);
            EditorGUILayout.EndHorizontal();
            if (allHotFixCsFiles.TryGetValue(prefabName,out var getcsFile))
            {
                var node = new MoveCsModuleContext();
                node.modulePath = "hot_fix/module/" + moduleName+"/interface";
                node.filePath = getcsFile;
                node.fileFolder = Path.GetDirectoryName(getcsFile);
                node.targetFolder = "Assets/" + node.modulePath;
                node.nameArray = new List<string>() {
                    prefabName + ".cs",
                    "GenCode_" + prefabName + ".cs",

                }; 
                checkCSFiles.Add(node);
            }            
            
           


        }

        GUILayout.BeginVertical();
        EditorGUILayout.LabelField("包含的cs代码：");
        for (int i = 0; i < checkCSFiles.Count; i++)
        {
            GUILayout.BeginHorizontal();
            
            var genCodePath = checkCSFiles[i].nameArray[1];
            var folderPath = checkCSFiles[i].fileFolder;
            if (!File.Exists(folderPath + "/" + genCodePath))
            {
                EditorGUILayout.LabelField(checkCSFiles[i].filePath );
            }
            else
            {
                EditorGUILayout.LabelField(checkCSFiles[i].filePath + "--- " + genCodePath);
            }

            if(GUILayout.Button("跳转"))
            {
                
                EditorUtility.RevealInFinder(checkCSFiles[i].filePath);
               
            }

            
            GUILayout.EndHorizontal();
            
        }
        GUILayout.EndVertical();
        

        GUILayout.EndVertical();
        GUILayout.EndScrollView();

        if (GUILayout.Button("刷新ui"))
        {
            Repaint();
        }
        if (GUILayout.Button("代码迁移"))
        {
            try
            {
                
                for (int i = 0; i < checkCSFiles.Count; i++)
                {
                    EasyUseEditorFuns.CreateDir(checkCSFiles[i].targetFolder);
                    var fileName = checkCSFiles[i].nameArray[0];

                    // 显示进度条
                    bool cancel = EditorUtility.DisplayCancelableProgressBar(
                        "处理资源中",
                        $"正在处理: {fileName}",
                        (float)i / checkCSFiles.Count
                    );
                    if (File.Exists(checkCSFiles[i].fileFolder + "/" + fileName))
                    {
                        MoveAsset(checkCSFiles[i].fileFolder + "/" + fileName, checkCSFiles[i].targetFolder + "/" + fileName);
                        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate); // 立即刷新
                    }
                   
                    fileName = checkCSFiles[i].nameArray[1];

                    if(File.Exists(checkCSFiles[i].fileFolder + "/" + fileName))
                    {
                        MoveAsset(checkCSFiles[i].fileFolder + "/" + fileName, checkCSFiles[i].targetFolder + "/" + fileName);
                        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate); // 立即刷新
                    }

                    if (cancel)
                    {
                        EditorUtility.ClearProgressBar();
                        Debug.Log("用户取消了处理");
                        break;
                    }

                }
            }
            catch(Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                AssetDatabase.Refresh();
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }
        if(GUILayout.Button("代码同步"))
        {
            CodeMoveTool.ShowWindow();
        }
       
        

    }
    

    /// <summary>
    /// 
    /// </summary>
    /// <param name="source unity路径"></param>
    /// <param name="target unity路径"></param>
    void MoveAsset(string source ,string target)
    {
        //第一步先备份 
        if(!source.StartsWith("Assets") || !target.StartsWith("Assets"))
        {
            Debug.LogError("路径错误");
            return;
        }
        var unityResPathName = EasyUseEditorFuns.GetUnityAssetPath(source);
        var untiyTargetResPathName = EasyUseEditorFuns.GetUnityAssetPath(target); 
        var sourceFullPathName = Environment.CurrentDirectory  + "/" + source;
        string backupFilePath = Path.Combine(EasyUseEditorFuns.baseCustomTmpCache, unityResPathName);
        EasyUseEditorFuns.UnitySaveCopyFile(sourceFullPathName, backupFilePath,withMetaFile:true,withPathMetaFile:true,isShowLog:false);
        
        //第二步记录需要回退的文件路径 写入.path文件 
        
        var metaFilePath = Path.Combine(EasyUseEditorFuns.baseCustomTmpCache, untiyTargetResPathName + ".path");
        // 用额外的txt文件记录该文件的路径 方便回退
        EasyUseEditorFuns.WriteFileToTargetPath(metaFilePath, untiyTargetResPathName, isShowLog: false);

        

        //第三步 移动资源 
        AssetDatabase.MoveAsset(source, target);
    }

    public Assembly GetHotFixAssembly()
    {
        if(hotFixAssembly == null)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            // 遍历每个程序集
            foreach (Assembly assembly in assemblies)
            {
                if (assembly.GetName().Name == "hot_fix")
                {
                    hotFixAssembly = assembly;
                    break;
                }
            }
        }
        return hotFixAssembly;
    }

    string GetModuleName(string resName, int layer = 1)
    {
        DirectoryInfo dirInfo = null;
        while(layer > 0)
        {
            dirInfo = Directory.GetParent(resName);
            resName = dirInfo.FullName;
            --layer;
        }
        return dirInfo?.FullName;
    }

    void BrowseFolder()
    {
        string path = EditorUtility.OpenFolderPanel("Select Base Path", basePath, "");
        if (!string.IsNullOrEmpty(path))
        {
            basePath = ConvertToAssetPath(path);
            SavePrefs($"{this.name}{selectedIndex}", basePath);
            Repaint();
        }
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

    void SavePrefs(string key,string value)
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

    void HandleDragAndDrop()
    {
        Event evt = Event.current;

        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropRect.Contains(evt.mousePosition))
                    return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    ProcessDroppedItems();
                }
                evt.Use();
                break;
        }
    }

    void ProcessDroppedItems()
    {
        foreach (var obj in DragAndDrop.objectReferences)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (AssetDatabase.IsValidFolder(path))
            {
                basePath = path;
                break;
            }
        }
    }

    void CreateFolderStructure()
    {
        try
        {
            if (string.IsNullOrEmpty(folderName))
            {
                throw new System.ArgumentException("Folder name cannot be empty!");
            }

            string fullPath = Path.Combine(basePath, folderName);
            CreateDirectory(fullPath);

            foreach (var folder in subFolders)
            {
                if (folder.Value)
                {
                    string subPath = Path.Combine(fullPath, folder.Key);
                    CreateDirectory(subPath);
                }
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", 
                $"Folder structure created at: {fullPath}", "OK");
            
            // 关闭窗口
            Close();
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", 
                $"Creation failed: {e.Message}", "OK");
        }
    }

    void CreateDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            Debug.Log($"Created directory: {path}");
        }
        else
        {
            Debug.LogWarning($"Directory already exists: {path}");
        }
    }

    string ConvertToAssetPath(string systemPath)
    {
        if (systemPath.StartsWith(Application.dataPath))
        {
            return "Assets" + systemPath.Substring(Application.dataPath.Length);
        }
        return systemPath.Replace("\\", "/");
    }

    void BuildCstoAssemblyMap()
    {
        allHotFixCsFiles.Clear();
        var list = AssetDatabase.FindAssets("t:script", new string[] {
         "Assets/hot_fix"
        }).Select((xx) => AssetDatabase.GUIDToAssetPath(xx))
        .ToList();
        for(int i = 0; i < list.Count; i++)
        {
            var key = Path.GetFileNameWithoutExtension(list[i]);
            if (!allHotFixCsFiles.ContainsKey(key))
            {
                allHotFixCsFiles.Add(key, list[i]);
            }
        }
        
    }
}