
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]

public class SelectionTools
{
    static SelectionTools()
    {
        // 监听 Selection 变化事件
        Selection.selectionChanged += OnSelectionChanged;
    }

    private static void OnSelectionChanged()
    {
        if (!EditorPrefs.GetBool("SelectionTools", true))
        {
            return;
        }

        // 获取当前选中的资源
        var selectedObject = Selection.activeObject;
        if (selectedObject == null)
            return;

        // 获取资源的路径
        string path = AssetDatabase.GetAssetPath(selectedObject);
        //目录不要这种操作
        if (string.IsNullOrEmpty(path) || System.IO.Directory.Exists(path))
            return;

        // 获取资源所在的目录
        string folderPath = System.IO.Path.GetDirectoryName(path);
        var folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folderPath);
        if (folder != null)
        {
            // 将焦点切换到 Project 窗口
            //EditorUtility.FocusProjectWindow();
            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
            //unity编辑下模拟协程 
            EditorCoroutine.StartCoroutine(new EditorWaitForSeconds(0.00001f, () =>
            {
                Selection.activeObject = selectedObject;
                EditorGUIUtility.PingObject(selectedObject);
            }));
            // Ping 目录（高亮显示但不会选中它）

        }
    }
}
