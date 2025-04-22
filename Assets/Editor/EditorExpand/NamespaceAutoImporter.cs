using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Assets.Editor;
using UnityEditor;
using UnityEngine;

public class NamespaceAutoImporter : EditorWindow
{
    private static readonly Dictionary<string, string> UnityTypeToNamespace = new Dictionary<string, string>
    {
        // Unity 核心命名空间
        { "TestPackageA","HotFix.Module.MyTestModule"},
        { "TestPackageB","HotFix.Module.MyTestModuleB"},
        {"MonoBehaviour", "UnityEngine"},
        {"ScriptableObject", "UnityEngine"},
        {"GameObject", "UnityEngine"},
        {"Transform", "UnityEngine"},
        {"Vector2", "UnityEngine"},
        {"Vector3", "UnityEngine"},
        {"Vector4", "UnityEngine"},
        {"Quaternion", "UnityEngine"},
        {"Color", "UnityEngine"},
        {"Material", "UnityEngine"},
        {"Shader", "UnityEngine"},
        {"Texture", "UnityEngine"},
        {"Sprite", "UnityEngine"},
        {"AudioClip", "UnityEngine"},
        {"Animation", "UnityEngine"},
        {"Animator", "UnityEngine"},
        {"Rigidbody", "UnityEngine"},
        {"Collider", "UnityEngine"},
        {"Mesh", "UnityEngine"},
        {"Camera", "UnityEngine"},
        {"Light", "UnityEngine"},
        
        // Unity UI 命名空间
        {"Canvas", "UnityEngine"},
        {"RectTransform", "UnityEngine"},
        {"Image", "UnityEngine.UI"},
        {"Text", "UnityEngine.UI"},
        {"Button", "UnityEngine.UI"},
        {"Slider", "UnityEngine.UI"},
        {"ScrollRect", "UnityEngine.UI"},
        {"Toggle", "UnityEngine.UI"},
        {"InputField", "UnityEngine.UI"},
        {"Dropdown", "UnityEngine.UI"},
        
        // Unity 其他命名空间
        {"SceneManager", "UnityEngine.SceneManagement"},
        {"Physics", "UnityEngine"},
        {"Physics2D", "UnityEngine"},
        {"Time", "UnityEngine"},
        {"Debug", "UnityEngine"},
        {"Random", "UnityEngine"},
        {"Resources", "UnityEngine"},
        {"AssetBundle", "UnityEngine"},
        {"PlayerPrefs", "UnityEngine"},
        
        // UnityEditor 命名空间 (通常只在编辑器脚本中使用)
        {"Editor", "UnityEditor"},
        {"EditorWindow", "UnityEditor"},
        {"MenuItem", "UnityEditor"},
        {"SerializedProperty", "UnityEditor"},
        {"SerializedObject", "UnityEditor"},
    };

    [MenuItem("Tools/Code/高级命名空间修复")]
    public static void ShowWindow()
    {
        GetWindow<NamespaceAutoImporter>("高级命名空间修复");
    }

    private EditorDragFolderComponet dragCom = new EditorDragFolderComponet();
    private List<string> filePaths = new List<string>();
    private Vector2 scrollPosition;
    private bool showDetails = false;
    private readonly List<string> logs = new List<string>();

    private void OnGUI()
    {
        dragCom.OnGui();
        GUILayout.Label("高级命名空间修复工具", EditorStyles.boldLabel);

        if (GUILayout.Button("分析并修复命名空间"))
        {
            AnalyzeAndFixAllFiles();
        }

        EditorGUILayout.Space();

        showDetails = EditorGUILayout.Foldout(showDetails, "显示详细信息");
        if (showDetails && logs.Count > 0)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            foreach (var log in logs)
            {
                EditorGUILayout.LabelField(log);
            }
            EditorGUILayout.EndScrollView();
        }

