/** ********************************************************************************
* Texture Viewer
* @ 2019 RNGTM
***********************************************************************************/
namespace TextureTool
{
    using System.Linq;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.IMGUI.Controls;
    using UnityEngine;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.IO;
    using System.Text;

    /** ********************************************************************************
     * @summary テクスチャツール用TreeView
     ***********************************************************************************/
    internal partial class TextureTreeView : TreeView
    {
        public static readonly string defaultSearchString = "HOGE";
        private static readonly TextAnchor fieldLabelAnchor = TextAnchor.MiddleLeft;

        private Texture2D prefabIconTexture = null; // Prefabアイコン
        private TextureTreeElement[] baseElements = new TextureTreeElement[0]; // TreeViewで描画する要素

        public bool IsInitialized => isInitialized;
        public bool IsEmpty => baseElements.Length == 0;
        public int ElementCount => baseElements.Length;

        /** ********************************************************************************
        * @summary コンストラクタ
        ***********************************************************************************/
        public TextureTreeView(TextureTreeViewState state, TextureColumnHeaderState headerState)
        : base(new TextureTreeViewState(), new TextureColumnHeader(headerState))
        {
            showAlternatingRowBackgrounds = true; // 背景のシマシマを表示
            showBorder = true; // 境界線を表示

            var textureColumnHeader = multiColumnHeader as TextureColumnHeader;
            textureColumnHeader.sortingChanged += OnSortingChanged; // ソート変化時の処理を登録
            textureColumnHeader.searchChanged += CallSearchChanged; // 列の検索が変化したときの処理を登録

            foreach (var searchField in headerState.SearchFields)
            {
                //searchField.searchChanged += () => CallSearchChanged("");
                searchField.searchChanged += CallSearchChanged;
            }
        }

        /// <summary>
        /// 绘制列  该方法由RowGUI 行绘制触发 
        /// </summary>
        /// <param name="args"></param>
        /// <param name="rect"></param>
        /// <param name="columnIndex"></param>
        private void DrawRowColumn(RowGUIArgs args, Rect rect, int columnIndex)
        {
            if (args.item.id < 0) { return; }  // 検索がヒットしない場合はid=-999のダミー(DummyTreeViewItem)が入ってくる。ここでは描画をスキップする

            TextureTreeElement element = baseElements[args.item.id];
            if (element == null) { return; }

            var texture = element.Texture;
            var textureImporter = element.TextureImporter;
            if (texture == null || textureImporter == null)
            {
                EditorGUI.LabelField(rect, "(null)");
                return;
            }

            //GUIStyle labelStyle = EditorStyles.label;
            GUIStyle labelStyle = EditorStyles.label;
            switch ((EHeaderColumnId)columnIndex)
            {
                case EHeaderColumnId.TextureName:
                    rect.x += 2f;
                    Rect toggleRect = rect;
                    toggleRect.y += 2f;
                    toggleRect.size = new Vector2(12f, 12f);
                    GUI.DrawTexture(toggleRect, texture);

                    // テキストを描画する
                    Rect labelRect = new Rect(rect);
                    labelRect.x += toggleRect.width;
                    EditorGUI.LabelField(labelRect, args.label);
                    break;
                default:
                    var text = element.GetDisplayText((EHeaderColumnId)columnIndex);
                    var style = element.GetLabelStyle((EHeaderColumnId)columnIndex);
                    EditorGUI.LabelField(rect, text, style);
                    break;
            }
        }

        /** ********************************************************************************
        * @summary 要素のクリア
        ***********************************************************************************/
        public void Clean()
        {
            baseElements = new TextureTreeElement[0];
            Reload();
        }

        /** ********************************************************************************
        * @summary キー入力イベント
        ***********************************************************************************/
        protected override void KeyEvent()
        {
            base.KeyEvent();

            var e = Event.current;
            // 按下回车键 用绑定的程序打开贴图 
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return) 
            {
                var selection = this.GetSelection();
                if (selection.Count == 0) { return; }

                int id = selection.ElementAt(0);
                if (id < 0) { return; }

               
                var path = baseElements[id].AssetPath;
                var prefab = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
                // 打开资源
                AssetDatabase.OpenAsset(prefab);
            }
        }

