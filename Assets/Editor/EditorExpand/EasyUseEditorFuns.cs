using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System;
using System.Reflection;
using UnityEditor.Build;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor.SceneManagement;
using System.Linq;
using JetBrains.Annotations;


public static class EasyUseEditorFuns
{
    


    public static string _baseVersion;
    
    public static string baseVersion
    {
        get
        {
           
            _baseVersion = EditorPrefs.GetString(nameof(baseVersion), "1");
            return _baseVersion;
        }
        set
        {
            if (_baseVersion != value)
            {
                _baseVersion = value;
                EditorPrefs.SetString(nameof(baseVersion), value);
            }
        }
    }


    public static string baseCustomTmpCache
    {
        get
        {
            return System.Environment.CurrentDirectory + "/../mySvn/" + baseVersion;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="modulePath 是一个路径 类似hot_fix/hall/chat/interface"></param>
    /// <returns></returns>
    public static string GetNameSpaceStringPath(string modulePath)
    {
        var tmpNameSpace = modulePath.Replace("/", ".");


        if (tmpNameSpace.Contains("hot_fix"))
        {
            tmpNameSpace = tmpNameSpace.Replace("hot_fix", "HotFix");
        }

        var nameSpaceArray = tmpNameSpace.Split('.');
        for (int i = 0; i < nameSpaceArray.Length; i++)
        {
            if (char.IsLower(nameSpaceArray[i][0]))
            {
                nameSpaceArray[i] = CapitalizeFirstLetter(nameSpaceArray[i]);
            }

        }
        //首字母大写 
        string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return char.ToUpper(input[0]) + input.Substring(1);
        }


        tmpNameSpace = string.Join(".", nameSpaceArray);
        if (tmpNameSpace.Contains("Hotfix"))
        {
            tmpNameSpace = tmpNameSpace.Replace("Hotfix", "HotFix");
        }
        return tmpNameSpace;
    }
    /// <summary>
    /// 获取目录内所有文件 
    /// </summary>
    /// <param name="folders"></param>
    /// <param name="suffix"></param>
    /// /// filter 过滤器 过滤某些文件等 
    /// <returns></returns>
    public static List<string> GetAllFiles(string[] folders,string suffix,Func<string,bool> filter = null)
    {
        List<string> files = new();
        for(int i = 0; i < folders.Length; i++)
        {
            if (!Directory.Exists(folders[i])) continue;
            files.AddRange( Directory.GetFiles(folders[i], suffix, SearchOption.AllDirectories) ) ;
        }
        files = files.ConvertAll((xx) => xx.ToLinuxPath());
        if (filter != null)
        {
            files = files.Where(f => filter(f)).ToList();
        }
        return files;
        
    }
    static List<string> csFilterFolder = new()
    {
        "Resources","RTLTMPro","StreamingAssets","TextMesh Pro","Reporter","Plugins","Firebase",
        "FacebookSDK","AppleAuth","3rdParty"
    };
    static List<string> csFileFilter = new()
    {
        "NameSpaceFix.cs","CodeMoveTool.cs","FixNameSpaceError.cs","FolderRenameWindow.cs"
    };
    public static List<string> GetAllUserLayerCSCode()
    {
        var allCsFiles = GetAllFiles(new string[] {
                    "Assets"
                }, "*.cs", (file) =>
                {
                    bool rst = csFilterFolder.Any((ll) => Regex.IsMatch(file, @$"/Assets/{ll}/"));
                    rst |= csFileFilter.Any((ll) => file.EndsWith(ll));
                    return !rst;
                });
        return allCsFiles;
    }


    public static string GetLinuxPath(string s)
    {
        return s.Replace("\\", "/");
    }

    public static string ToLinuxPath(this string s)
    {
        return GetLinuxPath(s);
    }

    public static string ToWindowsContent(this string s)
    {
        s = s.Replace("\r\n", "\n");
        s = s.Replace("\n", "\r\n");
        return s;
    }

    public static string ToUnityPath(this string s,bool withAsset = true)
    {
        return GetUnityAssetPath(s,withAsset);
    }
    /// <summary>
    /// 获取unity资源路径 
    /// </summary>
    /// <param name="fullPath全路径"></param>
    /// <returns></returns>
    public static string GetUnityAssetPath( string fullPath,bool withAsset = true)
    {
        fullPath = GetLinuxPath(fullPath);
        var index = fullPath.IndexOf("Assets");
        if (index >= 0 )
        {
            if (withAsset)
                return fullPath.Substring(index);
            int idx = index + "Assets/".Length;
            if (idx > fullPath.Length - 1)
            {
                return "";
            }
            else
            {
                return fullPath.Substring(index + "Assets/".Length);
            }
            
        }
        return fullPath;
    }

    /// <summary>
    /// 拷贝unity的文件从source到taget 并且也拷贝meta文件
    /// </summary>
    /// <param name="source 绝对路径"></param>
    /// <param name="target 绝对路径"></param>
    /// <param name="withMetaFile 是否将meta文件一并move"></param>

    public static void UnitySaveMoveFile(string source, string target, bool withMetaFile = true,bool withPathMetaFile = false)
    {
        try
        {
            source = GetLinuxPath(source);
            target = GetLinuxPath(target);
            var sourceFolder = System.IO.Path.GetDirectoryName(source);
            var targetFolder = System.IO.Path.GetDirectoryName(target);
            sourceFolder = Path.GetFullPath(sourceFolder);
            targetFolder = Path.GetFullPath(targetFolder);
            CreateDir(sourceFolder);
            CreateDir(targetFolder);

            var sourceName = System.IO.Path.GetFileName(source);
            var targetName = System.IO.Path.GetFileName(target);
            //拷贝源文件  
            if(File.Exists(target))
            {
                File.Delete(target);
            }
            System.IO.File.Move(source, target);
            //拷贝meta文件 
            if (withMetaFile)
            {
                var metaSourceFile = Path.Combine(sourceFolder, sourceName + ".meta");
                var metaTargeFile = Path.Combine(targetFolder, targetName + ".meta");
                if(File.Exists(metaTargeFile))
                {
                    File.Delete(metaTargeFile);
                }
                System.IO.File.Move(metaSourceFile, metaTargeFile);
            }

            if (withPathMetaFile)
            {
                var metaFilePath2 = target + ".path";
                var targetUnityAssetPathName = target.Substring(target.IndexOf("Assets/"));
                // 用额外的txt文件记录该文件的路径 方便回退
                EasyUseEditorFuns.WriteFileToTargetPath(metaFilePath2, targetUnityAssetPathName, false);
            }

        }
        catch (Exception e)
        {
            Debug.LogError(source + "\n" + e);
        }


    }
   
    /// 
    /// <summary>
    /// 拷贝unity的文件从source到taget 并且也拷贝meta文件
    /// </summary>
    /// <param name="source 绝对路径"></param>
    /// <param name="target 绝对路径"></param>
    /// <param name="overrite"></param>
    /// <param name="withPathMetaFile 若为true则会自动写入.path文件记录之前所在的文件夹"></param> 
    public static void UnitySaveCopyFile(string source, string target, bool overrite = true,bool withMetaFile = true,bool withPathMetaFile = false,bool isShowLog = true)
    {
        try
        {
            source = GetLinuxPath(source);
            target = GetLinuxPath(target);
            var sourceFolder = System.IO.Path.GetDirectoryName(source);
            var targetFolder = System.IO.Path.GetDirectoryName(target);
            sourceFolder = Path.GetFullPath(sourceFolder);
            targetFolder = Path.GetFullPath(targetFolder);
            CreateDir(sourceFolder);
            CreateDir(targetFolder);

            var sourceName = System.IO.Path.GetFileName(source);
            var targetName = System.IO.Path.GetFileName(target);
            //拷贝源文件  
            System.IO.File.Copy(source, target, overrite);
            //拷贝meta文件 
            if (withMetaFile)
            {
                var metaSourceFile = Path.Combine(sourceFolder, sourceName + ".meta");
                var metaTargeFile = Path.Combine(targetFolder, targetName + ".meta");
                System.IO.File.Copy(metaSourceFile, metaTargeFile, overrite);
            }
            if (withPathMetaFile)
            {
                var metaFilePath2 = target + ".path";
                var targetUnityAssetPathName = target.Substring(target.IndexOf("Assets/"));
                // 用额外的txt文件记录该文件的路径 方便回退
                EasyUseEditorFuns.WriteFileToTargetPath(metaFilePath2, targetUnityAssetPathName, isShowLog);
            }
        }
        catch(Exception e)
        {
            Debug.LogError(source +"\n"+ e);
        }
        
        
    }
    /// <summary>
    /// 参数2 是否存档 
    /// </summary>
    /// <param name="resPath"></param>
    /// <param name="isSaveToLocal"></param>
    public static void DelEditorResFromDevice(string resPath, bool isSaveToLocal = true)
    {
        try
        {
            if (!isSaveToLocal)
                Debug.Log($"{resPath}已删除且不存档");

            AssetDatabase.DeleteAsset(resPath);
            AssetDatabase.DeleteAsset(resPath + ".meta");
        
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.ToString());
        }


    }
    public static string CalculateMD5(string filePath)
    {
        using (var md5 = MD5.Create())
        {
            using (var stream = File.OpenRead(filePath))
            {
                // 计算文件的 MD5 哈希值
                byte[] hashBytes = md5.ComputeHash(stream);

                // 将字节数组转换为十六进制字符串
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2")); // "x2" 表示两位小写十六进制
                }

                return sb.ToString();
            }
        }
    }

    public static string GetScriptSymble(out NamedBuildTarget nameBt)
    {
        BuildTarget target = BuildTarget.iOS;
#if UNITY_ANDROID
		target = BuildTarget.Android;

#elif UNITY_WEBGL
		target = BuildTarget.WebGL;

#elif UNITY_STANDALONE
        target = BuildTarget.StandaloneWindows;
#endif
        string symbles = "";
        NamedBuildTarget nameBuildTarget;
        if (target == BuildTarget.iOS)
        {
            nameBuildTarget = NamedBuildTarget.iOS;
        }
        else if (target == BuildTarget.Android)
        {
            nameBuildTarget = NamedBuildTarget.Android;
        }
        else if (target == BuildTarget.WebGL)
        {
            nameBuildTarget = NamedBuildTarget.WebGL;
        }
        else
        {
            nameBuildTarget = NamedBuildTarget.Standalone;
        }
        symbles = PlayerSettings.GetScriptingDefineSymbols(nameBuildTarget);
        nameBt = nameBuildTarget;
        return symbles;
    }
    /// <summary>
    /// 是否具有预编译宏 xx
    /// </summary>
    /// <param name="symble"></param>
    /// <returns></returns>
    public static bool HasDebugSymble(string symble)
    {
        NamedBuildTarget nameBt;
        var symbles = GetScriptSymble(out nameBt);
        if (!string.IsNullOrEmpty(symbles))
        {
            var symble_arr = symbles.Split(";");
            var index = Array.IndexOf(symble_arr, symble);
            if (index >= 0)
            {
                return true;
            }
        }
        return false;
    }
    public static void SetUnitySymbleDefine(bool isDebug, string symble)
    {
        NamedBuildTarget nameBt;
        var symbles = GetScriptSymble(out nameBt);
        if (isDebug)
        {
            if (!string.IsNullOrEmpty(symbles))
            {
                var symble_arr = symbles.Split(";");
                if (Array.IndexOf(symble_arr, symble) < 0)
                {
                    symbles += $";{symble}";
                }
            }
            else
            {
                symbles = symble;
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(symbles))
            {
                var symble_arr = symbles.Split(";");
                var index = Array.IndexOf(symble_arr, symble);
                if (index >= 0)
                {
                    if (index == symble_arr.Length - 1)
                    {
                        symbles = index > 0 ? symbles.Replace($";{symble}", "") : symbles.Replace(symble, "");
                    }
                    else
                    {
                        symbles = symbles.Replace($"{symble};", "");
                    }
                }

            }
        }
     
        PlayerSettings.SetScriptingDefineSymbols(nameBt, symbles);

        AssetDatabase.Refresh();

    }
    /// <summary>
    /// 任何情况下都需要能写入一个文件
    /// </summary>
    /// <param name="filePath 全路径"></param>
    /// <param name="contents unity资源路径"></param>
    public static void WriteFileToTargetPath(string filePath, string contents,bool isShowLog  = true)
    {
        contents = GetUnityAssetPath(contents);
        filePath = Path.GetFullPath(filePath);
        var folderName = System.IO.Path.GetDirectoryName(filePath);
        if (!Directory.Exists(folderName))
        {
            CreateDir(folderName);
        }
        File.WriteAllText(filePath, contents);
        var writeFilePath = EasyUseEditorFuns.GetLinuxPath(baseCustomTmpCache);
        filePath = filePath.Replace(baseCustomTmpCache + "/", "");

        if(isShowLog)
            EditorLogWindow.WriteLog(filePath.Replace(".path", ""));
    }

    public static int CreateDir(string path)
    {

        if (Directory.Exists(path))
        {
            return 1;
        }
        path = GetLinuxPath(path);
        string tmp = path.Substring(0, path.LastIndexOf("/"));
        if (1 == CreateDir(tmp))
        {
            if (!(path.LastIndexOf(".") > 0))
            {
                Directory.CreateDirectory(path);
                return 1;
            }
        }
        return 0;
    }
    public static GameObject GetSelectObject()
    {
        return Selection.activeGameObject;
    }

    public static string GetNodePath(GameObject go)
    {
        var parent = go.transform.parent;
        return parent == null ? go.name : GetNodePath(parent.gameObject) + "/" + go.name;
    }
    public static List<GameObject> GetAllPrefabFormFolder(string curPath)
    {
        List<GameObject> goes = new List<GameObject>();
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new string[] { curPath });
        for (int i = 0; i < guids.Length; i++)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject go = (GameObject)AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject));
            goes.Add(go);
        }
        return goes;
    }


    public static List<string> GetSelFoloderPath()
    {
        UnityEngine.Object[] selObjs = Selection.GetFiltered(
            typeof(UnityEngine.Object),
            SelectionMode.DeepAssets);
        return null;
    }
    public static List<GameObject> GetSelectPrefabs() // 也可以用于选中单个prefab
    {

        List<GameObject> tmp = new List<GameObject>();
        var gos = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets);
        foreach (var go in gos)
        {
            if (go as GameObject)
            {
                tmp.Add(go as GameObject);
            }
        }
        return tmp;
    }

    public static AudioClip GetSelectAudio()
    {
        var go = Selection.activeObject as AudioClip;
        return go;

    }

    public static List<UnityEngine.Material> GetSelectMaterials()
    {
        List<UnityEngine.Material> tmp = new List<UnityEngine.Material>();
        var gos = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets);
        foreach (var go in gos)
        {
            if (go as UnityEngine.Material)
            {
                tmp.Add(go as UnityEngine.Material);
            }
        }
        return tmp;
    }


    public static GameObject GetSelGameObjectInHierarchy()
    {
        return Selection.activeGameObject;
    }
    //获取选中对象路径 在展示面板上 
    public static string GetSelObjPathInHierarchy(GameObject go)
    {

        List<string> pathStack = new List<string>();
        while (go != null)
        {
            pathStack.Add(go.name);
            if (go.transform.parent != null)
            {
                go = go.transform.parent.gameObject;
            }
            else
            {
                break;
            }
        }
        string str = "";
        for (int i = pathStack.Count - 1; i >= 0; i--)
        {
            if (i == pathStack.Count - 1)
            {
                str += pathStack[i];
            }
            else
            {
                str += "/" + pathStack[i];
            }

        }
        return str;
    }

    /// <summary>
    /// 拷贝目录
    /// </summary>
    /// <param name="oldpath">源目录</param>
    /// <param name="newpath">新目录</param>
    public static void CopyDirectory(string oldpath, string newpath,bool withFolderName = true)
    {
        oldpath = oldpath.Replace("\\", "/");
        newpath = newpath.Replace("\\", "/");
        if (string.IsNullOrWhiteSpace(newpath)) return;
        var folderName = oldpath.Substring(oldpath.LastIndexOf("/", StringComparison.Ordinal) + 1);
        var desfolderdir = withFolderName ? newpath + "/" + folderName : newpath;
        if (newpath.LastIndexOf("/", StringComparison.Ordinal) == (newpath.Length - 1))
        {
            desfolderdir = withFolderName ? newpath + folderName : newpath;

        }
        var filenames = Directory.GetFileSystemEntries(oldpath);
        foreach (string file in filenames)
        {
            var file2 = file.Replace("\\", "/");
            if (Directory.Exists(file))
            {
                var currentdir = desfolderdir + "/" + file2.Substring(file2.LastIndexOf("/", StringComparison.Ordinal) + 1);
                if (!Directory.Exists(currentdir))
                {
                    Directory.CreateDirectory(currentdir);

                }
                CopyDirectory(file2, desfolderdir);
            }
            else
            {
                var srcfileName = file2.Substring(file2.LastIndexOf("/", StringComparison.Ordinal) + 1);
                srcfileName = desfolderdir + "/" + srcfileName;
                if (!Directory.Exists(desfolderdir))
                {
                    Directory.CreateDirectory(desfolderdir);
                }
                File.Copy(file, srcfileName, true);
            }
        }
    }

    /// <summary>
    /// 调用公开的静态方法
    /// </summary>
    /// <param name="type">类的类型</param>
    /// <param name="method">类里要调用的方法名</param>
    /// <param name="parameters">调用方法传入的参数</param>

    public static object InvokePublicStaticMethod(System.Type type, string method, params object[] parameters)
    {
        var methodInfo = type.GetMethod(method, BindingFlags.Public | BindingFlags.Static);
        if (methodInfo == null)
        {
            UnityEngine.Debug.LogError($"{type.FullName} not found method : {method}");
            return null;
        }
        return methodInfo.Invoke(null, parameters);
    }

    /// <summary>
    /// 调用私有的静态方法
    /// </summary>
    /// <param name="type">类的类型</param>
    /// <param name="method">类里要调用的方法名</param>
    /// <param name="parameters">调用方法传入的参数</param>
    public static object InvokeNonPublicStaticMethod(System.Type type, string method, params object[] parameters)
    {
        var methodInfo = type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static);
        if (methodInfo == null)
        {
            UnityEngine.Debug.LogError($"{type.FullName} not found method : {method}");
            return null;
        }
        return methodInfo.Invoke(null, parameters);
    }

