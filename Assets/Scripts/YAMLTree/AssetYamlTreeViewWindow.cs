// 引用：https://github.com/satanabe1/asset-yaml-tree-view

using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using UnityEditor;

namespace AssetYamlTree
{
    /// <summary>
    /// YAMLデータを階層的に表示するウィンドウ
    /// </summary>
    internal class AssetYamlTreeViewWindow : EditorWindow
    {
        [SerializeField]
        private TreeViewState _treeViewState;

        private AssetYamlTreeView _treeView;
        private SearchField _searchField;

        // 表示設定用のフィールド
        [SerializeField]
        private AssetYamlTreeDisplayNameOption _displayNameOption;

        [SerializeField]
        private bool _showObjectHeaderIcon;

        [SerializeField]
        private string _searchString;

        [SerializeField]
        private string[] _selecteds;


        /// <summary>
        /// メニューからツールウィンドウを開く
        /// </summary>
        [MenuItem("Tools/Asset Yaml Tree Viewer", false, 1200)]
        private static void Open() => GetWindow<AssetYamlTreeViewWindow>(nameof(AssetYamlTreeViewWindow));

        /// <summary>
        /// ウィンドウが有効になったら初期化実行
        /// </summary>
        private void OnEnable()
        {
            _treeViewState ??= new TreeViewState();
            _treeView = new AssetYamlTreeView(_treeViewState);
            _searchField = new SearchField();
            _searchField.downOrUpArrowKeyPressed += _treeView.SetFocusAndEnsureSelectedItem;
            ReloadTreeView();
        }

        /// <summary>
        /// 選択されたアセットに基づいてツリービューを更新する
        /// </summary>
        private void ReloadTreeView()
        {
            // 現在アセットが選択されているか
            if (_selecteds == null || _selecteds.Length == 0) return;

            int id = 1;
            var merged = Enumerable.Empty<AssetYamlTreeElement>();

            foreach (var selected in _selecteds)
            {
                var path = AssetDatabase.GUIDToAssetPath(selected);
                if (AssetDatabase.IsValidFolder(path) && File.Exists(path + ".meta")) path += ".meta";

                // ツリー要素を構築
                var (elements, nextId) = AssetYamlTreeUtil.BuildElements(id, path);
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
            if (_selecteds == null || (Selection.assetGUIDs.Length > 0 && !_selecteds.SequenceEqual(Selection.assetGUIDs)))
            {
                _selecteds = Selection.assetGUIDs;
                ReloadTreeView();
            }

            EditorGUI.BeginChangeCheck();

            // ツールバー関係
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                var classIdToClassName = _displayNameOption.HasFlag(AssetYamlTreeDisplayNameOption.ClassIdToClassName);
                var guidToAssetPath = _displayNameOption.HasFlag(AssetYamlTreeDisplayNameOption.GuidToAssetName);

                _showObjectHeaderIcon = EditorGUILayout.ToggleLeft("Icon", _showObjectHeaderIcon, GUILayout.Width(50f));
                classIdToClassName = EditorGUILayout.ToggleLeft("ClassIdToName", classIdToClassName, GUILayout.Width(110f));
                guidToAssetPath = EditorGUILayout.ToggleLeft("GuidToName", guidToAssetPath);

                GUILayout.FlexibleSpace();

                _searchString = _searchField.OnToolbarGUI(_treeView.searchString);
                _treeView.searchString = _searchString;

                _displayNameOption = AssetYamlTreeDisplayNameOption.Default;
                if (classIdToClassName) _displayNameOption |= AssetYamlTreeDisplayNameOption.ClassIdToClassName;
                if (guidToAssetPath) _displayNameOption |= AssetYamlTreeDisplayNameOption.GuidToAssetName;
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
    }
}
