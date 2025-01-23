using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
#if AYTV_YAMLDOTNET_11_2_OR_NEWER
using YamlDotNet.RepresentationModel;
#elif AYTV_VISUALSCRIPTING_1_6_0_OR_NEWER
using Unity.VisualScripting.YamlDotNet.RepresentationModel;
#else
#error require '"yamldotnet": "11.2.1"' or '"com.unity.visualscripting": "1.6.0"'
#endif
using UnityEditor;
using Object = UnityEngine.Object;

namespace MergeYamlTree
{
    internal static class MergeUnityYamlParser
    {
        private const string ObjectHeaderPrefix = "--- !u!";

        public static int GetClassIdByObjectHeader(string objectHeader)
        {
            return int.Parse(objectHeader.Substring(ObjectHeaderPrefix.Length).Split(' ')[0]);
        }

        public static IEnumerable<(string objectHeader, List<(string Key, string Value, string Source)>[] documents)> Parse(string yamlPath)
        {
            if (File.Exists(yamlPath))
            {
                return yamlPath.EndsWith(".meta") ? ParseMetaYaml(yamlPath) : ParseAssetYaml(yamlPath);
            }
            return new (string objectHeader, List<(string Key, string Value, string Source)>[] documents)[] { (null, new List<(string Key, string Value, string Source)>[0]) };
        }

        private static IEnumerable<(string objectHeader, List<(string Key, string Value, string Source)>[] documents)> ParseMetaYaml(string yamlPath)
        {
            yield return (null, ParseText(File.ReadAllText(yamlPath)));
        }

        private static IEnumerable<(string objectHeader, List<(string Key, string Value, string Source)>[] documents)> ParseAssetYaml(string yamlPath)
        {
            var lines = File.ReadLines(yamlPath);
            var sb = new StringBuilder();
            string header = null;

            foreach (var line in lines)
            {
                if (line.StartsWith(ObjectHeaderPrefix))
                {
                    if (header != null)
                    {
                        yield return (header, ParseText(sb.ToString()));
                        sb.Clear();
                    }
                    header = line;
                    continue;
                }
                if (header != null) sb.AppendLine(line);
            }

            if (header != null)
                yield return (header, ParseText(sb.ToString()));
        }

        private static List<(string Key, string Value, string Source)>[] ParseText(string text)
        {
            var docs = new List<List<(string Key, string Value, string Source)>>();
            var currentDoc = new List<(string Key, string Value, string Source)>();
            string currentSource = "BASE";

            foreach (var line in text.Split('\n'))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                {
                    continue;
                }

                // コンフリクトの開始を検出
                if (trimmed.StartsWith("<<<<<<<"))
                {
                    currentSource = "HEAD";
                    continue;
                }
                if (trimmed.StartsWith("======="))
                {
                    currentSource = "REMOTE";
                    continue;
                }
                if (trimmed.StartsWith(">>>>>>>"))
                {
                    currentSource = "BASE";
                    continue;
                }

                var parts = trimmed.Split(':', 2);
                if (parts.Length == 2)
                {
                    currentDoc.Add((parts[0].Trim(), parts[1].Trim(), currentSource));
                }
            }

            docs.Add(currentDoc);
            return docs.ToArray();
        }
    }
}