#if UNITY_EDITOR
    [MenuItem("Tools/EasyUseEditorTool/find脚本引用丢失")]
    static void FindMissing()
    {
        // 获取所有预设的 GUID
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");

        // 遍历所有预设
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
            {
                Debug.Log($"无法加载预设: {path}");
                continue;
            }

            // 检查预设中是否有丢失的脚本
            List<Component> missingComponents = new List<Component>();
            CheckForMissingScripts(prefab, missingComponents);

            if (missingComponents.Count > 0)
            {
                Debug.Log($"预设 {path} 中有 {missingComponents.Count} 个丢失的脚本:");
                foreach (var component in missingComponents)
                {
                    if (component != null)
                        Debug.Log($"- 丢失的脚本: {component.name}");
                }
            }
        }

        Debug.Log("查找完成！");
    }
    private static void CheckForMissingScripts(GameObject gameObject, List<Component> missingComponents)
    {
        // 检查当前 GameObject 的所有组件
        Component[] components = gameObject.GetComponents<Component>();
        foreach (var component in components)
        {
            if (component == null)
            {
                missingComponents.Add(component);
            }
        }

        // 递归检查子对象
        foreach (Transform child in gameObject.transform)
        {
            CheckForMissingScripts(child.gameObject, missingComponents);
        }
    }