       /// <summary>
       /// 检测到选中事件 
       /// </summary>
       /// <param name="selectedIds"></param>
        protected override void SelectionChanged(IList<int> selectedIds)
        {
            base.SelectionChanged(selectedIds);
            if (selectedIds.Count == 0) { return; }

            //selectedIds = selectedIds.Distinct().ToArray();

            Object[] objects = new Object[selectedIds.Count];
            for (int i = 0; i < selectedIds.Count; i++)
            {
                int id = selectedIds.ElementAt(i);
                var path = baseElements[id].AssetPath;
                objects[i] = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
            }
            // 选中资源 
            Selection.objects = objects;
            // 高亮显示资源路径 
            EditorGUIUtility.PingObject(objects[objects.Length - 1]); 
        }

        /// <summary>
        ///  双击打开文件，代码还有一处是回车打开文件 
        /// </summary>
        /// <param name="id"></param>
        protected override void DoubleClickedItem(int id)
        {
            base.DoubleClickedItem(id);

            // 強調表示する
            var path = baseElements[id].AssetPath;
            var prefab = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
            AssetDatabase.OpenAsset(prefab); // Prefabを開く
        }

   
        public TextureTreeElement GetElement(int index)
        {
            return baseElements[index];
        }

        /** ********************************************************************************
        * @summary データサイズ更新
        ***********************************************************************************/
        public void UpdateDataSize()
        {
            foreach (var element in baseElements)
            {
                element.UpdateDataSize();
            }
        }

        /// <summary>
        /// 获取选中的元素 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TextureTreeElement> GetSelectionElement()
        {
            return GetSelection().Select(id => GetElement(id));
        }

