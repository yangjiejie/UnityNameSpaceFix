using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using UnityEditor;
using UnityEngine;

public class NamespaceAutoAdder : EditorWindow
{
    private string targetNamespace = null;
   
    private Vector2 scrollPos;
    private int filesProcessed;
    private int filesModified;
    private int needAddNameSpaceFileCount = 0;
    private bool previewOnly = true;
    private string logContent = "";

    List<string> targetDirectories  = new List<string>();

    [MenuItem("Tools/Code/Add Namespace Automatically")]
    public static void ShowWindow()
    {
        GetWindow<NamespaceAutoAdder>("自动添加命名空间");
    }
    private void DrawDragDropArea()
    {
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 40.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, targetDirectories.Count == 0 ?
            "拖拽文件夹到这里 (支持多选)" : $"已选择 {targetDirectories.Count} 个目录", EditorStyles.helpBox);

        HandleDragAndDrop(dropArea);
    }

    private void HandleDragAndDrop(Rect dropArea)
    {
        Event evt = Event.current;

        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    if (DragAndDrop.paths.Length > 0)
                    {
                        foreach (string path in DragAndDrop.paths)
                        {
                            if (Directory.Exists(path) && !targetDirectories.Contains(path))
                            {
                                targetDirectories.Add(path);
                            }
                            else if (File.Exists(path))
                            {
                                string dir = Path.GetDirectoryName(path);
                                if (!targetDirectories.Contains(dir))
                                {
                                    targetDirectories.Add(dir);
                                }
                            }
                        }
                    }
                }
                break;
        }
    }
    private void DrawDirectoriesList()
    {
        if (targetDirectories.Count > 0)
        {
            EditorGUILayout.LabelField("已选目录:", EditorStyles.boldLabel);

            for (int i = 0; i < targetDirectories.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                // 目录路径
                EditorGUILayout.LabelField(targetDirectories[i]);

                // 移除按钮
                if (GUILayout.Button("×", GUILayout.Width(20)))
                {
                    targetDirectories.RemoveAt(i);
                    return; // 避免在循环中修改集合
                }

                EditorGUILayout.EndHorizontal();
            }
        }
    }

    private Texture2D folderIcon;
    private Texture2D fileIcon;

    private string searchFilter = "*.txt";
    private bool showHiddenFiles;

    private bool IsHidden(string path)
    {
        FileAttributes attr = File.GetAttributes(path);
        return (attr & FileAttributes.Hidden) == FileAttributes.Hidden;
    }
    private void DrawMergedContents()
    {
        try
        {
            // 合并所有目录的文件和子目录
            var allFiles = new List<string>();
            var allDirectories = new List<string>();

            foreach (var dir in targetDirectories)
            {
                if (!Directory.Exists(dir)) continue;

                allFiles.AddRange(Directory.GetFiles(dir, searchFilter, SearchOption.AllDirectories)
                    .Where(f => (showHiddenFiles || !IsHidden(f))));

                allFiles = allFiles.Where((ll) => !Regex.IsMatch(ll, @"\\ctrl\\") && !Regex.IsMatch(ll, @"\\ctr\\")).ToList();


                allDirectories.AddRange(Directory.GetDirectories(dir)
                    .Where(d => showHiddenFiles || !IsHidden(d)));
            }

            // 去重并排序
            var uniqueDirs = allDirectories.Distinct().OrderBy(d => d).ToArray();
            var uniqueFiles = allFiles.Distinct().OrderBy(f => f).ToArray();

            // 显示子目录
            foreach (string dir in uniqueDirs)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(folderIcon, GUILayout.Width(20), GUILayout.Height(20));
                EditorGUILayout.LabelField(Path.GetFileName(dir));
                EditorGUILayout.LabelField($"({Path.GetDirectoryName(dir)})", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            // 显示文件
            foreach (string file in uniqueFiles)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(fileIcon, GUILayout.Width(20), GUILayout.Height(20));
                EditorGUILayout.LabelField(Path.GetFileName(file));
                EditorGUILayout.LabelField($"({Path.GetDirectoryName(file)})", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
        }
        catch (System.Exception e)
        {
            EditorGUILayout.HelpBox($"无法读取目录: {e.Message}", MessageType.Error);
        }
    }

    private void DrawDirectoryContents()
    {
        if (targetDirectories.Count == 0) return;

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        DrawMergedContents();



        EditorGUILayout.EndScrollView();
    }

    private void OnGUI()
    {
        GUILayout.Label("命名空间自动添加工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 配置区域
        DrawDragDropArea();

        DrawDirectoriesList();
        DrawDirectoryContents();
        EditorGUILayout.Space();
        previewOnly = EditorGUILayout.Toggle("仅预览(不修改)", previewOnly);

        EditorGUILayout.Space();
        if (GUILayout.Button("设置命名空间"))
        {
            ScanAndAddNamespaces();
        }
        if(GUILayout.Button("修复报错"))
        {
            FixError(); 
        }
        if (GUILayout.Button("跳转到代码行"))
        {
            var final = Application.dataPath + $"/Editor/EditorExpand/NamespaceAutoAdder.cs";
            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(final, 10);
        }

        // 结果显示
        EditorGUILayout.Space();
        GUILayout.Label($"结果: 处理 {filesProcessed} 个文件, 修改 {filesModified} 个文件");

        // 日志显示
        EditorGUILayout.Space();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        EditorGUILayout.TextArea(logContent, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }
    private static void FixSingleScriptNamespace(string fullPath)
    {
        string content = File.ReadAllText(fullPath);

        // 计算正确的命名空间（基于目录结构）
        string relativePath = fullPath.Replace(Application.dataPath, "Assets");
        string directoryPath = Path.GetDirectoryName(relativePath);

        // 移除"Assets/"开头，转换路径分隔符为点
        string correctNamespace = directoryPath
            .Substring("Assets/".Length)
            .Replace('/', '.')
            .Replace('\\', '.');

        // 处理可能的特殊目录（如 Editor、Plugins 等）
        correctNamespace = correctNamespace
            .Replace(".Editor", "")
            .Replace(".Plugins", "");

        // 使用正则表达式替换命名空间
        string pattern = @"namespace\s+([^\s{]+)";
        string replacement = $"namespace {correctNamespace}";

        string newContent = Regex.Replace(content, pattern, replacement);

        // 只有当内容确实改变时才写入文件
        if (newContent != content)
        {
            File.WriteAllText(fullPath, newContent);
        }
    }

    private static void FixSingleScript(string scriptPath)
    {
        string content = File.ReadAllText(scriptPath);
        string newContent = content;

        // 1. 修正命名空间声明
        string correctNamespace = EasyUseEditorFuns.GetNameSpaceName(scriptPath);
        newContent = Regex.Replace(newContent,
            @"namespace\s+([^\s{]+)",
            $"namespace {correctNamespace}");

        // 2. 替换所有using语句
        foreach (var mapping in namespaceMapping)
        {
            newContent = newContent.Replace(
                $"using {mapping.Key};",
                $"using {mapping.Value};");
        }

        // 3. 替换完全限定类型名
        foreach (var mapping in namespaceMapping)
        {
            // 替换 TypeName 形式的引用
            newContent = Regex.Replace(newContent,
                $@"\b{mapping.Key}\.(\w+)",
                $"{mapping.Value}.$1");

            // 替换 typeof(TypeName) 形式的引用
            newContent = Regex.Replace(newContent,
                $@"typeof\(\s*{mapping.Key}\.(\w+)\s*\)",
                $"typeof({mapping.Value}.$1)");
        }

        // 4. 处理特殊情况
        newContent = FixSpecialCases(newContent);

        if (newContent != content)
        {
            File.WriteAllText(scriptPath, newContent);
        }
    }

    private static string FixSpecialCases(string content)
    {
        // 处理 [AddComponentMenu] 特性
        content = Regex.Replace(content,
            @"\[AddComponentMenu\("".*?\/([^""]+)""\)\]",
            match =>
            {
                string typeName = match.Groups[1].Value;
                foreach (var mapping in namespaceMapping)
                {
                    if (typeName.StartsWith(mapping.Key + "."))
                    {
                        return match.Value.Replace(
                            $"{mapping.Key}.",
                            $"{mapping.Value}.");
                    }
                }
                return match.Value;
            });

        return content;
    }

    void FixError()
    {
        var allCsFiles =  GetAllCSFiles();
        BuildNamespaceMapping(allCsFiles.ToArray());
        // 3. 修复所有脚本
        foreach (var scriptPath in allCsFiles)
        {
            FixSingleScript(scriptPath);
        }


    }
    private static Dictionary<string, string> namespaceMapping = new Dictionary<string, string>();
    private static void BuildNamespaceMapping(string[] scriptPaths)
    {
        namespaceMapping.Clear();

        foreach (var path in scriptPaths)
        {
            string content = File.ReadAllText(path);
            string className = ExtractClassName(content);

            if (!string.IsNullOrEmpty(className))
            {
                string newNamespace = EasyUseEditorFuns.GetNameSpaceName(path);
                namespaceMapping[className] = newNamespace;
            }
        }
    }

    private static string ExtractClassName(string content)
    {
        // 匹配类声明，处理普通类、抽象类、静态类、分部类等
        var match = Regex.Match(content,
            @"(?:public\s+|private\s+|internal\s+|protected\s+)?(?:abstract\s+|static\s+|sealed\s+|partial\s+)*class\s+([A-Za-z_][A-Za-z0-9_]*)");

        return match.Success ? match.Groups[1].Value : null;
    }

    private static string CalculateCorrectNamespace(string fullPath)
    {
        string relativePath = fullPath.Replace(Application.dataPath, "Assets");
        string directory = Path.GetDirectoryName(relativePath)?
            .Replace("Assets/", "")
            .Replace("/", ".");

        // 处理特殊文件夹
        if (directory.Contains("Editor"))
        {
            directory = directory.Replace(".Editor", "") + ".Editor";
        }

        // 处理插件目录
        if (directory.Contains("Plugins"))
        {
            directory = directory.Replace(".Plugins", "") + ".Plugins";
        }

        return directory;
    }

    private static string ExtractCurrentNamespace(string content)
    {
        var match = Regex.Match(content, @"namespace\s+([^\s{]+)");
        return match.Success ? match.Groups[1].Value : null;
    }

    List<string> GetAllCSFiles()
    {

        var allFiles = new List<string>();
        

        foreach (var dir in targetDirectories)
        {
            if (!Directory.Exists(dir)) continue;

            allFiles.AddRange(Directory.GetFiles(dir, searchFilter, SearchOption.AllDirectories)
                .Where(f => (showHiddenFiles || !IsHidden(f))));

            allFiles = allFiles.Where((ll) => !Regex.IsMatch(ll, @"\\ctrl\\") && !Regex.IsMatch(ll, @"\\ctr\\")).ToList();


        }

    
        var uniqueFiles = allFiles.Distinct().OrderBy(f => f).ToList();
        return uniqueFiles;
    }
    private void ScanAndAddNamespaces()
    {
        filesProcessed = 0;
        filesModified = 0;
        needAddNameSpaceFileCount = 0;
        logContent = "";



        var scriptPaths = GetAllCSFiles();



        foreach (string scriptPath in scriptPaths)
        {
            if (ProcessScript(scriptPath))
            {
                filesModified++;
            }
            filesProcessed++;

            // 更新进度显示
            if (filesProcessed % 10 == 0)
            {
                EditorUtility.DisplayProgressBar("处理中...",
                    $"已扫描 {filesProcessed}/{scriptPaths.Count} 个文件",
                    (float)filesProcessed / scriptPaths.Count);
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();

        logContent += $"\n处理完成! 共处理 {filesProcessed} 个文件, 修改 {filesModified} 个文件，{needAddNameSpaceFileCount}个文件没有命名空间";
    }

    private bool ProcessScript(string scriptPath)
    {
        string content = File.ReadAllText(scriptPath);
        string originalContent = content;

        // 检查是否已有命名空间
        if (HasNamespaceDeclaration(content))
        {
            logContent += $"\n跳过: {GetRelativePath(scriptPath)} (已有命名空间)";
            return false;
        }
        needAddNameSpaceFileCount++;

        // 获取类名
        string className = ExtractClassName(content);
        if (string.IsNullOrEmpty(className))
        {
            logContent += $"\n警告: {GetRelativePath(scriptPath)} (未找到有效类定义)";
            return false;
        }

        targetNamespace = EasyUseEditorFuns.GetNameSpaceName(scriptPath);
        // 构建新的命名空间声明
        string namespaceDeclaration = $"\nnamespace {targetNamespace}\n{{\n";
        string closingBrace = "\n}\n";

        // 在类定义前插入命名空间
        int classPos = FindClassDefinitionPosition(content, className);
        if (classPos < 0)
        {
            logContent += $"\n错误: {GetRelativePath(scriptPath)} (无法定位类定义)";
            return false;
        }

        content = content.Insert(classPos, namespaceDeclaration);
        content += closingBrace;

        // 如果是预览模式，不实际修改文件
        if (!previewOnly)
        {
            content = content.Replace("\r\n", "\n");
            content = content.Replace("\n", "\r\n");
            File.WriteAllText(scriptPath, content);
            logContent += $"\n修改: {GetRelativePath(scriptPath)} -> {targetNamespace}.{className}";
            return true;
        }
        else
        {
            logContent += $"\n预览: {GetRelativePath(scriptPath)} 将添加命名空间 {targetNamespace}";
            return false;
        }
    }

    private bool HasNamespaceDeclaration(string content)
    {
        return Regex.IsMatch(content, @"\bnamespace\s+\w+");
    }



    private int FindClassDefinitionPosition(string content, string className)
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

    private string GetRelativePath(string fullPath)
    {
        return fullPath.Replace(Path.GetFullPath(Application.dataPath), "Assets");
    }
}