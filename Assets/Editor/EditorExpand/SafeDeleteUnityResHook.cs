using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;
public class SafeDeleteUnityResHook : AssetModificationProcessor
{
    // 当资源被删除时调用此方法
    public static bool forbidHook;
    public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
    {
        if(forbidHook || Regex.IsMatch(assetPath, @"/StreamingAssets/"))
        {
            return AssetDeleteResult.DidNotDelete;
        }
        Debug.Log($"即将删除资源: {assetPath}");

        // 这里可以添加备份资源等自定义逻辑
        // 例如，将被删除的资源复制到备份目录
       
        if (File.Exists(assetPath))
        {
            var unityResPathName = assetPath.Substring(assetPath.IndexOf("Assets/"));
            string backupFilePath = Path.Combine(EasyUseEditorFuns.baseCustomTmpCache, unityResPathName);
            backupFilePath = EasyUseEditorFuns.GetLinuxPath(backupFilePath);
            EasyUseEditorFuns.UnitySaveCopyFile(assetPath, backupFilePath);
            Debug.Log($"文件 {assetPath} 已备份到 {backupFilePath}。");
            var metaFilePath = Path.Combine(EasyUseEditorFuns.baseCustomTmpCache, unityResPathName + ".path");
            // 用额外的txt文件记录该文件的路径 方便回退
            EasyUseEditorFuns.WriteFileToTargetPath(metaFilePath, unityResPathName);
        }

        // 返回 AssetDeleteResult.DidNotDelete 表示不阻止删除操作
        return AssetDeleteResult.DidNotDelete;
    }
}