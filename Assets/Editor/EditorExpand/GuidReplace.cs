using System;
using System.Collections.Generic;

using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Codice.Client.Common.TreeGrouper;
using JetBrains.Annotations;
using Unity.Plastic.Newtonsoft.Json;

using UnityEditor;
using UnityEngine;

public   class GuidReplace
{
    public CodeMoveTool condeMovetool;
    public GuidReplace(CodeMoveTool condeMovetool)
    {
        this.condeMovetool = condeMovetool;
    }

    public class GuidNode
    {
        public string originFilePathName;
        public string originGuid;
        public List<GuidNode> dependecy;
    }
    public Dictionary<string,GuidNode> lastGuidNodeCache = new Dictionary<string,GuidNode>();
    public Dictionary<string,GuidNode> nowGuidNodeCache = new Dictionary<string,GuidNode>();
    GuidNode GetGuidNode(string guid,bool isNow = false)
    {
        var cacheNode = isNow ? nowGuidNodeCache : lastGuidNodeCache;
        if (cacheNode.TryGetValue(guid,out GuidNode tmp))
        {
            return tmp;
        }
        var path = AssetDatabase.GUIDToAssetPath(guid).ToLinuxPath();
       
        if(path.Contains("com.unity.ugui"))
        {
            return null;
        }

        var node = new GuidNode();
        node.originGuid = guid;
        node.originFilePathName = AssetDatabase.GUIDToAssetPath(guid).ToLinuxPath();
        cacheNode.Add(node.originGuid, node);
        List<string> arr = null; 
        if (node.originFilePathName.EndsWith(".asmdef"))
        {
            var content = File.ReadAllText(node.originFilePathName);

            string pattern = "\"GUID:([a-f0-9]+)\"";
            var matches = Regex.Matches(content, pattern);
             arr = matches.Cast<Match>().Select(m => m.Groups[1].Value).ToList();

            
        }
        else
        {
            if(isNow)
            {
                if(File.Exists(node.originFilePathName))
                {
                    var content = File.ReadAllText(node.originFilePathName);
                    var matches = Regex.Matches(content, @"guid:\s*([0-9a-fA-F]{32})");
                    arr = new List<string>();
                    foreach (Match match in matches)
                    {
                        string tmpGuid = match.Groups[1].Value;
                        arr.Add(tmpGuid);
                    }
                }
               
            }
            else
            {
                arr = AssetDatabase.GetDependencies(node.originFilePathName).Where((xx) => xx != node.originFilePathName).Select((ll) => AssetDatabase.AssetPathToGUID(ll)).ToList();
            }
            
            
        }

        for (int i = 0; arr != null && i < arr.Count; i++)
        {
            if (node.dependecy == null)
            {
                node.dependecy = new();
            }
            var subNode = GetGuidNode(arr[i]);
            if (subNode != null)
            {
                node.dependecy.Add(subNode);
            }

        }


        return node;
    }

    public void CreateMapByJson()
    {
        var content = File.ReadAllText(Application.dataPath + "/Editor/~config/allRes.json");
        lastGuidNodeCache = JsonConvert.DeserializeObject<Dictionary<string, GuidNode>>(content);
    }

    public void BuildMap()
    {
        lastGuidNodeCache.Clear();
        List<GuidNode> list = new();
        var allScenes = AssetDatabase.FindAssets("t:scene", new string[] { "Assets" });
        foreach (var guid in allScenes)
        {
            GetGuidNode(guid);
        }
        var allPrefabs = AssetDatabase.FindAssets("t:prefab", new string[] { "Assets" });
        foreach (var guid in allPrefabs)
        {
            GetGuidNode(guid);
        }

        var allMats = AssetDatabase.FindAssets("t:material", new string[] { "Assets" });
        foreach (var guid in allMats)
        {
            GetGuidNode(guid);
        }
        var allFont = AssetDatabase.FindAssets("t:font", new string[] { "Assets" });
        foreach (var guid in allFont)
        {
            GetGuidNode(guid);
        }
        var allAsmdefs = AssetDatabase.FindAssets("t:asmdef", new string[] { "Assets" });
        

        foreach(var guid in allAsmdefs)
        {
            GetGuidNode(guid);

        }


        SaveJson();

    }
    bool HasRes(string resPath,out GuidNode node)
    {
        var originPath = this.condeMovetool.ConvertPathWithExclusions(resPath, true);
        var rst = lastGuidNodeCache.Values.FirstOrDefault((xx) => xx.originFilePathName == originPath);
        if (rst == null || rst == default)
        {
            node = null;
            return false;
        }
        node = rst;
        return true;
    }
    public void Replace(List<string> allGuids)
    {
        nowGuidNodeCache.Clear();
   
        foreach(var guid     in allGuids)
        {
            GetGuidNode(guid, true);
        }

        //遍历所有现有文件 现有文件的meta不要变 要改prefab文件本身 

        foreach (var item in lastGuidNodeCache.Values)
        {
            var res = item.originFilePathName;
            //遍历所有依赖 
            if(item.dependecy != null && item.dependecy.Count > 0)
            {
                for (global::System.Int32 j = 0; j < item.dependecy.Count; j++)
                {
                    var subResNode = item.dependecy[j];
                    var oldRes = subResNode.originFilePathName;
                    var nowRes =  this.condeMovetool.ConvertPathWithExclusions(oldRes, false);

                    var rst = nowGuidNodeCache.FirstOrDefault((xx) => xx.Value.originFilePathName == nowRes);
                    if(rst.Value != null)
                    {
                        if (res.EndsWith(".prefab") || res.EndsWith(".asmdef") || res.EndsWith(".material") ||
                        res.EndsWith(".font"))
                        {
                            var content = File.ReadAllText(res);
                            
                            var newContent = Regex.Replace(content, subResNode.originGuid, rst.Value.originGuid);
                            File.WriteAllText(res, newContent);
                        }
                    }

                    


                    
                    
                   
                }
            }
            
        }
        AssetDatabase.Refresh();    
       
    }

    void SaveJson()
    {
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
        var str = JsonConvert.SerializeObject(lastGuidNodeCache, settings);
        EasyUseEditorFuns.CreateDir(Application.dataPath + "/Editor/~config");
        File.WriteAllText(Application.dataPath + "/Editor/~config/allRes.json", str);
    }
}
