using UnityEditor;
using UnityEngine;
using System.IO;

public class EditorResReplaceByUuid 
{
   

    public static void ReplaceUUID(string resPath, string uuidA,string uuidB)
    {
        if (string.IsNullOrEmpty(uuidA) || string.IsNullOrEmpty(uuidB))
        {
            Debug.LogError("请指定 UUID A 和 UUID B！");
            return;
        }
        if (uuidA == uuidB)
        {
            Debug.LogError("不用替换uuidA == uuidB！");
            return;
        }
        //备份 
        var source = Path.Combine(System.Environment.CurrentDirectory, resPath);
        var target = Path.Combine(EasyUseEditorFuns.baseCustomTmpCache, resPath);
        EasyUseEditorFuns.UnitySaveCopyFile(source, target, withPathMetaFile: true,isShowLog:false);
       
        if (resPath.EndsWith(".prefab") || resPath.EndsWith(".mat"))
        {
            // 读取文件内容
            string fileContent = File.ReadAllText(resPath);

            // 检查是否包含 UUID A
            if (fileContent.Contains(uuidA))
            {
                // 替换 UUID A 为 UUID B
                fileContent = fileContent.Replace(uuidA, uuidB);

                //写回文件之前先备份

                // 写回文件
                File.WriteAllText(resPath, fileContent);

                Debug.Log($"替换成功：{resPath}");

            }
        }
        // 刷新 Unity 项目
        AssetDatabase.Refresh();
    }
}