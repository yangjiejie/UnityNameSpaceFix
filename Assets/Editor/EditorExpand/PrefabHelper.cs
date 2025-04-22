

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;


public class PrefabHelper 
{

    [InitializeOnLoadMethod]
    public static void init()
    {
        Debug.Log("PrefabHelper.init");
        PrefabStage.prefabStageOpened -= OnPrefabStateOpened;
        PrefabStage.prefabStageOpened += OnPrefabStateOpened;
        PrefabStage.prefabStageClosing -= OnPrefabStageClosing;
        PrefabStage.prefabStageClosing += OnPrefabStageClosing;
        
    }
    /// <summary>
    /// 确保打开ui进行编辑的时候是开发分辨率
    /// </summary>
    /// <param name="ps"></param>
    public static void OnPrefabStateOpened(PrefabStage ps)
    {
        var size = GameViewTools.GameViewSize();
        if (size.x != GameViewTools.devWidth || size.y != GameViewTools.devHeight)
        {
            GameViewTools.ChangeSolution(new Vector2(GameViewTools.devWidth, GameViewTools.devHeight));
        }

    }
    
    public static void OnPrefabStageClosing(PrefabStage ps)
    {
        var root = ps.prefabContentsRoot;
        

        var rawImage = root.transform.Find("ViewTheImage");
        if(rawImage != null)
        {
            GameObject.DestroyImmediate(rawImage.gameObject);
        }

        rawImage = root.transform.Find("ViewTheImage2");
        if (rawImage != null)
        {
            GameObject.DestroyImmediate(rawImage.gameObject);
        }

        var origin = root.transform.Find("origin");
        if (origin == null) return;

        List<Transform> childs = new List<Transform>();
        for (int i = 0; i < origin.childCount; i++)
        {
            childs.Add(origin.transform.GetChild(i));
        }
        for (int i = 0; i < childs.Count;i++)
        {
            childs[i].SetParent(root.transform, false);
        }
#if !DEV_MODE
        for (int i = 0; i < childs.Count; i++)
        {
            if (childs[i].name.ToLower() == "DebugNode".ToLower())
            {
                GameObject.DestroyImmediate(childs[i].gameObject);
                break;
            }
        }
#endif
        childs.Clear();
        GameObject.DestroyImmediate(origin.gameObject);
        origin = root.transform.Find("origin2");
        if(origin != null)
        {
            GameObject.DestroyImmediate(origin.gameObject);
        }
        
        // SCGTool.RemoveNoUseRayCast(root);
        Object prefabObj = PrefabUtility.SaveAsPrefabAsset(root, ps.assetPath,out bool success);
        if(success)
        {
            Debug.Log("apply sucess " + ps.assetPath);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
