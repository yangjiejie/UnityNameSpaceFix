using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.Animations;
using System.IO;



using System.Text.RegularExpressions;
using UnityEngine.Windows;

using System.Linq;




public class RightBtnMenu
{
    [MenuItem("CONTEXT/Text/获取格式化文本")]
    static void GetTextFormat(MenuCommand cmd)
    {
        var tmp = cmd.context as Text;
        var content = tmp.text;
        var array = content.Split("\n");
        var list = new List<string>();
        list.AddRange(array);
        GUIUtility.systemCopyBuffer = string.Join(",\r\n", list).ToString();
    }
    static MonoScript FindMonoScriptOfType(System.Type type)
    {
        foreach (var script in MonoImporter.GetAllRuntimeMonoScripts())
        {
            if (script.GetClass() == type)
            {
                return script;
            }
        }
        return null;
    }

    [MenuItem("CONTEXT/Transform/获取世界坐标")]
    static void PrintTransPos(MenuCommand cmd)
    {
        var trans = cmd.context as Transform;
        Debug.Log("trans.position = " + trans.position);

        var screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main,trans.position);
        //屏幕坐标转ui坐标 

        Camera.main.ScreenToWorldPoint(screenPoint);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(trans as RectTransform,screenPoint,Camera.main,out Vector2 test);

