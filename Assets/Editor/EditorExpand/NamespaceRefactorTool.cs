using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
//对整个cs代码库修改命名空间
public class NamespaceRefactorTool : EditorWindow
{
    private string oldNamespace = "Old.Namespace";
    private string newNamespace = "New.Namespace";
    private Vector2 scrollPos;
    private int filesProcessed;
    private int filesModified;
    private bool includeSubfolders = true;
    private bool previewOnly = true;

    [MenuItem("Tools/Code/Namespace Refactor Tool")]
    public static void ShowWindow()
    {
        GetWindow<NamespaceRefactorTool>("命名空间重构工具");
    }

    private void OnGUI()
    {
        GUILayout.Label("命名空间批量修改", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 配置区域
        oldNamespace = EditorGUILayout.TextField("原命名空间:", oldNamespace);
        newNamespace = EditorGUILayout.TextField("新命名空间:", newNamespace);

        EditorGUILayout.Space();
        includeSubfolders = EditorGUILayout.Toggle("包含子文件夹", includeSubfolders);
        previewOnly = EditorGUILayout.Toggle("仅预览(不修改)", previewOnly);

        EditorGUILayout.Space();
        if (GUILayout.Button("分析项目"))
        {
            AnalyzeProject();
        }

        // 结果显示
        EditorGUILayout.Space();
        GUILayout.Label($"结果: 处理 {filesProcessed} 个文件, 修改 {filesModified} 个文件");

        if (filesModified > 0 && !previewOnly)
        {
            EditorGUILayout.HelpBox("命名空间修改完成! 请重新编译项目。", MessageType.Info);
        }
    }

    private void AnalyzeProject()
    {
        filesProcessed = 0;
        filesModified = 0;

        string[] scriptPaths = Directory.GetFiles(Application.dataPath, "*.cs",
            includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

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
                    $"已处理 {filesProcessed}/{scriptPaths.Length} 个文件",
                    (float)filesProcessed / scriptPaths.Length);
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
    }

    private bool ProcessScript(string scriptPath)
    {
        string content = File.ReadAllText(scriptPath);
        string originalContent = content;

        // 1. 修改命名空间声明
        content = UpdateNamespaceDeclaration(content);

        // 2. 更新using语句
        content = UpdateUsingStatements(content);

        // 3. 更新完全限定名
        content = UpdateFullQualifiedNames(content);

        // 如果内容有变化且不是预览模式，则保存
        if (content != originalContent && !previewOnly)
        {
            File.WriteAllText(scriptPath, content);
            return true;
        }

        return false;
    }

    private string UpdateNamespaceDeclaration(string content)
    {
        // 匹配命名空间声明
        string pattern = $@"\bnamespace\s+{Regex.Escape(oldNamespace)}\b";
        return Regex.Replace(content, pattern, $"namespace {newNamespace}");
    }

    private string UpdateUsingStatements(string content)
    {
        // 匹配using语句
        string pattern = $@"^using\s+{Regex.Escape(oldNamespace)}\s*;(?:\r?\n)?";
        return Regex.Replace(content, pattern, $"using {newNamespace};\n", RegexOptions.Multiline);
    }

    private string UpdateFullQualifiedNames(string content)
    {
        // 匹配完全限定名
        string pattern = $@"\b{Regex.Escape(oldNamespace)}\.(\w+)";
        return Regex.Replace(content, pattern, $"{newNamespace}.$1");
    }
}