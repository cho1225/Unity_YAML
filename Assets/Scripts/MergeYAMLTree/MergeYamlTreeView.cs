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
            // 見やすくするため
            this.rowHeight = EditorGUIUtility.singleLineHeight + 2;
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

            bool isSelected = args.selected;

            Color bgColor = isSelected ? new Color(0.24f, 0.49f, 0.91f, 0.5f) : new Color(0.22f, 0.22f, 0.22f, 1.0f);

            if (!string.IsNullOrEmpty(item.Source))
            {
                if (item.Source == "HEAD")
                {
                    GUI.color = Color.green;
                    bgColor = isSelected ? new Color(0f, 0f, 0f, 0f) : new Color(0.22f, 0.22f, 0.22f, 1.0f);
                }
                else if (item.Source.StartsWith("REMOTE")) 
                { 
                    GUI.color = Color.red;
                    bgColor = isSelected ? new Color(0f, 0f, 0f, 0f) : new Color(0.22f, 0.22f, 0.22f, 1.0f);
                }
                else
                {
                    GUI.color = Color.white;
                    bgColor = isSelected ? new Color(0.24f, 0.49f, 0.91f, 0.5f) : new Color(0.22f, 0.22f, 0.22f, 1.0f);
                }
            }

            var rect = args.rowRect;
            var mousePos = Event.current.mousePosition;

            if (rect.Contains(mousePos))
            {
                var description = GetDescriptionForElement(item.Data.Name);
                if (!string.IsNullOrEmpty(description))
                {
                    var tooltipContent = new GUIContent(item.displayName, description);
                    GUI.Label(rect, tooltipContent);
                }
            }

            EditorGUI.DrawRect(args.rowRect, bgColor);
            base.RowGUI(args);
        }

        protected override void DoubleClickedItem(int id)
        {
            var item = FindRows(new List<int> { id }).FirstOrDefault() as MergeYamlTreeViewItem;
            if (item?.Data?.AssetPath != null)
            {
                AssetDatabase.OpenAsset(AssetDatabase.LoadMainAssetAtPath(item.Data.AssetPath));
            }
            else if (item != null && !string.IsNullOrEmpty(item.Source) && item.Source != "BASE")
            {
                BeginRename(item);
            }
        }

        protected override bool CanRename(TreeViewItem item)
        {
            if (item is MergeYamlTreeViewItem yamlItem)
            {
                return !string.IsNullOrEmpty(yamlItem.Source) && yamlItem.Source != "BASE";
            }
            return false;
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            if (!args.acceptedRename) return;

            var item = FindItem(args.itemID, rootItem) as MergeYamlTreeViewItem;
            if (item != null)
            {
                item.Data.Name = args.newName;
                item.displayName = args.newName;
                Reload();
                Repaint();
            }
        }

        private string GetDescriptionForElement(string elementName)
        {
            string[] guids = AssetDatabase.FindAssets("t:YamlElementDescription"); // 型を指定して検索
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var descs = AssetDatabase.LoadAssetAtPath<YamlElementDescription>(path);
                foreach (var desc in descs.Descriptions)
                {
                    if (desc != null && desc.Name == elementName)
                    {
                        return desc.Description;
                    }
                }
            }
            return null;
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
