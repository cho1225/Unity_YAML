using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using UnityEditor;
namespace MergeYamlTree
{
    internal class MergeYamlTreeViewWindow : EditorWindow
    {
        [SerializeField]
        private TreeViewState _treeViewState;

        private MergeYamlTreeView _treeView;
        private SearchField _searchField;

        // 表示設定用のフィールド
        [SerializeField]
        private MergeYamlTreeDisplayNameOption _displayNameOption;

        [SerializeField]
        private bool _showObjectHeaderIcon;

        [SerializeField]
        private string _searchString;

        [SerializeField]
        private string[] _selecteds;


        /// <summary>
        /// メニューからツールウィンドウを開く
        /// </summary>
        [MenuItem("Tools/Merge Yaml Tree Viewer", false, 1200)]
        private static void Open() => GetWindow<MergeYamlTreeViewWindow>(nameof(MergeYamlTreeViewWindow));

        /// <summary>
        /// ウィンドウが有効になったら初期化実行
        /// </summary>
        private void OnEnable()
        {
            _treeViewState ??= new TreeViewState();
            _treeView = new MergeYamlTreeView(_treeViewState);
            _searchField = new SearchField();
            _searchField.downOrUpArrowKeyPressed += _treeView.SetFocusAndEnsureSelectedItem;
            ReloadTreeView();
            ExpandAllNodes();
        }

        /// <summary>
        /// 選択されたアセットに基づいてツリービューを更新する
        /// </summary>
        private void ReloadTreeView()
        {
            // 現在アセットが選択されているか
            if (_selecteds == null || _selecteds.Length == 0) return;

            int id = 1;
            var merged = Enumerable.Empty<MergeYamlTreeElement>();

            foreach (var selected in _selecteds)
            {
                var path = AssetDatabase.GUIDToAssetPath(selected);
                if (AssetDatabase.IsValidFolder(path) && File.Exists(path + ".meta")) path += ".meta";

                // ツリー要素を構築
                var (elements, nextId) = MergeYamlTreeUtil.BuildElements(id, path);
                merged = merged.Concat(elements);
                id = nextId;
            }

            // ここでツリーの要素を反映
            _treeView.SetElements(merged.ToArray());
        }

        private void Update() => Repaint();

        /// <summary>
        /// ウィンドウの描画処理。
        /// </summary>
        private void OnGUI()
        {
            // 選択されていないか変数の内容と選択内容が違う場合
            if (_selecteds == null || (Selection.assetGUIDs.Length > 0 && !_selecteds.SequenceEqual(Selection.assetGUIDs)))
            {
                // 変数の内容と選択内容を一致させる
                _selecteds = Selection.assetGUIDs;
                // ツリー要素のリロード
                ReloadTreeView();
                ExpandAllNodes();
            }

            EditorGUI.BeginChangeCheck();

            // ツールバー関係
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                var classIdToClassName = _displayNameOption.HasFlag(MergeYamlTreeDisplayNameOption.ClassIdToClassName);
                var guidToAssetPath = _displayNameOption.HasFlag(MergeYamlTreeDisplayNameOption.GuidToAssetName);

                _showObjectHeaderIcon = EditorGUILayout.ToggleLeft("Icon", _showObjectHeaderIcon, GUILayout.Width(50f));
                classIdToClassName = EditorGUILayout.ToggleLeft("ClassIdToName", classIdToClassName, GUILayout.Width(110f));
                guidToAssetPath = EditorGUILayout.ToggleLeft("GuidToName", guidToAssetPath);

                GUILayout.FlexibleSpace();

                _searchString = _searchField.OnToolbarGUI(_treeView.searchString);
                _treeView.searchString = _searchString;

                _displayNameOption = MergeYamlTreeDisplayNameOption.Default;
                if (classIdToClassName) _displayNameOption |= MergeYamlTreeDisplayNameOption.ClassIdToClassName;
                if (guidToAssetPath) _displayNameOption |= MergeYamlTreeDisplayNameOption.GuidToAssetName;
            }

            _treeView.ShowObjectHeaderIcon = _showObjectHeaderIcon;
            _treeView.DisplayNameOption = _displayNameOption;
            if (EditorGUI.EndChangeCheck()) _treeView.Reload();

            if (_treeView.IsInitialized)
            {
                var rect = EditorGUILayout.GetControlRect(false, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                _treeView.OnGUI(rect);
            }
        }

        private void ExpandAllNodes()
        {
            var root = _treeView.RootItem;
            if (root == null) return;
            ExpandRecursively(root);
        }

        private void ExpandRecursively(TreeViewItem item)
        {
            _treeView.SetExpanded(item.id, true);
            if (item.children == null) return;

            foreach (var child in item.children)
            {
                ExpandRecursively(child);
            }
        }
    }
}
