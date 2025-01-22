using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace MergeYamlTree
{
    internal class MergeYamlTreeView : TreeView
    {
        private MergeYamlTreeElement[] _elements;
        public bool IsInitialized => _elements != null;

        public MergeYamlTreeDisplayNameOption DisplayNameOption { get; set; }
        public bool ShowObjectHeaderIcon { get; set; }

        public TreeViewItem RootItem { get; private set; }

        public MergeYamlTreeView(TreeViewState treeViewState) : base(treeViewState)
        {
        }

        public void SetElements(MergeYamlTreeElement[] elements)
        {
            _elements = elements;
            RootItem = BuildRoot();
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            foreach (var baseElement in _elements)
            {
                var baseItem = new MergeYamlTreeViewItem(baseElement);
                root.AddChild(baseItem);
                AddChildrenRecursive(baseElement, baseItem);
            }

            SetupDepthsFromParentsAndChildren(root);
            return root;
        }

        private static void AddChildrenRecursive(MergeYamlTreeElement model, TreeViewItem item)
        {
            foreach (var childModel in model.Children)
            {
                var childItem = new MergeYamlTreeViewItem(childModel);
                item.AddChild(childItem);
                AddChildrenRecursive(childModel, childItem);
            }
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (!(args.item is MergeYamlTreeViewItem item))
            {
                base.RowGUI(args);
                return;
            }

            item.DisplayNameOption = DisplayNameOption;
            item.ShowIcon = ShowObjectHeaderIcon;

            base.RowGUI(args);
        }

        protected override void DoubleClickedItem(int id)
        {
            base.DoubleClickedItem(id);
            var first = FindRows(new List<int> { id }).FirstOrDefault() as MergeYamlTreeViewItem;
            if (first?.Data?.AssetPath == null) return;
            AssetDatabase.OpenAsset(AssetDatabase.LoadMainAssetAtPath(first.Data.AssetPath));
        }

        protected override void SingleClickedItem(int id)
        {
            base.SingleClickedItem(id);
            if (hasSearch) SetExpandedParent(FindRows(new[] { id }));
        }

        private void SetExpandedParent(IList<TreeViewItem> rows)
        {
            var expands = new List<int>(GetExpanded());
            for (int i = 0, count = rows.Count; i < count; i++)
            {
                var row = rows[i];
                var p = row;
                while (p.parent != null)
                {
                    expands.Add(p.parent.id);
                    p = p.parent;
                }
            }

            SetExpanded(expands);
        }

        protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            var assetYamlTreeViewItem = item as MergeYamlTreeViewItem;
            if (assetYamlTreeViewItem == null) return base.DoesItemMatchSearch(item, search);
            if (Hit(assetYamlTreeViewItem.Data.Name, search)) return true;
            if (Hit(assetYamlTreeViewItem.Data.Value, search)) return true;
            if (Hit(assetYamlTreeViewItem.Data.AssetPath, search)) return true;
            var headerObj = assetYamlTreeViewItem.Data as MergeYamlObjectHeaderElement;
            if (headerObj == null) return base.DoesItemMatchSearch(item, search);
            if (Hit(headerObj.ClassName, search)) return true;
            if (Hit(headerObj.ClassId.ToString(), search)) return true;
            return base.DoesItemMatchSearch(item, search);

            static bool Hit(string str, string search)
                => str != null && str.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        protected override void ContextClickedItem(int id)
        {
            var ev = Event.current;
            ev.Use();

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Copy Text"), false, () =>
            {
                var rows = FindRows(GetSelection());
                var sb = new StringBuilder();
                foreach (var treeViewItem in rows) sb.AppendLine(treeViewItem.displayName);
                if (sb.Length > 0) sb.Length -= 1;
                GUIUtility.systemCopyBuffer = sb.ToString();
            });
            menu.ShowAsContext();
        }
    }
}
