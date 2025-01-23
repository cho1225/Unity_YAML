using UnityEngine;
using UnityEditor.IMGUI.Controls;

namespace MergeYamlTree
{
    internal class MergeYamlTreeViewItem : TreeViewItem
    {
        public MergeYamlTreeElement Data { get; }
        public string Source { 
            get => Data.Source;
            set => Data.Source = value;
        }

        public override int id
        {
            get => Data.Id;
            set => Data.Id = value;
        }

        public override string displayName
        {
            get => Data.GetDisplayName(DisplayNameOption);
            set => Data.Name = value;
        }

        public override Texture2D icon
        {
            get => ShowIcon ? Data.Icon : null;
            set => Data.Icon = value;
        }

        public MergeYamlTreeDisplayNameOption DisplayNameOption { get; set; }
        public bool ShowIcon { get; set; }

        public MergeYamlTreeViewItem(MergeYamlTreeElement data)
        {
            Data = data;
        }
    }
}
