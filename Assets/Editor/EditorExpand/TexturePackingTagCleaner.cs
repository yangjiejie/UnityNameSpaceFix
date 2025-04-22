using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class TexturePackingTagCleaner : EditorWindow
{
    [MenuItem("Tools/纹理Packing Tag清理工具")]
    public static void ShowWindow()
    {
        GetWindow<TexturePackingTagCleaner>("Packing Tag Cleaner");
    }

    private string infoMessage;
    private MessageType messageType;
    private Vector2 scrollPosition;
    private List<TextureInfo> textureInfos = new List<TextureInfo>();
    private int selectedIndex = -1;
    private string selectedFolder = "";
    private const string FolderSaveKey = "TexturePackingTagCleaner";


    class TextureInfo
    {
        public string assetPath;
        public int width;
        public int height;
        public string packingTag;
        public bool shouldClear;
    }

    void OnGUI()
    {
        GUILayout.Space(10);

        DrawHeader();

        //if (GUILayout.Button("Select Folder and Process Textures", GUILayout.Height(40)))
        {
            //    ProcessTextures();
        }

        GUILayout.Space(20);
        GUILayout.Label($"当前目录:{this.selectedFolder}", EditorStyles.boldLabel);

        GUILayout.Space(20);
        DrawTextureList();

        GUILayout.Space(10);
        if (string.IsNullOrEmpty(this.selectedFolder))
        {
            UpdateMessage("请先选择目录", MessageType.Info);
        }

        EditorGUILayout.HelpBox(infoMessage, messageType);
    }

    private void DrawHeader()
    {
        var defaultColor = GUI.backgroundColor;

        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUI.backgroundColor = Color.blue;
            DrawReloadButton();
            GUILayout.Space(10);

            GUI.backgroundColor = Color.green;
            DrawFolderButton();
            GUILayout.Space(10);

            GUI.backgroundColor = Color.red;
            DrawBulkClearButton();
            GUILayout.Space(10);

            GUILayout.FlexibleSpace();
        }

        GUI.backgroundColor = defaultColor;
    }

    void DrawFolderButton() {
        if (GUILayout.Button("选择目录", EditorStyles.toolbarButton))
        {
            //选择目录
            SelectFolder();
            ProcessTextures();
        }
    }

    void DrawReloadButton()
    {
        if (GUILayout.Button("刷新", EditorStyles.toolbarButton))
        {
            ProcessTextures();
        }
    }

    void SelectFolder() {
        selectedFolder = EditorUtility.OpenFolderPanel("选择目录", "Assets/Art", "");
        //PlayerPrefs.SetString(FolderSaveKey, selectedFolder);

    }

    void DrawTextureList()
    {
        if (textureInfos.Count == 0) return;

        GUILayout.Label("贴图列表:", EditorStyles.boldLabel);
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(400));

        for (int i = 0; i < textureInfos.Count; i++)
        {
            var info = textureInfos[i];

            GUILayout.BeginHorizontal();

            GUIStyle style = new GUIStyle(GUI.skin.label);
            if (i == selectedIndex)
            {
                style.normal.background = Texture2D.whiteTexture;
                style.normal.textColor = Color.blue;
            }

            //string status = info.shouldClear ? "[Clear]" : "[Keep]";
            string status = "";
            string packingTagStr = string.IsNullOrEmpty(info.packingTag) ? "" : $"packingTag={ info.packingTag}";
            if (GUILayout.Button($"{status} {Path.GetFileName(info.assetPath)} {info.width}x{info.height} {packingTagStr}", style))
            {
                selectedIndex = i;
                SelectTextureInProject(info.assetPath);
            }

            if (GUILayout.Button("清除", GUILayout.Width(80)))
            {
                ClearPackingTag(info);
                selectedIndex = i;
                SelectTextureInProject(info.assetPath);
            }

            GUILayout.EndHorizontal();

        }

        GUILayout.EndScrollView();
    }

    void SelectTextureInProject(string assetPath)
    {
        var obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
        if (obj != null)
        {
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }
    }

    void DrawBulkClearButton()
    {
        if (GUILayout.Button("一键清除", EditorStyles.toolbarButton))
        {
            if (textureInfos.Count == 0) return;
            if (EditorUtility.DisplayDialog("一键清除",
                "是否清除列表中所有的Packing Tags?",
                "Yes", "Cancel"))
            {
                int clearedCount = 0;
                foreach (var info in textureInfos)
                {
                    if (ClearPackingTag(info))
                    {
                        clearedCount++;
                    }
                }
                UpdateMessage($"Cleared packing tags for {clearedCount} textures", MessageType.Info);
            }
        }
    }

    bool ClearPackingTag(TextureInfo info)
    {
        if (string.IsNullOrEmpty(info.packingTag)) return false;

        TextureImporter importer = AssetImporter.GetAtPath(info.assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.spritePackingTag = "";
            importer.SaveAndReimport();
            info.packingTag = "";
            return true;
        }
        return false;
    }

    void ProcessTextures()
    {
        if(string.IsNullOrEmpty(selectedFolder))
        {
            return;
        }
        string relativePath = ConvertToRelativePath(selectedFolder);
        if (string.IsNullOrEmpty(relativePath))
        {
            UpdateMessage("Selected folder is not in the Assets directory", MessageType.Error);
            return;
        }

        var extensions = new[] { ".png", ".jpg", ".jpeg", ".tga", ".psd", ".bmp" };
        string[] allFiles = Directory.GetFiles(selectedFolder, "*.*", SearchOption.AllDirectories)
            .Where(f => extensions.Contains(Path.GetExtension(f).ToLower())).ToArray();

        textureInfos.Clear();
        int totalFiles = allFiles.Length;

        try
        {
            for (int i = 0; i < totalFiles; i++)
            {
                string filePath = allFiles[i];
                string assetPath = ConvertToAssetPath(filePath);

                if (string.IsNullOrEmpty(assetPath)) continue;

                EditorUtility.DisplayProgressBar("Processing Textures",
                    $"Checking: {Path.GetFileName(filePath)} ({i + 1}/{totalFiles})",
                    (float)i / totalFiles);

                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null) continue;

                importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                bool overSize = width > 512 || height > 512;
                if(!string.IsNullOrEmpty(importer.spritePackingTag) && overSize)
                {
                    textureInfos.Add(new TextureInfo
                    {
                        assetPath = assetPath,
                        width = width,
                        height = height,
                        packingTag = importer.spritePackingTag,
                        shouldClear = overSize
                    });
                }

                if (overSize && !string.IsNullOrEmpty(importer.spritePackingTag))
                {
                    //importer.spritePackingTag = "";
                    //importer.SaveAndReimport();
                }
            }

            UpdateMessage($"Processed {textureInfos.Count} textures", MessageType.Info);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }
    }

    private string ConvertToRelativePath(string fullPath)
    {
        string assetsPath = Application.dataPath;
        if (!fullPath.StartsWith(assetsPath))
            return null;
        return "Assets" + fullPath.Substring(assetsPath.Length);
    }

    private string ConvertToAssetPath(string fullPath)
    {
        string projectPath = Directory.GetParent(Application.dataPath).FullName.Replace('\\', '/'); ;
        if (!fullPath.StartsWith(projectPath))
            return null;
        return fullPath.Substring(projectPath.Length + 1).Replace('\\', '/');
    }

    private void UpdateMessage(string message, MessageType type)
    {
        infoMessage = message;
        messageType = type;
        Repaint();
    }
}