#endif
    /// <summary>
    /// 删除所有目录下的文件
    /// </summary>
    /// <param name="directoryPath"></param>
    public static void DelFolderAllContens(string directoryPath,bool includeSelf = false,string filter = "",bool
        isSaveArchive = false)
    {
        directoryPath = GetLinuxPath(directoryPath);
        // 检查目录是否存在
        if (Directory.Exists(directoryPath))
        {
            // 删除目录下的所有文件
            string[] files = Directory.GetFiles(directoryPath);
            foreach (string file in files)
            {

                if (string.IsNullOrEmpty(filter) || file.EndsWith(filter))
                {
                    if (!file.EndsWith(".meta"))
                    {
                        UnitySaveCopyFile(file, baseCustomTmpCache + "/" + file.ToUnityPath(), true, true, true, false);
                    }


                    File.Delete(file);
                    if (File.Exists(file + ".meta"))
                    {
                        File.Delete(file + ".meta");
                    }
                }


                
            }

            // 递归删除所有子目录及其内容
            string[] subDirectories = Directory.GetDirectories(directoryPath);
            foreach (string subDirectory in subDirectories)
            {
                DelFolderAllContens(subDirectory);
                Directory.Delete(subDirectory);
            }
            if (includeSelf)
                Directory.Delete(directoryPath);
        }
    }

    public static bool IsFormatCorrect(out bool isInPrefabStage)
    {
        UnityEngine.Object selectedObject = Selection.activeObject;
        if (selectedObject != null)
        {
            string path = AssetDatabase.GetAssetPath(selectedObject);
            if (!string.IsNullOrEmpty(path))
            {
                string extension = System.IO.Path.GetExtension(path).ToLower();
                if (extension.Contains(".prefab"))
                {
                    isInPrefabStage = false;
                    return true;
                }
            }
            else
            {
                if (Regex.IsMatch(selectedObject.name, selectedObject.name) &&
                PrefabStageUtility.GetCurrentPrefabStage() != null &&
                selectedObject as GameObject != null )
                {
                    isInPrefabStage = true;
                    return true;

                }
            }


        }
        isInPrefabStage = false;
        return false;
    }

    public static  void GetSpineDependency()
    {
        var resPath = AssetDatabase.GetAssetPath(Selection.activeObject);

        var folderName = System.IO.Path.GetDirectoryName(resPath);

        var dependency = AssetDatabase.GetDependencies(resPath).Where((xx) => xx != resPath && !xx.EndsWith(".cs") && !xx.EndsWith(".shader") && !xx.StartsWith("Assets/3rdParty/Spine")).ToList();
        bool hasReferency = false;
        foreach(var item in dependency)
        {
            if(!item.Contains(folderName))
            {
                hasReferency = true;
                break;
            }
            
        }
        StringBuilder sb = new();
        dependency.ForEach((xx) => sb.AppendLine(xx));
        Debug.Log("依赖：" + sb.ToString());
        if(hasReferency)
        {
            Debug.Log("引用不为空");
        }
    }
    /// <summary>
    /// 获得命名空间
    /// </summary> 
    /// <param name="filePath 文件全路径 或者unity资源路径"></param>
    /// <returns></returns>
    public static  string GetNameSpaceName(string filePath)
    {
        filePath = GetLinuxPath(filePath);
        string folderName = "";
        if (filePath.Contains("/"))
        {
            folderName = Path.GetDirectoryName(filePath);
        }
        else
        {
            folderName = filePath;
        }
        
        var item = EasyUseEditorFuns.GetUnityAssetPath(folderName, false);
        var tmpNameSpace = item.Replace("/", ".");
        if (tmpNameSpace.Contains("hot_fix"))
        {
            tmpNameSpace = tmpNameSpace.Replace("hot_fix", "HotFix");
        }
        var nameSpaceArray = tmpNameSpace.Split('.');
        if(nameSpaceArray.Length > 0)
        {
            for (int i = 0; i < nameSpaceArray.Length; i++)
            {
                if (nameSpaceArray[i].Length == 0)
                {
                    Debug.LogError("异常" + filePath);
                    continue;
                }
                if (char.IsLower(nameSpaceArray[i][0]))
                {
                    nameSpaceArray[i] = CapitalizeFirstLetter(nameSpaceArray[i]);
                }

            }
        }
        else
        {
            tmpNameSpace  = CapitalizeFirstLetter(tmpNameSpace);
        }
        

        tmpNameSpace = string.Join(".", nameSpaceArray);

        if (tmpNameSpace.Contains("Hotfix"))
        {
            tmpNameSpace = tmpNameSpace.Replace("Hotfix", "HotFix");
        }

        string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return char.ToUpper(input[0]) + input.Substring(1);
        }

        return tmpNameSpace;

    }
    /// <summary>
    /// 判断一个类是否具有命名空间 
    /// </summary>
    /// <param name="content cs代码文本 "></param>
    /// <returns></returns>
    public static bool HasNamespaceDeclaration(string content)
    {
        return Regex.IsMatch(content, @"\bnamespace\s+\w+");
    }
    /// <summary>
    /// 获取全路径 
    /// </summary>
    /// <param name="unityPath"></param>
    /// <returns></returns>
    public static  string GetFullPath(string unityPath)
    {
        unityPath = GetLinuxPath(unityPath);
        if(unityPath.StartsWith("Assets/"))
        {
            return GetLinuxPath(Environment.CurrentDirectory + "/" + unityPath);
        }
        else
        {
            return GetLinuxPath(Environment.CurrentDirectory + "/Assets/" + unityPath);
        }
    }


    /// <summary>
    /// 获取cs文件的文件类名
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    public static string ExtractClassName(string content)
    {
        // 匹配类、结构体或接口定义
        Match match = Regex.Match(content, @"(class|struct|interface)\s+([^\s:{]+)");
        return match.Success ? match.Groups[2].Value.Trim() : null;
    }
    /// <summary>
    /// 找到类的定义位置 
    /// </summary>
    /// <param name="content"></param>
    /// <param name="className"></param>
    /// <returns></returns>
    public static int FindClassDefinitionPosition(string content, string className)
    {
        // 查找类定义行
        int pos = content.IndexOf($"class {className}") + $"class {className}".Length;
        if (pos < 0) pos = content.IndexOf($"struct {className}") + $"struct {className}".Length;
        if (pos < 0) pos = content.IndexOf($"interface {className}") + $"interface {className}".Length;

        // 找到类定义行的开头
        while (pos >= 0 && content[pos] != '\n')
        {
            pos--;
        }

        return pos + 1;
    }

    /// <summary>
    /// 删除无效的文件夹 
    /// </summary>
    /// <param name="rootPath"></param>
    public static void CleanEmptyDirectories(string rootPath)
    {
        if (!Directory.Exists(rootPath))
        {
            Console.WriteLine($"目录不存在: {rootPath}");
            return;
        }

        try
        {
            // 1. 先递归处理所有子目录
            foreach (string subDir in Directory.GetDirectories(rootPath))
            {
                CleanEmptyDirectories(subDir);
            }

            // 2. 检查当前目录是否为空
            if (IsDirectoryEmpty(rootPath))
            {
                try
                {
                    if(File.Exists(rootPath+".meta"))
                    {
                        File.Delete(rootPath + ".meta");
                    }
                    Directory.Delete(rootPath);
                    Console.WriteLine($"已删除空目录: {rootPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"删除目录失败 {rootPath}: {ex.Message}");
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine($"无权限访问目录: {rootPath}");
        }
        catch (DirectoryNotFoundException)
        {
            Console.WriteLine($"目录不存在: {rootPath}");
        }
    }
    private static bool IsDirectoryEmpty(string path)
    {
        try
        {
            return Directory.GetFiles(path).Length == 0 &&
                   Directory.GetDirectories(path).Length == 0;
        }
        catch
        {
            return false;
        }
    }

}


