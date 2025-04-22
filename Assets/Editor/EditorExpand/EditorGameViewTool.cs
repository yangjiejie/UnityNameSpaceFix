using System;

using System.Reflection;
using UnityEditor;
using UnityEngine;

public class GameViewTools
{
    public enum GameViewSizeType
    {
        AspectRatio,
        FixedResolution
    }
    public static int devWidth = 1125;
    public static int devHeight = 2436;
    private static object gameViewSizesInstance; // GameViewSizes的引用
    private static bool isInited = false;
    public static int  landScapeIndex = 1;
    public static int portraitIndex = 1;
    private static MethodInfo getGroup; 
    public static object curGroup;
    public static MethodInfo curGroupFunc;
    public static MethodInfo getGameViewSizeFunc; // 获取索引x的分辨率
    private static MethodInfo addCustomSize; //添加方法
    private static MethodInfo removeCustomSize; //添加方法
    private static MethodInfo getCustomCount; //引用获取当前自定义数量
    private static MethodInfo getTotakCount; //获取所以的数量
    private static MethodInfo getBuiltinCount; //获取基础数量

    public static bool IsHorizonal()
    {
        var vec = GameViewSize();
        return vec.x > vec.y;
    }
    public  static Vector2 GameViewSize()
    {
        var mouseOverWindow = UnityEditor.EditorWindow.mouseOverWindow;
        System.Reflection.Assembly assembly = typeof(UnityEditor.EditorWindow).Assembly;
        System.Type type = assembly.GetType("UnityEditor.PlayModeView");

        Vector2 size = (Vector2)type.GetMethod(
            "GetMainPlayModeViewTargetSize",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Static
        ).Invoke(mouseOverWindow, null);

        return size;
    }




    public static void ChangeResolution(int index)
{
    var type = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
    var window = EditorWindow.GetWindow(type);

      

    var SizeSelectionCallback = type.GetMethod("SizeSelectionCallback",
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
    SizeSelectionCallback.Invoke(window, new object[] { index, null });
}



public static void ChangeSolution(Vector2 size)
{
    GameViewTools.InitiaLized();    
    GameViewTools.CheckCustomSizeEveryOnce(size);
    GameViewTools.ChangeResolution(size.x < size.y ? GameViewTools.landScapeIndex : GameViewTools.portraitIndex);
}

public static void InitiaLized()
{
    if (isInited) return;
    isInited = true;
    var gvSize = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
    var singleType = typeof(ScriptableSingleton<>).MakeGenericType(gvSize);
    var instacnceProp = singleType.GetProperty("instance");
    getGroup = gvSize.GetMethod("GetGroup");
    curGroupFunc = gvSize.GetMethod("get_currentGroup");
    curGroup = curGroupFunc.Invoke(instacnceProp.GetValue(null, null), null);       
    gameViewSizesInstance = instacnceProp.GetValue(null, null); //获取单例
    getGameViewSizeFunc = curGroup.GetType().GetMethod("GetGameViewSize");
    getCustomCount = getGroup.ReturnType.GetMethod("GetCustomCount");
    getTotakCount = getGroup.ReturnType.GetMethod("GetTotalCount");
    getBuiltinCount = getGroup.ReturnType.GetMethod("GetBuiltinCount");
    addCustomSize = getGroup.ReturnType.GetMethod("AddCustomSize");
    removeCustomSize = getGroup.ReturnType.GetMethod("RemoveCustomSize");
    CheckCustomSizeEveryOnce();
}
    public static void CheckCustomSizeEveryOnce(Vector2 vector2 = default)
    {
        var buildInCount = (int)getBuiltinCount.Invoke(curGroup, null);//内建分辨率数量
        int count = (int)getCustomCount.Invoke(curGroup, null);  //自定义分辨率数量
        int totalCount = (int)getTotakCount.Invoke(curGroup, null); //累计分辨率数量 
        landScapeIndex = 0;
        portraitIndex = 0;
        int tmpDevHeight = 0;
        int tmpDevWidth = 0;
        if (vector2 == default)
        {
            tmpDevHeight = devHeight;
            tmpDevWidth = devWidth;
        }
        else
        {
            tmpDevWidth = (int)vector2.x;
            tmpDevHeight = (int)vector2.y;

        }
        for (int i = buildInCount; (landScapeIndex == 0 || portraitIndex == 0) && i < totalCount; i++)
        {
            var viewSize = getGameViewSizeFunc.Invoke(curGroup, new object[] { i });
            var w = (int)viewSize.GetType().GetProperty("width").GetValue(viewSize, null);
            var h = (int)viewSize.GetType().GetProperty("height").GetValue(viewSize, null);
            if (w == tmpDevWidth && h == tmpDevHeight)
            {
                landScapeIndex = i;
            }
            else if (w == tmpDevHeight && h == tmpDevWidth)
            {
                portraitIndex = i;
            }
        }
        if (landScapeIndex == 0 || portraitIndex == 0)
        {
            AddCustomSize(GameViewSizeType.FixedResolution, GameViewSizeGroupType.Android, devWidth, devHeight, "(code added)");
            AddCustomSize(GameViewSizeType.FixedResolution, GameViewSizeGroupType.Android, devHeight, devWidth, "(code added)");
        }
    }

    public static void AddCustomSize(GameViewSizeType viewSizeType, GameViewSizeGroupType gameViewSizeGroupType,
        int width, int height, string text)
    {
           
        //获取类GameViewSize
        var gvsType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");

        //找到 构造方法 public GameViewSize(GameViewSizeType type, int width, int height, string baseText)
        var ctor = gvsType.GetConstructor(new Type[]
        {
            typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType"),
            typeof(int),
            typeof(int),
            typeof(string)
        });

        //找到enum 类型 GameViewSizeType
        var newGvsType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType");

        int enumGvsType = 0;

        // //获取enum中的类型
        if (viewSizeType == GameViewSizeType.AspectRatio)
        {
            var aspectRatio = newGvsType.GetField("AspectRatio", BindingFlags.Static | BindingFlags.Public);
            //newGvsType =aspectRatio.GetType() ; //typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType.AspectRatio");
            enumGvsType = (int)aspectRatio.GetValue(null);
        }
        else
        {
            var fixedResolution = newGvsType.GetField("FixedResolution", BindingFlags.Static | BindingFlags.Public);
            // newGvsType = fixedResolution.GetType(); //typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType.FixedResolution");
            enumGvsType = (int)fixedResolution.GetValue(null);
        }

        var newSize = ctor.Invoke(new object[] { enumGvsType, width, height, text });


        // //执行 添加到 group 中 GameViewSizeType m_SizeType;
        var sizetype = gvsType.GetField("m_SizeType", BindingFlags.NonPublic | BindingFlags.Instance);

        //获取 GameViewSizeGroup
        var gameViewSizes = getGroup.Invoke(gameViewSizesInstance, new object[] { (int)gameViewSizeGroupType });

        addCustomSize.Invoke(gameViewSizes, new object[] { newSize });
    }
}
