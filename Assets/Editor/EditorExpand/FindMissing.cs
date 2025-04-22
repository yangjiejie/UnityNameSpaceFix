using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;



public  class FindMissing
{
    // 查找项目中的丢失脚本
    public static  List<string> FindMissingScriptsInProject()
    {
        string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab");

        List<string> missingScripts = new List<string>();
        List<string> assetPaths = new();
        foreach (string prefabGuid in allPrefabs)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab != null)
            {
                Component[] components = prefab.GetComponentsInChildren<Component>(true);

                foreach (Component component in components)
                {
                    if (component == null)
                    {
                        missingScripts.Add($"Missing script found in prefab: <<{prefab.name}>> -- (Path: {prefabPath})");
                        assetPaths.Add(prefabPath);
                        break;
                    }
                }
            }
        }

        if (missingScripts.Count > 0)
        {
            Debug.LogError($"Found {missingScripts.Count} missing scripts in the project:");
            foreach (string log in missingScripts)
            {
                Debug.LogError(log);    
            }
        }
        else
        {
            Debug.Log("No missing scripts found in the project.");
        }
        return assetPaths;
    }


    // 清理项目中的丢失脚本
    public static  void CleanMissingScriptsInProject(List<string> missingPrefab)
    {
        if(missingPrefab.Count == 0)
        {
            Debug.LogError("请先查");
            return;
        }
        foreach (string prefabPath in missingPrefab)
        {
            EasyUseEditorFuns.UnitySaveCopyFile(
                Path.Combine(System.Environment.CurrentDirectory,prefabPath),
                Path.Combine(EasyUseEditorFuns.baseCustomTmpCache,prefabPath),
                true);
            var metaFilePath2 = Path.Combine(EasyUseEditorFuns.baseCustomTmpCache, prefabPath + ".path");
            // 用额外的txt文件记录该文件的路径 方便回退
            EasyUseEditorFuns.WriteFileToTargetPath(metaFilePath2, prefabPath);

        }
        int cleanedCount = 0;
        foreach (string prefabPath in missingPrefab)
        {
            
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab != null)
            {
                cleanedCount += CleanMissingScriptsOnGameObject(prefab,prefabPath);
                
            }
        }
        Debug.Log($"Cleaned {cleanedCount} missing scripts in the project.");
    }

    // 清理单个 GameObject 上的丢失脚本
    public static  int CleanMissingScriptsOnGameObject(GameObject objPrefab, string prefabPath)
    {
        return  CheckMissMonoBehavior(prefabPath);
    }

    static int CheckMissMonoBehavior(string path)
    {
        //先截取路径，使路径从ASSETS开始
        int index = path.IndexOf("Assets/", StringComparison.CurrentCultureIgnoreCase);
        string newPath = path.Substring(index);
        GameObject obj = AssetDatabase.LoadAssetAtPath(newPath, typeof(GameObject)) as GameObject;
        //实例化物体
        GameObject go = PrefabUtility.InstantiatePrefab(obj) as GameObject;
        //递归删除
        int num = 0;
        num += searchChild(go);
        // 将数据替换到asset

        PrefabUtility.SaveAsPrefabAsset(go, newPath);

        go.hideFlags = HideFlags.HideAndDontSave;
        //删除掉实例化的对象
        UnityEngine.Object.DestroyImmediate(go);
        return num;
    }

    //递归物体的子物体
    static int searchChild(GameObject gameObject)
    {
        int number = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
        if (gameObject.transform.childCount > 0)
        {
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                number += searchChild(gameObject.transform.GetChild(i).gameObject);
            }
        }
        return number;
    }


    // 获取 GameObject 的层级路径
    private string GetHierarchyPath(GameObject obj)
    {
        string path = obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = obj.name + "/" + path;
        }
        return path;
    }
}

