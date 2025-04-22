
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
    public class EditorDragFolderComponet
    {
        public List<string> targetDirectories { get; set; } = new();

        private string searchFilter = "*.txt";
        private bool showHiddenFiles;

        private UnityEngine.Vector2 scrollPos;
        private Texture2D folderIcon;
        private Texture2D fileIcon;

        public static EditorDragFolderComponet Create()
        {
            return new EditorDragFolderComponet();
        }

        public List<string> GetAllFiles()
        {
            var allFiles = new List<string>();
            

            foreach (var dir in targetDirectories)
            {
                if (!Directory.Exists(dir)) continue;

                allFiles.AddRange(Directory.GetFiles(dir, searchFilter, SearchOption.AllDirectories)
                    .Where(f => (showHiddenFiles || !IsHidden(f))));

                allFiles = allFiles.Where((ll) => !Regex.IsMatch(ll, @"\\ctrl\\") && !Regex.IsMatch(ll, @"\\ctr\\")).ToList();


                
            }

            // 去重并排序
      
            var uniqueFiles = allFiles.Distinct().OrderBy(f => f).ToList();
            return uniqueFiles;
        }

        public void OnGui()
        {
            DrawDragDropArea();
            DrawDirectoriesList();
            DrawDirectoryContents();}

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
    }
}
