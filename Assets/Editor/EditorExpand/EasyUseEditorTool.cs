using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using System.Reflection;
using System.Security.Cryptography;




using UnityEditor.SceneManagement;





public static class EasyUseEditorTool  // 简称euetool 
{

    static Component[] copiedComponents;
    private static List<string> matchFiles = new List<string>();
    private static List<string> findRstList = new();
    private static string[] sertchPaths = new string[] { Application.dataPath };	//搜索路径;

    public static string[] matchExtensions = new string[] { ".prefab", ".mat" }; //需要进行匹配的格式

    public static string hotFixAssemblyFolder = "Assets/hot_fix"; // 项目c#热更目录 
    public static string UIBaseClass = "UIPanelBase"; // ui的基础类 

    [MenuItem("GameObject/右键菜单/结点|预设存档", priority = -1)]
    static void SavePrefabNode()
    {
        var go = Selection.activeGameObject;

        PrefabUtility.SaveAsPrefabAsset(go, System.Environment.CurrentDirectory + $"/tools/{go.name}.prefab", out bool success);
        if (!success)
        {
            Debug.Log("保存失败");
        }
        else
        {
            Debug.Log("保存成功");
        }


    }

    [MenuItem("GameObject/右键菜单/拷贝顶层gameObject #%d", priority = -2)]
    static void CopyTopGameObject()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogWarning("No GameObject selected!");
            return;
        }

        // 复制选中的 GameObject
        GameObject copiedObject = GameObject.Instantiate(selected);
        copiedObject.name = selected.name;

        // 移除复制的 GameObject 的所有子节点
        for(int i = copiedObject.transform.childCount - 1; i >=0; --i)
        {
            GameObject.DestroyImmediate(copiedObject.transform.GetChild(i).gameObject);
        }

        var index = selected.transform.GetSiblingIndex() + 1; 
        // 设置父节点和位置
        copiedObject.transform.SetParent(selected.transform.parent,true);
        copiedObject.transform.SetSiblingIndex(index);
        if (copiedObject.transform is RectTransform it)
        {
            it.offsetMin = (selected.transform as RectTransform).offsetMin;
            it.offsetMax = (selected.transform as RectTransform).offsetMax;
        }
        copiedObject.transform.localPosition = selected.transform.localPosition;
        copiedObject.transform.localRotation = selected.transform.localRotation;
        copiedObject.transform.localScale = selected.transform.localScale;

        // 注册撤销操作
        Undo.RegisterCreatedObjectUndo(copiedObject, "Copy Selected Node Only");

        // 选中复制的 GameObject
        Selection.activeGameObject = copiedObject;
    }

    [MenuItem("GameObject/右键菜单/获取对象路径", priority = 0)]
    static void GetGameObjectPath()
    {
        var path = EasyUseEditorFuns.GetNodePath(Selection.activeGameObject);

        GUIUtility.systemCopyBuffer = path;
        Debug.Log(path.ToString());
    }




    public static bool isValidFileContent(string filePath1, string filePath2)
    {
        //创建一个哈希算法对象 
        using (HashAlgorithm hash = HashAlgorithm.Create())
        {
            using (FileStream file1 = new FileStream(filePath1, FileMode.Open), file2 = new FileStream(filePath2, FileMode.Open))
            {
                byte[] hashByte1 = hash.ComputeHash(file1);//哈希算法根据文本得到哈希码的字节数组 
                byte[] hashByte2 = hash.ComputeHash(file2);
                string str1 = BitConverter.ToString(hashByte1);//将字节数组装换为字符串 
                string str2 = BitConverter.ToString(hashByte2);
                return (str1 == str2);//比较哈希码 
            }
        }
    }


    

    [MenuItem("Tools/EasyUseEditorTool/打开streamingAssets目录", priority = 0)]
    static void OpenStreamingFolder()
    {

        var path = Application.streamingAssetsPath;
        var newpath = Path.GetFullPath(path);
       // System.Diagnostics.Process.Start("explorer.exe", newpath);
        EditorUtility.RevealInFinder(newpath);
       
    }

    [MenuItem("Tools/EasyUseEditorTool/打开存档目录", priority = 0)]
    static void OpenpersistentFolder()
    {
        
        var path = Application.persistentDataPath ;
        var newpath = Path.GetFullPath(path);
      //  System.Diagnostics.Process.Start("explorer.exe", newpath);
        EditorUtility.RevealInFinder(newpath);
    }
    [MenuItem("Tools/EasyUseEditorTool/打开临时缓存目录", priority = 0)]
    static void OpenTemporaryCachePath()
    {

        var path = Application.temporaryCachePath;
        var newpath = Path.GetFullPath(path);
        //System.Diagnostics.Process.Start("explorer.exe", newpath);
        EditorUtility.RevealInFinder(newpath);
    }

    
    

    
    
    static string recursiveFind(GameObject go,ref StringBuilder sb)
    {
        if (go != null)
        {
            
            if (go.transform.parent != null)
            {
                if(string.IsNullOrEmpty( recursiveFind(go.transform.parent.gameObject, ref sb)))
                {
                    sb.Append(go.name + "/");
                    return "";
                }
            }
            else
            {
                sb.Append(go.name + "/");
                return "";
            }
        }
        return "";
    }
    

    [MenuItem("Tools/EasyUseEditorTool/得到场景树目录")]
    public static string GetName()
    {
        StringBuilder sb = new StringBuilder();
        recursiveFind(Selection.activeGameObject.gameObject, ref sb);
        string path = sb.ToString();
        path = path.Substring(0, path.LastIndexOf("/"));
        Debug.Log("路径：" + path);
        TextEditor te = new TextEditor();
        te.text = path;
        te.SelectAll();
        te.Copy();
        return path;
    }
    public static void UnForbidLog(string filePath)
    {

        string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

        // 正则表达式匹配被注释的 Debug.Log 调用及其相关代码
        string pattern = unLogPattern;
        string replacement = @"$1";

        if (!Regex.IsMatch(fileContent, pattern))
        {
            return;
        }

        string newContent = Regex.Replace(fileContent, pattern, replacement);



        using (StreamWriter writer = new StreamWriter(filePath, false, new UTF8Encoding(false)))
        {
            writer.Write(newContent);
        }

    }
    private const string LogPattern = @"(?s)(Debug\.Log\s*\(.*?\);|UnityEngine\.Debug\.Log\s*\(.*?\);)";
    private const string unLogPattern = @"(?s)/\*(\s*Debug\.Log\s*\(.*?\);\s*|UnityEngine\.Debug\.Log\s*\(.*?\);\s*)\*/";

    public static void ForbidLog(string filePath)
    {

        string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

        // 正则表达式匹配 Debug.Log 调用及其相关代码
        string pattern = LogPattern;
        string replacement = @"/*$1*/";

        if (!Regex.IsMatch(fileContent, pattern))
        {
            return;
        }
        string newContent = Regex.Replace(fileContent, pattern, replacement);

        using (StreamWriter writer = new StreamWriter(filePath, false, new UTF8Encoding(false)))
        {
            writer.Write(newContent);
        }
    }
    [MenuItem("Tools/日志/注释所有日志", priority = -1999)]
    public static void ReMoveLog()
    {

        var listFolder = new List<string>();
        listFolder.Add(System.Environment.CurrentDirectory + "/Assets/Script");
        
        var allCsharpFiles = new List<string>();
        foreach (string folder in listFolder)
        {
            allCsharpFiles.AddRange(Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories));
        }
        string[] filters = new string[]
        {
        //    "Main.cs",
       
        };
        foreach (string csFile in allCsharpFiles)
        {
            var filename = Path.GetFileName(csFile);
            if (filters.Length > 0 && filters.Contains(filename))
            {
                Debug.Log("跳过过滤文件" + csFile);
                continue;
            }
            ForbidLog(csFile);
        }
        allCsharpFiles.Clear();
        listFolder.Clear();
        listFolder = null;
        allCsharpFiles = null;
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    [MenuItem("Tools/日志/反注释所有日志", priority = -1999)]
    public static void UnReMoveLog()
    {
        var listFolder = new List<string>();
        listFolder.Add(System.Environment.CurrentDirectory + "/Assets/Script");
      
        var allCsharpFiles = new List<string>();
        foreach (string folder in listFolder)
        {
            allCsharpFiles.AddRange(Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories));
        }
        foreach (string csFile in allCsharpFiles)
        {
            UnForbidLog(csFile);
        }
        allCsharpFiles.Clear();
        listFolder.Clear();
        listFolder = null;
        allCsharpFiles = null;
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    [MenuItem("Tools/EasyUseEditorTool/检查c#代码")]
    public static void CheckCsharpCode()
    {


        var newpath = System.Environment.CurrentDirectory + "./tools/开发工具/CodeCheck.bat";
        var pStartInfo = new System.Diagnostics.ProcessStartInfo(newpath);
        pStartInfo.Arguments = null;
        pStartInfo.CreateNoWindow = false; // 是否显示黑框 
        pStartInfo.UseShellExecute = false;
        pStartInfo.RedirectStandardError = false;
        pStartInfo.RedirectStandardInput = false;
        pStartInfo.RedirectStandardOutput = false;
        string workingDir = newpath;

        System.Diagnostics.Process tp = System.Diagnostics.Process.Start(pStartInfo);

        tp.WaitForExit(5000);
        var time = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
        var initTime = time;

        void record()
        {
            var filePath = Application.dataPath + "/../tools/tempLog.txt";

            if (File.Exists(filePath))
            {
                var allLogs = File.ReadAllLines(filePath);
                foreach (var slog in allLogs)
                {
                    Debug.LogError(slog);
                }
                EditorApplication.update = null;
            }
        }

        void Update()
        {
            if (System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond - initTime > 10000)
            {
                EditorApplication.update = null;
                return;
            }
            if (System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond - time > 2000)
            {
                time = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
                record();
            }

        }

        EditorApplication.update += () =>
        {

            Update();
        };

    }
    public static void OnSceneOpenOrPlay(string sPath)
    {
        string sSceneName = Path.GetFileNameWithoutExtension(sPath);
        bool bIsCurScene = EditorSceneManager.GetActiveScene().name.Equals(sSceneName);//是否为当前场景

        if (!Application.isPlaying)
        {
            if (bIsCurScene)
            {
                Debug.Log($"运行场景：{sSceneName}");
                EditorApplication.ExecuteMenuItem("Edit/Play");
            }
            else
            {
                Debug.Log($"打开场景：{sSceneName}");
                EditorSceneManager.OpenScene(sPath);
                EditorApplication.ExecuteMenuItem("Edit/Play");
            }
        }
        else
        {
            Debug.Log($"退出场景：{sSceneName}");
            EditorApplication.ExecuteMenuItem("Edit/Play");
        }
    }




    static private void FindRefByGUIDRaw(string[] sertchPathsRaw)
    {
        var findTargetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        Debug.Log("<color=#006400>开始查找" + Selection.activeObject.name + "</color>");

        EditorSettings.serializationMode = SerializationMode.ForceText;

        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (!string.IsNullOrEmpty(path))
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            int startIndex = 0;
            matchFiles.Clear();
            findRstList.Clear();
            foreach (var item in sertchPathsRaw)
            {
                string[] files = Directory.GetFiles(item + "", "*.*", SearchOption.AllDirectories)
                .Where(s => matchExtensions.Contains(Path.GetExtension(s).ToLower())).ToArray();
                matchFiles.AddRange(files);
            }
            if (matchFiles.Count > 0)
                EditorApplication.update = delegate ()
                {
                    string file = matchFiles[startIndex];

                    bool isCancel = EditorUtility.DisplayCancelableProgressBar("匹配资源中", file, (float)startIndex / (float)matchFiles.Count);

                    if (Regex.IsMatch(File.ReadAllText(file), guid))
                    {
                        var startCount = file.IndexOf("/Assets");
                        var newFilePath = file.Substring(startCount + 1);
                        Debug.Log(file, LoadInHierarchy(newFilePath));
                        findRstList.Add(file);
                    }

                    startIndex++;
                    if (isCancel || startIndex >= matchFiles.Count)
                    {
                        EditorUtility.ClearProgressBar();
                        EditorApplication.update = null;
                        startIndex = 0;
                        Debug.Log("<color=#006400>查找结束" + Selection.activeObject.name + "</color>");
                        StringBuilder sb = new StringBuilder();
                        findRstList.ForEach((xx) => sb.AppendLine(xx));
                        GUIUtility.systemCopyBuffer = sb.ToString();
                    }
                };
            else
                Debug.Log("<color=#006400>查找结束" + Selection.activeObject.name + "</color>");
        }
    }
    [MenuItem("Tools/代码行数")]
    public static void CalcCodeLine()
    {
        string[] fileName = System.IO.Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
        int totalLine = 0;
        foreach (var item in fileName)
        {
            int nowLine = 0;
            StreamReader sr = new StreamReader(item);
            while (sr.ReadLine() != null)
            {
                nowLine++;
            }
            totalLine += nowLine;
        }
        Debug.Log($"代码总行数: {totalLine}=> 代码文件数{fileName.Length}");
    }

    [MenuItem("Assets/右键工具/选中物体被引用查找", false, 0)]
    static private void FindRefByGUIDAvatars()
    {
        FindRefByGUIDRaw(new string[] { Application.dataPath , });
    }

    [MenuItem("Assets/右键工具/跳转到c#对应的行 _F12", false, 0)]
    static private void GotoCSharpCodeLine()
    {
        var go = Selection.activeGameObject;
        if (go == null) return;
        string varName = "";
        //匹配  结点名@类型名 中的 结点名@  因为我们ui的结点命名存在 变量名@类型名的设定 方便ui代码生成
        if(Regex.Match(go.transform.name, @"^(.*?)@").Success)
        {
            varName = Regex.Match(go.name, @"^(.*?)@").Groups[1].Value;
        }
        else
        {
            varName = go.name;
        }

        GameObject selectGo = null;
        var csName = "";
        if(PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            var RunGo = go.transform;
            while (RunGo.transform.parent.parent != null)
            {
                RunGo = RunGo.transform.parent;
            }
            csName = RunGo.name.Trim();
            selectGo = RunGo.gameObject;
        }
        else
        {
            var runGo = go.transform;
            while (runGo != null && runGo.GetComponent(UIBaseClass) == null)
            {
                runGo = runGo.transform.parent;
            }
            if (runGo == null) return;

            selectGo = runGo.gameObject;
            csName = runGo.GetComponent(UIBaseClass).GetType().Name;
           
           
        }
       
        var guids = AssetDatabase.FindAssets($"t:Script {csName}", new string[] 
        {
            hotFixAssemblyFolder
        });
        
        var listPath = guids.Select((x) => AssetDatabase.GUIDToAssetPath(x)).Where((x)=>x.EndsWith(csName+".cs")).ToList<string>();
        string targetGenCsFile = "";
        string targetInterfaceCsFile = "";

        if (listPath.Count > 0)
        {
            targetInterfaceCsFile = listPath[0];
            var tmpCsName = System.IO.Path.GetFileName(targetInterfaceCsFile);
            if(tmpCsName.StartsWith("GenCode_"))
            {
                targetGenCsFile = targetInterfaceCsFile;
                targetInterfaceCsFile = targetGenCsFile.Replace("GenCode_", "");
            }
            else
            {
                var folderName = Path.GetDirectoryName(targetInterfaceCsFile);
                targetGenCsFile = Path.Combine(folderName, "GenCode_" + tmpCsName);
            }

            var localGenCSFilePath = targetGenCsFile; 
            if (!File.Exists(localGenCSFilePath))
            {
                targetGenCsFile = "";
            }
        }

        //兼容之前的那套逻辑 
        if (string.IsNullOrEmpty(targetGenCsFile))
        {
            foreach (var item in listPath)
            {
                if (!item.Contains("generate"))
                {
                    targetInterfaceCsFile = item;
                }
                else
                {
                    targetGenCsFile = item;

                }
            }
        }
        if (string.IsNullOrEmpty(targetGenCsFile))
        {
            Debug.LogError("未找到类" + targetGenCsFile);
            return;
        }

        var finalCSPath = targetInterfaceCsFile;
        int codeNum = FindLineNumber(targetInterfaceCsFile, varName);
        if(codeNum < 0)
        {
            codeNum = FindLineNumber(targetGenCsFile, varName);
            finalCSPath = targetGenCsFile;
        }
        if (codeNum < 0)
        {
            Debug.Log("AssetDatabase.FindAssets耗时分析begin : " + Time.realtimeSinceStartup);
            guids = AssetDatabase.FindAssets($"t:Script", new string[]
            {
                "Assets/hot_fix"
            });
            

            listPath = guids.Select((x) => AssetDatabase.GUIDToAssetPath(x)).ToList<string>();
        
            Debug.Log("AssetDatabase.FindAssets耗时分析end : " + Time.realtimeSinceStartup);
            Debug.Log("遍历所有cs脚本耗时begin : " + Time.realtimeSinceStartup);
            var coms = Selection.activeGameObject.GetComponentsInParent<Component>(true);

            for (int i = 0; i < coms.Length && codeNum < 0; i++)
            {
                var has = listPath.Find((xx) => xx.EndsWith(coms[i].GetType().Name + ".cs"));
                if (!string.IsNullOrEmpty(has))
                {
                    finalCSPath = has;
                    codeNum = FindLineNumber(has, varName);
                }
            }
            Debug.Log("遍历所有cs脚本耗时end : " + Time.realtimeSinceStartup);
        }

        if (codeNum < 0)
        {
            Debug.LogError(finalCSPath + "未找到定义" + varName);
            return;
        }
        if (File.Exists(finalCSPath))
        {
            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(finalCSPath, codeNum);
        }
        else
        {
            Debug.LogError("文件不存在" + finalCSPath);
        }
        
        int FindLineNumber(string filePath, string variableName)
        {
            string[] lines = System.IO.File.ReadAllLines(filePath);
            // 先精准匹配
            for (int i = 0; i < lines.Length; i++)
            {
                if (Regex.IsMatch(lines[i], $@"/b{Regex.Escape(variableName)}/b"))
                {
                    return i + 1;
                }
            }
            //然后模糊匹配
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(variableName))
                {
                    return i + 1;
                }
            }
            return -1;
        }
    }




    static UnityEngine.Object LoadInHierarchy(string relativePath)
    {
        UnityEngine.Object rst = null;
        //foreach (var oneMatch in matchExtensions)
        //{
        //    if (Regex.IsMatch(relativePath, oneMatch))
        //    {
        //        //预设要放到Hierarchy中 
        //        rst = AssetDatabase.LoadAssetAtPath<GameObject>(relativePath);
        //        GameObject obj = GameObject.Instantiate<GameObject>(rst as GameObject); //PrefabUtility.InstantiatePrefab(Selection.activeObject as GameObject) as GameObject;
        //        obj.transform.SetParent(GameObject.Find("UGuiSystem/MainCanvas/WindowLayer").transform, false);
        //        Selection.activeGameObject = obj;
        //        return rst;
        //    }
        //}
        if (rst == null)
        {
            rst = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativePath);
        }
        return rst;
    }
    [MenuItem("Assets/右键工具/选中cs在场景中的路径", false, 1)]
    public static void GetPathInScene()
    {
        var findTargetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        findTargetPath = findTargetPath.Substring(findTargetPath.LastIndexOf("/") + 1, findTargetPath.LastIndexOf(".") - findTargetPath.LastIndexOf("/") - 1);
        Assembly assembly = System.Reflection.Assembly.Load("Assembly-CSharp");
        var objs = Resources.FindObjectsOfTypeAll(assembly.GetType("SCG." + findTargetPath));
        foreach(var item in objs)
        {
           
            Debug.Log(AssetDatabase.GetAssetPath(item) + " -- " + EasyUseEditorFuns.GetNodePath((item as Component).gameObject) );
        }
    }

    [MenuItem("Assets/右键工具/获取场景中所有相机的路径", false, 1)]
    public static void GetCameraPathInScene()
    {        
        var objs = Resources.FindObjectsOfTypeAll(typeof(Camera));
        foreach (var item in objs)
        {
            Debug.Log(EasyUseEditorFuns.GetNodePath((item as Component).gameObject));
        }
    }

    [MenuItem("Assets/右键工具/获取场景中所有audiolistener的路径", false, 1)]
    public static void GetAudioListenerPathInScene()
    {
        var objs = Resources.FindObjectsOfTypeAll(typeof(AudioListener));
        foreach (var item in objs)
        {
            Debug.Log(EasyUseEditorFuns.GetNodePath((item as Component).gameObject));
        }
    }

   
    [MenuItem("Tools/EasyUseEditorTool/copy所有脚本")]
    static void CopyAllComponents()
    {
        copiedComponents = Selection.activeGameObject.GetComponents<Component>();
    }

    [MenuItem("Tools/EasyUseEditorTool/paste所有组件")]
    static void PasteAllCom()
    {
        var goes = Selection.gameObjects;
      
        foreach (var targetGameObject in Selection.gameObjects)
        {
            if (!targetGameObject || copiedComponents == null) continue;
            foreach (var copiedComponent in copiedComponents)
            {
                if (!copiedComponent) continue;
                
                
                UnityEditorInternal.ComponentUtility.CopyComponent(copiedComponent);
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(targetGameObject);
            }
        }
    }

    [MenuItem("Tools/EasyUseEditorTool/删除所有组件除了transform")]
    static void DeleteAllComponentsButTransForm()
    {
        foreach (var targetGameObject in Selection.gameObjects)
        {
            var coms = targetGameObject.GetComponents<Component>();
            foreach(var com in coms)
            {
                if(com as Transform == null)
                {
                    GameObject.DestroyImmediate(com);
                }
            }
        }
    }
   

    [MenuItem("Tools/EasyUseEditorTool/获取Meta检测工具")]
    static void GetMetaDetector()
    {
        string path = Application.dataPath + "/../../Shared/EditorTools/MetaDetector";
        string targetPath = Application.dataPath + "/ExternalEditor/MetaDetector";
        if (Directory.Exists(targetPath))
        {
            DirectoryInfo di = new DirectoryInfo(targetPath);
            di.Delete(true);
        }
        else
        {
            EasyUseEditorFuns.CreateDir(targetPath); // 创建目录  
        }
        path = Path.GetFullPath(path);
        targetPath = Path.GetFullPath(targetPath);
        EasyUseEditorFuns.CopyDirectory(path, targetPath);
        Debug.Log($"拷贝文件{path} --> {targetPath}成功");
        AssetDatabase.Refresh();
    }
    [MenuItem("Tools/EasyUseEditorTool/获取妆容导出数据")]
    public static void GetMakeupData()
    {
        //string path = FaceTool.FaceToolDef.FaceToolSaveFolder;
        //string file = EditorUtility.SaveFilePanel("导出数据", path, DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"), "txt");

        //var luafunc =  LuaMainState.instance.GetLuaFunction("LuaAvatarUtils.GetAvatarExportData");
        //var rst = luafunc.Invoke<LuaInterface.LuaTable>();
        //var rst2 = rst.ToDictTable();
        //var avataDataList = new List < FaceTool.AvataExportData >();
        //foreach (var item in rst.ToDictTable())
        //{
        //    var tab = (item.Value as LuaInterface.LuaTable).ToDictTable();
        //    int id =  int.Parse((item.Key.ToString()));
        //    var isMakeUp = FaceTool.FaceToolDef.IsMakeUp(id);
        //    if (isMakeUp)
        //    {
        //        foreach (var item2 in (tab))
        //        {
        //            if (item2.Key == "id")
        //            {
        //                continue;
        //            }
        //            var data = new FaceTool.AvataExportData();
        //            data.id = id;
        //            data.value = item2.Value.ToString();
        //            avataDataList.Add(data);
        //            Debug.Log(item2.Key);
        //        }
        //    }
        //}

        //proto.RoleAvatarTo pbData = new proto.RoleAvatarTo();
        //for (int i = 0; i < avataDataList.Count; i++)
        //{
        //    pbData.avatars.Add(new proto.AvatarExportData()
        //    {
        //        id = avataDataList[i].id,
        //        value = avataDataList[i].value,
        //    });
        //}

        //MemoryStream stream = new MemoryStream();
        //ProtoBuf.Serializer.Serialize(stream, pbData);
        //byte[] bytes = stream.ToArray();
        //File.WriteAllBytes(file, bytes);
        
    }

    [MenuItem("Assets/右键工具/获取依赖", false, 1)]
    private static void GetDependncy()
    {
        
       
        var arr  = AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(Selection.objects[0]));
    }

    [MenuItem("Assets/右键工具/图片-->Sprite",false,1)]
    private static void TextureTypeToSprite()
    {
        foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets))
        {
            if (obj is Texture)
            {
                AssetDatabase.Refresh();
                TextureImporter textureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(obj)) as TextureImporter;
                textureImporter.spriteImportMode = SpriteImportMode.Single;
                textureImporter.npotScale = TextureImporterNPOTScale.None;
                textureImporter.textureType = TextureImporterType.Sprite;
                textureImporter.alphaIsTransparency = true;
                textureImporter.textureCompression = TextureImporterCompression.Compressed;
                textureImporter.mipmapEnabled = false;
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(obj));
                AssetDatabase.Refresh();
            }

        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Assets/右键工具/选中物体被引用查找2", false, 0)]
    static private void FindRefByGUID()
    {

        var gameObject = Selection.activeObject;
        // 获取当前选中的资源路径
        string targetAssetPath = AssetDatabase.GetAssetPath(gameObject);
        if (string.IsNullOrEmpty(targetAssetPath))
        {
            Debug.LogWarning("请选择一个资源文件！");
            return;
        }

        // 获取项目中所有资源的路径
        string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();

        // 存储依赖目标资源的资源路径
        List<string> dependents = new List<string>();

        // 遍历所有资源
        foreach (string assetPath in allAssetPaths)
        {
            if (assetPath == targetAssetPath) continue;
            // 排除文件夹和非资源文件
            if (assetPath.StartsWith("Assets/") && !AssetDatabase.IsValidFolder(assetPath))
            {
                // 获取当前资源的依赖项
                string[] dependencies = AssetDatabase.GetDependencies(assetPath, false);

                // 检查是否依赖目标资源
                if (System.Array.Exists(dependencies, d => d == targetAssetPath))
                {
                    dependents.Add(assetPath);
                }
            }
        }
        if(dependents.Count == 0)
        {
            Debug.Log($"资源 {targetAssetPath} 没有任何依赖：");
        }
        else
        {
            var  sb = new StringBuilder();
            foreach(var item in dependents)
            {
                sb.AppendLine(item);
            }
            
            Debug.Log(targetAssetPath + "被这些资源依赖" + sb.ToString());
        }
       

    }


}