        if (GUILayout.Button("清除日志"))
        {
            logs.Clear();
        }
    }

    private void AnalyzeAndFixAllFiles()
    {
        logs.Clear();
        filePaths = dragCom.GetAllFiles();

        if (filePaths.Count == 0)
        {
            logs.Add("没有找到任何C#脚本文件");
            return;
        }

        int fixedFiles = 0;
        foreach (var filePath in filePaths)
        {
            if (CleanupNamespacesInFile(filePath))
            {
                fixedFiles++;
            }
        }

        AssetDatabase.Refresh();
        logs.Insert(0, $"已完成! 共修复 {fixedFiles}/{filePaths.Count} 个文件");
    }

    private bool CleanupNamespacesInFile(string filePath)
    {
        try
        {
            string originalContent = File.ReadAllText(filePath);
            string newContent = originalContent;

            // 1. 获取所有using语句
            var usingStatements = GetUsingStatements(newContent);

            // 2. 分析代码中实际使用的类型
            var usedTypes = AnalyzeUsedTypes(newContent);

            // 3. 确定需要的命名空间
            var requiredNamespaces = DetermineRequiredNamespaces(usedTypes);

            // 4. 清理无效using语句
            newContent = RemoveUnusedUsings(newContent, usingStatements, requiredNamespaces);

            // 5. 添加缺失的using语句
            newContent = AddMissingUsings(newContent, requiredNamespaces);

            // 6. 修复完全限定名
            newContent = FixFullyQualifiedNames(newContent, requiredNamespaces);

            // 7. 格式化using区域
            newContent = FormatUsingRegion(newContent);

            if (newContent != originalContent)
            {
                File.WriteAllText(filePath, newContent);
                logs.Add($"已修复: {Path.GetFileName(filePath)}");
                return true;
            }
        }
        catch (Exception ex)
        {
            logs.Add($"处理文件 {Path.GetFileName(filePath)} 时出错: {ex.Message}");
        }

        return false;
    }

    private List<UsingStatement> GetUsingStatements(string content)
    {
        var usings = new List<UsingStatement>();
        var matches = Regex.Matches(content, @"^using\s+([^=;\s]+)(?:\s*=\s*([^;\s]+))?\s*;", RegexOptions.Multiline);

        foreach (Match match in matches)
        {
            usings.Add(new UsingStatement
            {
                FullText = match.Value,
                Namespace = match.Groups[1].Value,
                Alias = match.Groups[2].Success ? match.Groups[2].Value : null
            });
        }

        return usings;
    }

    private HashSet<string> AnalyzeUsedTypes(string content)
    {
        var usedTypes = new HashSet<string>();

        // 移除字符串和注释以避免误判
        string cleanContent = RemoveStringsAndComments(content);

        // 查找所有可能的类型引用
        var matches = Regex.Matches(cleanContent, @"(?<!\w)([A-Z][a-zA-Z0-9_]*)(?=\s*\w|\(|\)|\[|\]|,|;|\.\s*[a-z])");

        foreach (Match match in matches)
        {
            string typeName = match.Groups[1].Value;
            if (!IsCSharpKeyword(typeName) && !typeName.StartsWith("System."))
            {
                usedTypes.Add(typeName);
            }
        }

        return usedTypes;
    }

    private string RemoveStringsAndComments(string content)
    {
        // 移除字符串
        content = Regex.Replace(content, @"""[^""]*""", "\"\"");
        // 移除单行注释
        content = Regex.Replace(content, @"//.*$", "", RegexOptions.Multiline);
        // 移除多行注释
        content = Regex.Replace(content, @"/\*.*?\*/", "", RegexOptions.Singleline);

        return content;
    }

    private HashSet<string> DetermineRequiredNamespaces(HashSet<string> usedTypes)
    {
        var requiredNamespaces = new HashSet<string>();

        foreach (var type in usedTypes)
        {
            if (UnityTypeToNamespace.TryGetValue(type, out var ns))
            {
                requiredNamespaces.Add(ns);
            }
        }

        return requiredNamespaces;
    }

    private string RemoveUnusedUsings(string content, List<UsingStatement> existingUsings, HashSet<string> requiredNamespaces)
    {
        // 找出未使用的using语句
        var unusedUsings = existingUsings
            .Where(u => !requiredNamespaces.Contains(u.Namespace) &&
                       !requiredNamespaces.Any(rn => rn.StartsWith(u.Namespace + ".")))
            .ToList();

        // 移除未使用的using语句
        foreach (var unused in unusedUsings)
        {
            content = content.Replace(unused.FullText + "\n", "");
            content = content.Replace(unused.FullText, "");
            logs.Add($"移除未使用的命名空间: {unused.Namespace}");
        }

        return content;
    }

    private string AddMissingUsings(string content, HashSet<string> requiredNamespaces)
    {
        var existingUsings = GetUsingStatements(content);
        var existingNamespaces = existingUsings.Select(u => u.Namespace).ToHashSet();

        var missingNamespaces = requiredNamespaces
            .Where(ns => !existingNamespaces.Contains(ns) &&
                        !existingNamespaces.Any(en => ns.StartsWith(en + ".")))
            .OrderBy(ns => ns)
            .ToList();

        if (missingNamespaces.Count == 0)
            return content;

        // 构建新的using语句
        var newUsings = new StringBuilder();
        foreach (var ns in missingNamespaces)
        {
            newUsings.AppendLine($"using {ns};");
            logs.Add($"添加缺失的命名空间: {ns}");
        }

        // 找到插入点
        int insertPosition = GetUsingInsertPosition(content);
        return content.Insert(insertPosition, newUsings.ToString());
    }

    private string FixFullyQualifiedNames(string content, HashSet<string> availableNamespaces)
    {
        // 查找完全限定名
        var matches = Regex.Matches(content, @"([a-zA-Z0-9_]+\.)+[A-Z][a-zA-Z0-9_]+");

        foreach (Match match in matches.Reverse()) // 反向处理避免位置变化
        {
            string fullName = match.Value;
            string typeName = fullName.Split('.').Last();

            if (UnityTypeToNamespace.TryGetValue(typeName, out var ns) &&
                availableNamespaces.Contains(ns))
            {
                // 如果已经有对应的using语句，可以简化
                content = content.Substring(0, match.Index) + typeName + content.Substring(match.Index + match.Length);
                logs.Add($"简化完全限定名: {fullName} -> {typeName}");
            }
        }

        return content;
    }

    private string FormatUsingRegion(string content)
    {
        // 提取所有using语句
        var usings = GetUsingStatements(content);
        if (usings.Count == 0)
            return content;

        // 移除所有现有using语句
        string withoutUsings = Regex.Replace(content, @"^using\s+[^;]+;\s*[\r\n]*", "", RegexOptions.Multiline);

        // 按字母顺序排序
        var sortedUsings = usings
            .OrderBy(u => u.Namespace.StartsWith("Unity") ? 0 : 1)
            .ThenBy(u => u.Namespace)
            .Select(u => u.FullText.Trim());

        // 重新插入
        var sb = new StringBuilder();
        sb.AppendLine(string.Join("\n", sortedUsings));
        sb.AppendLine();
        sb.Append(withoutUsings);

        return sb.ToString();
    }

    private int GetUsingInsertPosition(string content)
    {
        // 在第一个using后插入，如果没有则在namespace前插入
        int firstUsing = content.IndexOf("using ");
        if (firstUsing >= 0)
        {
            int lastUsing = content.LastIndexOf("using ");
            int endLine = content.IndexOf('\n', lastUsing);
            return endLine + 1;
        }

        int namespacePos = content.IndexOf("namespace ");
        return namespacePos >= 0 ? namespacePos : 0;
    }

    private bool IsCSharpKeyword(string word)
    {
        var keywords = new HashSet<string>
     {
         "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char",
         "checked", "class", "const", "continue", "decimal", "default", "delegate",
         "do", "double", "else", "enum", "event", "explicit", "extern", "false",
         "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit",
         "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
         "new", "null", "object", "operator", "out", "override", "params", "private",
         "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
         "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
         "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
         "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
     };

        return keywords.Contains(word);
    }

    private class UsingStatement
    {
        public string FullText { get; set; }
        public string Namespace { get; set; }
        public string Alias { get; set; }
    }
}