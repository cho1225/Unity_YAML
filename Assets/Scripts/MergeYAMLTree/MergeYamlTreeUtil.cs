using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
#if AYTV_YAMLDOTNET_11_2_OR_NEWER
using YamlDotNet.RepresentationModel;
#elif AYTV_VISUALSCRIPTING_1_6_0_OR_NEWER
using Unity.VisualScripting.YamlDotNet.RepresentationModel;
#else
#error require '"yamldotnet": "11.2.1"' or '"com.unity.visualscripting": "1.6.0"'
#endif
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace MergeYamlTree
{
    internal static class MergeYamlTreeUtil
    {
        public enum ClassId
        {
            MonoBehaviour = 114,
            PrefabInstance = 1001,
        }

        private static int _currentId;

        public static bool HasFlags(MergeYamlTreeDisplayNameOption option, MergeYamlTreeDisplayNameOption flag)
        {
            return (option & flag) == flag;
        }

        public static string GetValue(MergeYamlTreeElement element, string path)
        {
            return GetValue(element, path, x => x.Value);
        }

        public static Texture2D GetIcon(MergeYamlTreeElement element, string path)
        {
            return GetValue(element, path, x => x.Icon);
        }

        private static T GetValue<T>(MergeYamlTreeElement element, string path, System.Func<MergeYamlTreeElement, T> getValue)
        {
            if (element == null || path == null) return default;
            return GetValueRecursive(element, path.Split('/').ToList(), getValue);

            static T GetValueRecursive(MergeYamlTreeElement element, List<string> pathParts, System.Func<MergeYamlTreeElement, T> getValue)
            {
                var first = pathParts.First();
                pathParts.RemoveAt(0);
                if (element.Children == null) return default;
                foreach (var child in element.Children)
                {
                    if (child.Name != first) continue;
                    return pathParts.Count == 0 ? getValue.Invoke(child) : GetValueRecursive(child, pathParts, getValue);
                }

                return default;
            }
        }

        private static MergeYamlTreeElement BuildObjectHeaderElements(string objectHeader)
        {
            var classId = MergeUnityYamlParser.GetClassIdByObjectHeader(objectHeader);
            var icon = GetMiniTypeThumbnailFromClassID(classId);
            var objectHeaderElement = new MergeYamlObjectHeaderElement
            {
                Id = _currentId++,
                Name = objectHeader,
                ClassId = classId,
                ClassName = GetTypeNameByPersistentTypeID(classId),
                Icon = icon,
            };

            if (classId == (int)ClassId.PrefabInstance)
            {
                var guid = objectHeaderElement.Children?.FirstOrDefault(x => x.Name == "PrefabInstance")
                    ?.Children?.FirstOrDefault(x => x.Name == "m_SourcePrefab")
                    ?.Children?.FirstOrDefault(x => x.Name == "guid")?.Value;
                objectHeaderElement.Icon = GetAssetPreviewFromGUID(guid) ?? icon;
            }
            else if (classId == (int)ClassId.MonoBehaviour)
            {
                objectHeaderElement.Icon = GetIcon(objectHeaderElement, "MonoBehaviour/m_Script");
            }

            return objectHeaderElement;
        }

        public static (MergeYamlTreeElement[] elements, int nextId) BuildElements(int startElementId, string path)
        {
            _currentId = startElementId;
            var root = new MergeYamlTreeElement
            {
                Id = _currentId++,
                Name = Path.GetFileName(path),
                Icon = GetIcon(path),
                AssetPath = path,
            };
            var objectRoots = new List<MergeYamlTreeElement>();

            foreach (var (objectHeader, nodes) in MergeUnityYamlParser.Parse(path))
            {
                if (!string.IsNullOrEmpty(objectHeader))
                {
                    var headerElement = BuildObjectHeaderElements(objectHeader);
                    var convertedNodes = nodes
                        .SelectMany(dict => dict.Select(kv => (kv.Key, kv.Value, kv.Source))) // Dictionary を (string, string) のリストに変換
                        .ToList();
                    headerElement.Children.AddRange(ConvertToTreeElements(headerElement, convertedNodes));
                    objectRoots.Add(headerElement);
                }
            }

            root.Children = objectRoots;
            return (new[] { root }, _currentId);
        }

        private static Texture2D GetMiniTypeThumbnailFromClassID(int classId)
        {
            return typeof(AssetPreview).InvokeMember("GetMiniTypeThumbnailFromClassID",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null,
                new object[] { classId }) as Texture2D;
        }

        private static Texture2D GetAssetPreviewFromGUID(string guid)
        {
            return typeof(AssetPreview).InvokeMember("GetAssetPreviewFromGUID",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null,
                new object[] { guid }) as Texture2D;
        }

        private static Texture2D GetIcon(System.Type type) => type == null ? null : AssetPreview.GetMiniTypeThumbnail(type);

        private static Texture2D GetIcon(string assetPath)
        {
            var cachedIcon = AssetDatabase.GetCachedIcon(assetPath) as Texture2D;
            if (cachedIcon != null) return cachedIcon;

            if (AssetDatabase.IsValidFolder(assetPath)) return EditorGUIUtility.FindTexture(EditorResources.folderIconName);

            var tex = UnityEditorInternal.InternalEditorUtility.FindIconForFile(assetPath);
            if (tex != null) return tex;

            if (AssetImporter.GetAtPath(assetPath) is PluginImporter) return EditorGUIUtility.FindTexture("Assembly Icon");

            return GetIcon(AssetDatabase.GetMainAssetTypeAtPath(assetPath));
        }

        private static IEnumerable<MergeYamlTreeElement> ConvertToTreeElements(
    MergeYamlTreeElement parentElement, List<(string Key, string Value, string Source)> nodes)
        {
            var groupedNodes = nodes.GroupBy(n => n.Key); // 同じキーの要素をまとめる

            foreach (var group in groupedNodes)
            {
                var elements = group.Select(n => new MergeYamlTreeElement
                {
                    Id = _currentId++,
                    Name = n.Key,
                    Value = n.Value,
                    Source = n.Source
                }).ToList();

                // m_Color のように複数の値がある場合、それをすべて追加
                if (elements.Count > 1)
                {
                    var parent = new MergeYamlTreeElement
                    {
                        Id = _currentId++,
                        Name = group.Key,
                    };

                    foreach (var element in elements)
                    {
                        parent.Children.Add(element);
                    }

                    yield return parent;
                }
                else
                {
                    yield return elements[0];
                }
            }
        }

        private static string GetTypeNameByPersistentTypeID(int id)
        {
            const BindingFlags flags = BindingFlags.Public
                                       | BindingFlags.Static
                                       | BindingFlags.Instance
                                       | BindingFlags.InvokeMethod
                                       | BindingFlags.GetProperty;
            var assembly = Assembly.GetAssembly(typeof(MonoScript));
            var unityType = assembly.GetType("UnityEditor.UnityType");
            var findTypeByPersistentTypeID = unityType.GetMethod("FindTypeByPersistentTypeID", flags);
            var nameProperty = unityType.GetProperty("name", flags);
            var typeInstance = findTypeByPersistentTypeID?.Invoke(null, new object[] { id });
            return typeInstance != null ? nameProperty?.GetValue(typeInstance) as string : null;
        }
    }
}