        Debug.Log("test = " + test);
    }
    [MenuItem("GameObject/右键菜单/打印树结构", priority = 1)]
    static void PringTreeNode()
    {
        var go = Selection.activeObject as GameObject;
        //GMFunc.PringTreeNode(go);
    }
    [MenuItem("GameObject/右键菜单/item/item排序从大到小", priority = 1)]
    static void SortCreatedItem2()
    {
        var go = Selection.activeObject as GameObject;
        var nCount = go.transform.childCount;
        var firstNode = go.transform.GetChild(0);
        var baseName = Regex.Replace(firstNode.name, @"\d+", "");
        for (int i = nCount - 1; i >= 0 ; --i)
        {
            var subObj = go.transform.Find($"{baseName}{i+1}");
            subObj.SetSiblingIndex(nCount - i  - 1);
        }
        EditorUtility.SetDirty(go);
    }

    [MenuItem("GameObject/右键菜单/item/item排序从小到大", priority = 1)]
    static void SortCreatedItem()
    {
        var go = Selection.activeObject as GameObject;
        var nCount = go.transform.childCount;
        var firstNode = go.transform.GetChild(0);
        var baseName = Regex.Replace(firstNode.name, @"\d+","");
        for(int i = 0; i < nCount; i++)
        {
            var subObj = go.transform.Find($"{baseName}{i+1}");
            subObj.SetSiblingIndex(i);
        }
        EditorUtility.SetDirty(go);
    }
    [MenuItem("GameObject/右键菜单/item/调整子结点命名", priority = 1)]
    static void ChangeChildItemName()
    {
        var go = Selection.activeObject as GameObject;
        var nCount = go.transform.childCount;
        for (int i = 0; i < nCount; i++)
        {
            go.transform.GetChild(i).name = go.name + $"_{i}";
        }
        EditorUtility.SetDirty(go);
    }


    [MenuItem("GameObject/右键菜单/item/以第一个子结点创建item", priority = 1)]
    static void ReCreateItem()
    {
        var go = Selection.activeObject as GameObject;
        var nCount = go.transform.childCount;
        List<Vector3> posList = new List<Vector3>();
        for (int i = 0; i < nCount; i++)
        {
            posList.Add(go.transform.GetChild(i).localPosition);
        }
        for (int i = go.transform.childCount - 1; i >= 1; --i)
        {
            GameObject.DestroyImmediate(go.transform.GetChild(i).gameObject);
        }
        var firstr = go.transform.GetChild(0);
        var baseName = Regex.Replace(firstr.name, @"\d+", "");

        MatchCollection matches = Regex.Matches(firstr.name, @"-?\d+");
        var baseIndex = int.Parse(matches[0].Value);

        for (int i = 1; i < posList.Count; i++)
        {
            var newGo = GameObject.Instantiate(firstr.gameObject);
            newGo.name = baseName + (baseIndex + i);
            newGo.transform.SetParent(firstr.parent, false);
            newGo.transform.localPosition = posList[i];
            
        }
        posList.Clear();
        EditorUtility.SetDirty(go);
    }

    [MenuItem("GameObject/右键菜单/item/反转10个item", priority = 1)]
    static void ReverseCreateItem10()
    {
        var go = Selection.activeObject as GameObject;
        var nCount = go.transform.childCount;
        for(int i = 0; i < nCount / 2 ; i++)
        {
            var tmp = go.transform.GetChild(i).name;
            go.transform.GetChild(i).name = go.transform.GetChild(nCount - 1 - i).name;
            go.transform.GetChild(nCount - 1 - i).name = tmp;
        }
        EditorUtility.SetDirty(go);
    }

    [MenuItem("GameObject/右键菜单/item/生成10个item(使用前2个gap负向)", priority = 1)]
    static void CreateItem10WithFixed12ReverseGap()
    {
        var go = Selection.activeObject as GameObject;
        var first = go.transform.GetChild(0);
        var second = go.transform.GetChild(1);
        var fixed12Gap = (second as RectTransform).anchoredPosition.y - (first as RectTransform).anchoredPosition.y;
        fixed12Gap = -Mathf.Abs(fixed12Gap);

        List<Vector3> posList = new List<Vector3>();
        for (int i = go.transform.childCount - 1; i >= 1; --i)
        {

            GameObject.DestroyImmediate(go.transform.GetChild(i).gameObject);
        }

        var item = go.transform.GetChild(0);
        var baseName = Regex.Replace(item.name, @"\d+", "");

        MatchCollection matches = Regex.Matches(item.name, @"-?\d+");
        var baseIndex = int.Parse(matches[0].Value);

        for (int i = 0; i < 9; i++)
        {
            var newGo = GameObject.Instantiate(item);
            newGo.name = $"{baseName}{baseIndex + i + 1}";
            newGo.transform.SetParent(go.transform, false);
            if (i == 0)
            {
                newGo.localPosition = first.localPosition + new Vector3(0, fixed12Gap, 0);
            }
            else
            {
                newGo.localPosition = posList[i - 1] + new Vector3(0, fixed12Gap, 0);
            }
            posList.Add(newGo.localPosition);
        }
        posList.Clear();
        EditorUtility.SetDirty(go);
    }

    [MenuItem("GameObject/右键菜单/item/生成10个item(使用前2个gap)", priority = 1)]
    static void CreateItem10WithFixed12Gap()
    {
        var go = Selection.activeObject as GameObject;
        var first = go.transform.GetChild(0);
        var second = go.transform.GetChild(1);
        var fixed12Gap = (second as RectTransform).anchoredPosition.y - (first as RectTransform).anchoredPosition.y;
        fixed12Gap = Mathf.Abs(fixed12Gap);

        List<Vector3> posList = new List<Vector3>();
        for (int i = go.transform.childCount - 1; i >= 1; --i)
        {
            
            GameObject.DestroyImmediate(go.transform.GetChild(i).gameObject);
        }

        var item = go.transform.GetChild(0);
        var baseName = Regex.Replace(item.name, @"\d+", "");

        MatchCollection matches = Regex.Matches(item.name, @"-?\d+");
        var baseIndex = int.Parse(matches[0].Value);

        for (int i = 0; i < 9; i++)
        {
            var newGo = GameObject.Instantiate(item);
            newGo.name = $"{baseName}{baseIndex  + i + 1}";
            newGo.transform.SetParent(go.transform, false);
            if(i == 0)
            {
                newGo.localPosition = first.localPosition + new Vector3(0, fixed12Gap, 0);
            }
            else
            {
                newGo.localPosition = posList[i - 1] + new Vector3(0, fixed12Gap, 0);
            }
            posList.Add(newGo.localPosition);
        }
        posList.Clear();
        EditorUtility.SetDirty(go);
    }

    [MenuItem("GameObject/右键菜单/item/生成X个item(保持各自的transform)", priority = 1)]
    static void CreateItemX()
    {
        var go = Selection.activeObject as GameObject;
        List<Vector3> posList = new List<Vector3>();
        List<Vector3> scaleList = new List<Vector3>();
        List<Vector3> rotateList = new List<Vector3>();
        int nCount = go.transform.childCount;
        for (int i = go.transform.childCount - 1; i >= 1; --i)
        {
            posList.Add( go.transform.GetChild(i).localPosition);
            scaleList.Add( go.transform.GetChild(i).localScale);
            rotateList.Add( go.transform.GetChild(i).localEulerAngles);
            GameObject.DestroyImmediate(go.transform.GetChild(i).gameObject);
        }
        posList.Reverse(); scaleList.Reverse();rotateList.Reverse();
        var item = go.transform.GetChild(0);
        var baseName = Regex.Replace(item.name, @"\d+", "");

        MatchCollection matches = Regex.Matches(item.name, @"-?\d+");
        var baseIndex = int.Parse(matches[0].Value);

        for (int i = 0; i < posList.Count; i++)
        {
            var newGo = GameObject.Instantiate(item);
            newGo.name = $"{baseName}{baseIndex + i + 1}";
            newGo.transform.SetParent(go.transform, false);
            newGo.localPosition = posList[i];
            newGo.localScale = scaleList[i];
            newGo.localEulerAngles = rotateList[i];
        }
        posList.Clear();
        scaleList.Clear();
        rotateList.Clear();
        EditorUtility.SetDirty(go);
    }
    [MenuItem("GameObject/右键菜单/item/生成10个item", priority = 1)]
    static void CreateItem10()
    {
        var go = Selection.activeObject as GameObject;
        List<Vector3> posList = new List<Vector3>();
        for(int i = go.transform.childCount- 1; i>= 1; --i)
        {
            posList[i - 1] = go.transform.GetChild(i).localPosition;
            GameObject.DestroyImmediate(go.transform.GetChild(i).gameObject);
        }
        var item = go.transform.GetChild(0);
        var baseName = Regex.Replace(item.name, @"\d+", "");

        MatchCollection matches = Regex.Matches(item.name, @"-?\d+");
        var baseIndex = int.Parse(matches[0].Value);

        for (int i = 0; i < 9; i++)
        {
            var newGo = GameObject.Instantiate(item);
            newGo.name = $"{baseName}{baseIndex + i + 1}";
            newGo.transform.SetParent(go.transform, false);
            newGo.localPosition = posList[i];
        }
        posList.Clear();
        EditorUtility.SetDirty(go);
    }

    [MenuItem("GameObject/右键菜单/计算距离", priority = 1)]
    static void CalDidtance()
    {
        var objs = Selection.objects;
        if (objs.Length != 2 || objs.Length <= 1)
        {
            return;
        }
        var pos1 = (objs[0] as GameObject).transform.position;
        var pos2 = (objs[1] as GameObject).transform.position;
        Debug.Log(Vector3.Distance(pos1, pos2));
    }

    
  

    [MenuItem("GameObject/右键菜单/计算方向差", priority = 1)]
    static void CalDir()
    {
        var objs = Selection.objects;
        if (objs.Length != 2 || objs.Length <= 1)
        {
            return;
        }
        var pos1 = (objs[0] as GameObject).transform.position;
        var pos2 = (objs[1] as GameObject).transform.position;
        Debug.Log(pos2 - pos1);
    }

    [MenuItem("GameObject/右键菜单/打开设计图目录", priority = 1)]
    static void OpenDesignUIFolder()
    {
        
        EditorUtility.RevealInFinder(Application.dataPath + "./RefTexture");
    }
    [MenuItem("Assets/右键工具/动画压缩导出", priority = 1)]
    static void ClipOptimalAndExport()
    {
        var assetPath = AssetDatabase.GetAssetPath(Selection.objects[0]);
        var clipPathRoot = EditorUtility.OpenFolderPanel("选择导出路径", System.IO.Path.GetDirectoryName(assetPath), "");
        var goes = Selection.objects;
        for (int i = 0; i < goes.Length; i++)
        {
            var go = goes[i] as GameObject;
            assetPath = AssetDatabase.GetAssetPath(go);
            var clipName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            if (clipName.Contains("@"))
            {
                clipName = clipName.Split("@")[1];
            }
            var modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            modelImporter.animationCompression = ModelImporterAnimationCompression.Optimal;
            modelImporter.SaveAndReimport();

          
            AnimationClip oldClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            AnimationClip newClip = new();
            EditorUtility.CopySerialized(oldClip, newClip);
            var clipPath = clipPathRoot.Replace(Application.dataPath, "Assets");
            AssetDatabase.CreateAsset(newClip, clipPath + "/" + clipName + ".anim");
            AssetDatabase.Refresh();
        }
    }

    [MenuItem("Assets/右键工具/输出clip", priority = 1)]
    static void DumpClipName()
    {
        var go = Selection.activeObject;
        AnimatorOverrideController control = go as AnimatorOverrideController;
        if(control != null)
        {
            var anim = (control.runtimeAnimatorController as AnimatorController);
            dumpClip(anim);
        }
        else
        {
            AnimatorController control2 = go as AnimatorController;
            dumpClip(control2);
        }       
    }

    [MenuItem("Assets/右键工具/获得clip帧数", priority = 1)]
    static void DumpClipFrame()
    {
        var go = Selection.activeObject;
        AnimationClip control = go as AnimationClip;

       
        
        Debug.Log("帧:" + control.length * control.frameRate + "动画长度"+ control.length);
    }
 
    
    static void dumpClip(AnimatorController anim)
    {
        AnimatorStateMachine stateMachine = (anim).layers[0].stateMachine;
        string[] animatorState = new string[stateMachine.states.Length];
        for (int i = 0; i < stateMachine.states.Length; i++)
        {
            animatorState[i] = stateMachine.states[i].state.name;
        }
        var outStr = string.Join(";", animatorState);
        GUIUtility.systemCopyBuffer = outStr;
        Debug.Log(outStr);
    }


    [MenuItem("GameObject/右键菜单/raycast优化", priority = 1)]
    static void ModifyUIImageAndTextRaycastAttribute()
    {
        
        UnityEngine.Object[] selectedObjs = Selection.GetFiltered(typeof(GameObject), SelectionMode.DeepAssets);
        for (int i = 0; i < selectedObjs.Length; i++)
        {
            GameObject go = selectedObjs[i] as GameObject;
            var imgs = go.GetComponentsInChildren<Image>(true);
            for (int j = 0; j < imgs.Length; j++)
            {
                Image img = imgs[j];
                if(img.gameObject.GetComponent<Button>() == null )
                    img.raycastTarget = false;  
                else
                {
                    img.raycastTarget = true;
                }
            }
            var texts = go.GetComponentsInChildren<Text>(true);
            for (int j = 0; j < texts.Length; j++)
            {
                Text text = texts[j];
                if (text.transform.parent != null)
                {
                    if (text.transform.parent.GetComponent<InputField>() == null)
                        text.raycastTarget = false;
                    else
                        text.raycastTarget = true;
                }
                else
                {
                    text.raycastTarget = false;
                }
            }
            Debug.Log(string.Format("已修改{0}预设Image和Text组建的Raycast", go.name));
            EditorUtility.SetDirty(go);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }


    [MenuItem("GameObject/拼ui", priority = -100)]
    static void CreateRowImage(MenuCommand cmd)
    {
        var size = GameViewTools.GameViewSize(); // 屏幕大小  
        GameObject go = cmd.context as GameObject;
        var dirPath = EditorPrefs.GetString("ui_path", "");

        var path = EditorUtility.OpenFilePanel("选择图片", dirPath, "*.*");
        if (string.IsNullOrEmpty(path)) return;
        EditorPrefs.SetString("ui_path", Path.GetDirectoryName(path));

        GameObject origin = null;
        if (go.transform.Find("origin") == null)
        {
            origin = new GameObject("origin");
        }
        else
        {
            origin = go.transform.Find("origin").gameObject;
        }
        if (origin.GetComponent<CanvasGroup>() == null)
        {
            origin.AddComponent<CanvasGroup>();
        }
        var canVasGroup = origin.GetComponent<CanvasGroup>();
        canVasGroup.alpha = 0.3f; //ViewTheImage是0.4 ui是0.3
        origin.transform.SetParent(go.transform, false);
        var originRect = origin.AddComponent<RectTransform>();
        InitRectTransform(originRect);


        



        void InitRectTransform(RectTransform tmpRect)
        {
            tmpRect.anchorMin = Vector2.zero;
            tmpRect.anchorMax = Vector2.one;
            tmpRect.pivot = new Vector2(0.5f, 0.5f);
            tmpRect.offsetMin = new Vector2(0, 0); // left 和 bottom
            tmpRect.offsetMax = new Vector2(-0, -0); // right 和 top
        }
        List<Transform> childs = new List<Transform>();
        for (int i = 0; i < go.transform.childCount; i++)
        {
            childs.Add(go.transform.GetChild(i));
        }
#if DEV_MODE
        
        if (go.transform.Find("DebugNode") == null)
        {
            var tmp  =  new GameObject("DebugNode");
            var rect  = tmp.AddComponent<RectTransform>();
            InitRectTransform(rect);
            childs.Add(rect.transform);
        }
        else
        {
            Transform debugNode = go.transform.Find("DebugNode");
            childs.Add(debugNode);
        }
        
#endif
        for (int i = 0; i < childs.Count; i++)
        {
            childs[i].SetParent(originRect.transform, false);
        }

        childs.Clear();

        var origin2 = GameObject.Instantiate(originRect, originRect.parent, false);
        origin2.name = "origin2";
        origin2.offsetMin = new Vector2(-size.x, 0);
        origin2.offsetMax = new Vector2(-size.x, 0);
        origin2.GetComponent<CanvasGroup>().alpha = 1.0f;
        var width = GameViewTools.devWidth;
        var height = GameViewTools.devHeight;
        width = GameViewTools.devWidth;
        height = GameViewTools.devHeight;
         

        FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        fs.Seek(0, SeekOrigin.Begin);

        byte[] bytes = new byte[fs.Length];
        fs.Read(bytes, 0, (int)fs.Length);
        fs.Close();
        fs.Dispose();
        fs = null;

        

        Texture2D texture = new Texture2D(width, height);
        texture.LoadImage(bytes);
        InitViewImage("ViewTheImage", 0.4f);
       
        InitViewImage("ViewTheImage2", 1f,new Vector2(size.x, 0),new Vector2(size.x, 0));
        void InitViewImage(string goName,float alpha,Vector2 offsetMin = default,Vector2 offsetMax = default)
        {
            var xx = go.transform.Find(goName);
            GameObject rowObj = null;
            if (xx != null)
            {
                rowObj = xx.gameObject;
            }

            RawImage row = null;
            if (null == rowObj)
            {
                rowObj = new GameObject(goName);
                rowObj.transform.SetParent(go.transform, false);
                row = rowObj.AddComponent<UnityEngine.UI.RawImage>();

            }
            if (row == null)
            {
                row = rowObj.GetComponent<UnityEngine.UI.RawImage>();
            }
            row.color = new Color(row.color.r, row.color.g, row.color.b, alpha);
            (row.transform as RectTransform).sizeDelta = new Vector2(width, height);
            rowObj.transform.SetAsFirstSibling();
            (rowObj.transform as RectTransform).anchorMin = new Vector2(0, 0);
            (rowObj.transform as RectTransform).anchorMax = new Vector2(1, 1);

            
            if(offsetMin != default)
            {
                (rowObj.transform as RectTransform).offsetMin = offsetMin;
            }
            else
            {
                (rowObj.transform as RectTransform).offsetMin = Vector2.zero;
            }
            if (offsetMax != default)
            {
                (rowObj.transform as RectTransform).offsetMax = offsetMax;
            }
            else
            {
                (rowObj.transform as RectTransform).offsetMax = Vector2.zero;
            }
            row.texture = texture;
            if (!rowObj.activeSelf)
            {
                rowObj.SetActive(true);
            }
        }

        
        
       
    }
    [MenuItem("CONTEXT/Transform/删除所有组件除了trans本身")]
    static void DeleteAllComButTransform(MenuCommand cmd)
    {
        
        var go = (cmd.context as Transform).gameObject;
        
        var coms = go.GetComponents<Component>();
        foreach (var one in coms)
        {
            if (one is Transform)
            {
                continue;
            }
            GameObject.DestroyImmediate(one);
        }
        EditorUtility.SetDirty(go);
    }

    [MenuItem("GameObject/CopyNodePath", priority = -100)]
    static void CopyNodePath(MenuCommand cmd)
    {
        var trans = (cmd.context as GameObject).transform;
        string path = trans.name;
        while (trans.parent && !trans.parent.name.Contains("UI Root"))
        {
            trans = trans.parent;
            path = trans.name + "/" + path;
        }
        TextEditor te = new TextEditor();
        te.text = path;
        te.SelectAll();
        te.Copy();
        Debug.Log(path);
    }
}

