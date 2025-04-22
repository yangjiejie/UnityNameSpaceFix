#if DEV_MODE

using System;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

//自动处理项目中的纹理导入属性 
public class TextureProcessor : AssetPostprocessor
{
    static bool IsPowerOfTwo(int value)
    {
        return (value & (value - 1)) == 0;
    }

    static int GetNextPowerOfTwo(int value)
    {
        int power = 1;
        while (power < value)
        {
            power *= 2;
        }
        return power;
    }

    //c# 7.0 元组返回多参数 
    static (int, int) GetWidthAndHeight(TextureImporter textureImporter)
    {
        object[] args = new object[2] { 0, 0 };
        MethodInfo mi = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance);
        mi.Invoke(textureImporter, args);

        return ((int)args[0], (int)args[1]);
    }

    static string pattern = @"/image/";
    static string pattern2 = @"/icon/";

    public static bool DealTexture(string assetPath, AssetImporter assetImporter)
    {
        return false;

        if (Application.isPlaying) return false;
        if (!assetPath.EndsWith(".png") && !assetPath.EndsWith(".jpg"))
        {
            return false;
        }


        if (!assetPath.StartsWith("Assets/Art"))
        {
            return false;
        }
        //if(!assetPath.StartsWith("Assets/Art/gameCommon") && !assetPath.StartsWith("Assets/Art/gameFruit"))
        //{
        //    return false;
        //}

        bool isImage = Regex.IsMatch(assetPath, pattern);
        bool isIcon = Regex.IsMatch(assetPath, pattern2);

        if (Regex.IsMatch(assetPath, pattern) || Regex.IsMatch(assetPath, pattern2))
        {
        }
        else
        {
            return false;
        }


        bool needImport = false;
        TextureImporter textureImporter = (TextureImporter)assetImporter;



        object[] args = new object[2] { 0, 0 };
        var (width, height) = GetWidthAndHeight(textureImporter);



        if (textureImporter.mipmapEnabled)
        {
            Debug.LogError("警告!mipmapEnabled == true,已自动修正" + assetPath);
            textureImporter.mipmapEnabled = false;
            needImport = true;
        }

        if (textureImporter.filterMode != FilterMode.Bilinear)
        {
            textureImporter.filterMode = FilterMode.Bilinear;
            needImport = true;
        }

        /*
        // 找到最近的 /icon/ 文件夹
        string[] pathParts = assetPath.Split('/');
        string packingTag = "";
        if (isIcon)
        {
            for (int i = 0; i < pathParts.Length; i++)
            {
                if (pathParts[i] == "icon")
                {
                    // 获取 /icon/ 的上层文件夹名称
                    if (i > 0)
                    {
                        packingTag = pathParts[i - 1];
                        break;
                    }
                }
            }
            textureImporter.spritePackingTag = $"{packingTag.ToLower()}_icon";
        }
        else if(isImage)
        {
            for (int i = 0; i < pathParts.Length; i++)
            {
                if (pathParts[i] == "image")
                {
                    // 获取 /icon/ 的上层文件夹名称
                    if (i > 0)
                    {
                        packingTag = pathParts[i - 1];
                        break;
                    }
                }
            }
            textureImporter.spritePackingTag = packingTag.ToLower();
        }
       */





        //有一个问题就是textureImporter的纹理类型 可能做ui的话多数会是gui 但是不能排除其他的 这里不方便直接指定

        if (textureImporter.isReadable)
        {
            Debug.LogError("警告!isReadable == true,已自动修正" + assetPath);
            textureImporter.isReadable = false;
            needImport = true;
        }


        var androidSetting = textureImporter.GetPlatformTextureSettings("android");
        if (!androidSetting.overridden)
        {
            androidSetting.overridden = true;
            needImport = true;
        }
        //如果纹理大小不是2的幂级或者纹理设置的最大大小和纹理本身不匹配 那么获取一个最接近的匹配大小并设置
        if (!IsPowerOfTwo(width) || !IsPowerOfTwo(height) ||
            androidSetting.maxTextureSize != GetNextPowerOfTwo(width) ||
             androidSetting.maxTextureSize != GetNextPowerOfTwo(height))
        {
            width = GetNextPowerOfTwo(width);
            height = GetNextPowerOfTwo(height);
            androidSetting.maxTextureSize = Math.Max(width, height);//根据最近的2幂来判断 先放这里
            needImport = true;
        }


        if (androidSetting.format != TextureImporterFormat.ASTC_6x6)
        {
            androidSetting.format = TextureImporterFormat.ASTC_6x6;
            androidSetting.overridden = true;


            needImport = true;
        }

        if (needImport)
        {
            textureImporter.SetPlatformTextureSettings(androidSetting);
            // 重新导入纹理
            //AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            return true;
        }
        return false;
    }

    void OnPreprocessTexture()
    {
        DealTexture(assetPath, assetImporter);

    }
}

#endif