        /** ********************************************************************************
        * @summary 検索にヒットするかどうか
        ***********************************************************************************/
        protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            // 列に入力された検索文字をつかって絞り込み
            var textureItem = item as TextureTreeViewItem;
            var textureHeaderState = this.multiColumnHeader.state as TextureColumnHeaderState;
            return textureItem.data.DoesItemMatchSearch(textureHeaderState.SearchStates);
        }

        /** ********************************************************************************
        * @summary 検索文字列が入力されているかどうか
        ***********************************************************************************/
        new bool hasSearch
        {
            get
            {
                var textureHeaderState = this.multiColumnHeader.state as TextureColumnHeaderState;
                for (int i = 0; i < ToolConfig.HeaderColumnNum; i++)
                {
                    if (!textureHeaderState.SearchStates[i].HasValue) { return true; }
                }
                return false;
            }
        }

        /** ********************************************************************************
        * @summary 列の作成
        * @note    BuildRows()で返されたIListを元にしてTreeView上で描画が実行されます。
        ***********************************************************************************/
        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            // 現在のRowsを取得
            var rows = GetRows() ?? new List<TreeViewItem>();
            rows.Clear();

            //var textureHeaderState = this.multiColumnHeader.state as TextureColumnHeaderState;
            //var columnSearchStrings = textureHeaderState.SearchStrings;

            //　TreeViewItemの親子関係を構築
            var elements = new List<TreeViewItem>();
            foreach (var baseElement in baseElements)
            {
                var baseItem = CreateTreeViewItem(baseElement) as TextureTreeViewItem;
                baseItem.data = baseElement; // 要素を紐づける

                // 検索にヒットする場合は追加
                if (DoesItemMatchSearch(baseItem, searchString))
                {
                    root.AddChild(baseItem);
                    rows.Add(baseItem); // 列に追加
                }
            }

            // 親子関係に基づいてDepthを自動設定するメソッド
            SetupDepthsFromParentsAndChildren(root);
            return rows;
        }


        /** ********************************************************************************
        * @summary ルートの作成
        ***********************************************************************************/
        protected override TreeViewItem BuildRoot()
        {
            // BuildRootではRootだけを返す
            return new TextureTreeViewItem { id = -1, depth = -1, displayName = "Root" };
        }

        /** ********************************************************************************
        * @summary 要素を作成
        ***********************************************************************************/
        private TreeViewItem CreateTreeViewItem(TextureTreeElement model)
        {
            return new TextureTreeViewItem { id = model.Index, displayName = model.AssetName };
        }
        public static string CalculateMD5(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    // 计算文件的 MD5 哈希值
                    byte[] hashBytes = md5.ComputeHash(stream);

                    // 将字节数组转换为十六进制字符串
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        sb.Append(hashBytes[i].ToString("x2")); // "x2" 表示两位小写十六进制
                    }

                    return sb.ToString();
                }
            }
        }
        /** ********************************************************************************
        * @summary TreeView初期化
        ***********************************************************************************/
        public void SetTexture(Texture2D[] textures, TextureImporter[] importers)
        {
            // TreeViewの要素を作成
            baseElements = new TextureTreeElement[textures.Length];
            for (int i = 0; i < baseElements.Length; i++)
            {
                var path = AssetDatabase.GetAssetPath(textures[i]);
                baseElements[i] = new TextureTreeElement
                {
                    AssetPath = path,
                    AssetName = System.IO.Path.GetFileNameWithoutExtension(path),
                    md5 = CalculateMD5(path),
                    Texture = textures[i],
                    TextureImporter = importers[i],
                };
            }

            for (int i = 0; i < baseElements.Length; i++)
            {
                var element = baseElements[i];
                element.Index = i;
                element.UpdateDataSize();
            }
        }

        /// <summary>
        /// 树形控件  绘制每一行的 GUI 界面
        /// </summary>
        /// <param name="args"></param>
        protected override void RowGUI(RowGUIArgs args)
        {
            if (prefabIconTexture == null)
            {
                // Prefabアイコンをロード
                prefabIconTexture = EditorGUIUtility.Load("Prefab Icon") as Texture2D;
            }

            // TreeView 各列の描画
            for (var visibleColumnIndex = 0; visibleColumnIndex < args.GetNumVisibleColumns(); visibleColumnIndex++)
            {
                var rect = args.GetCellRect(visibleColumnIndex);
                var columnIndex = args.GetColumn(visibleColumnIndex);
                var labelStye = args.selected ? EditorStyles.whiteLabel : EditorStyles.label;
                labelStye.alignment = fieldLabelAnchor;

                DrawRowColumn(args, rect, columnIndex);
            }
        }

        /** ********************************************************************************
         * @summary 検索文字列が変化したことをTreeViewに教える
         ***********************************************************************************/
        public void CallSearchChanged()
        {
            searchString = "";
            searchString = defaultSearchString;

            //Debug.Log("CallSearchChanged : searchString=" + searchString);

            //// TreeViewController m_TreeView
            //var m_TreeView = typeof(TreeView)
            //    .GetField("m_TreeView", BindingFlags.NonPublic | BindingFlags.Instance)
            //    .GetValue(this);
            //Debug.Log(m_TreeView);

            //// public ITreeViewDataSource data { get; set; }
            //var data = m_TreeView.GetType()
            //    .GetProperty("data", BindingFlags.Public | BindingFlags.Instance)
            //    .GetValue(m_TreeView);
            //Debug.Log(data);

            //// void OnSearchChanged();
            //MethodInfo OnSearchChanged = data.GetType()
            //    .GetMethod("OnSearchChanged", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod);
            //Debug.Log(OnSearchChanged);

            //OnSearchChanged.Invoke(data, new object[0]);

            //System.Action<string> searchChanged = m_TreeView.GetType()
            //    .GetField("searchChanged", BindingFlags.NonPublic | BindingFlags.Instance)
            //    .GetValue(m_TreeView) as System.Action<string>;
        }
    }
}