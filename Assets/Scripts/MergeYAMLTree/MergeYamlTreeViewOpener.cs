using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MergeYamlTree
{
    public class MergeYamlTreeViewOpener
    {
        MergeYamlTreeViewWindow window;

        public void OpenYamlTreeView(List<string> conflictFiles)
        {
            Debug.Log(conflictFiles.Count);
            window = EditorWindow.GetWindow<MergeYamlTreeViewWindow>(nameof(MergeYamlTreeViewWindow));
            if (conflictFiles == null) UnityEditor.EditorUtility.DisplayDialog("Debug", "yabai", "OK");
            foreach (string conflictFile in conflictFiles) Debug.LogError($"conflictFiles Count: {conflictFiles?.Count}");
            window.ConflictFiles = conflictFiles;
        }
    }
}
