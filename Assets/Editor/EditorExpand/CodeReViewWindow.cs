using System.Collections.Generic;
using System.Linq;
using UnityEditor;


public class CodeReViewWindow : EditorWindow
{
    public string[] searchFolder = new string[]
    {
        "hot_fix",
    };

    public List<string> allCsFiles = new();

    void OnEnable()
    {
        allCsFiles.Clear();
        allCsFiles =  AssetDatabase.FindAssets("t:script", searchFolder)
            .Select((wc)=>AssetDatabase.GUIDToAssetPath(wc))
            .Where((yj)=>yj.EndsWith(".cs"))
            .OrderBy(ryw=>ryw).ToList();
    }
    private void OnGUI()
    {
        
    }
}
