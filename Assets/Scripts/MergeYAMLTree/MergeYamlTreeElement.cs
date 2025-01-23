using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using static MergeYamlTree.MergeYamlTreeUtil;

namespace MergeYamlTree
{
    [System.Flags]
    internal enum MergeYamlTreeDisplayNameOption
    {
        Default = 0,
        ClassIdToClassName = 1,
        GuidToAssetName = 2,
    }

    internal class MergeYamlTreeElement
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public string Source { get; set; }
        public List<MergeYamlTreeElement> Children { get; set; } = new List<MergeYamlTreeElement>();
        public Texture2D Icon { get; set; }
        public string AssetPath { get; set; }
        private readonly Dictionary<MergeYamlTreeDisplayNameOption, string> _nameCaches = new Dictionary<MergeYamlTreeDisplayNameOption, string>();

        public string GetDisplayName(MergeYamlTreeDisplayNameOption option)
        {
            if (_nameCaches.TryGetValue(option, out var name)) return name;
            name = CreateDisplayName(option);
            _nameCaches[option] = name;
            return name;
        }

        protected virtual string CreateDisplayName(MergeYamlTreeDisplayNameOption option)
        {
            if (HasFlags(option, MergeYamlTreeDisplayNameOption.GuidToAssetName) && Name == "guid" && string.IsNullOrEmpty(AssetPath) == false)
            {
                return Name + ": " + AssetPath;
            }
            else
            {
                if (string.IsNullOrEmpty(Value)) return Name;
                return Name + ": " + Value;
            }
        }
    }

    internal class MergeYamlObjectHeaderElement : MergeYamlTreeElement
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }

        protected override string CreateDisplayName(MergeYamlTreeDisplayNameOption option)
        {
            if (HasFlags(option, MergeYamlTreeDisplayNameOption.ClassIdToClassName) == false)
            {
                return base.CreateDisplayName(option);
            }

            var name = ClassName;
            if (ClassId == (int)MergeYamlTreeUtil.ClassId.MonoBehaviour)
            {
                var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(GetValue(this, "MonoBehaviour/m_Script/guid")));
                if (asset != null) name = asset.GetClass()?.Name ?? name;
            }

            return Name.Replace($"!u!{ClassId} ", $"{name} ");
        }
    }
}

