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
            window = EditorWindow.GetWindow<MergeYamlTreeViewWindow>(nameof(MergeYamlTreeViewWindow));
            if (conflictFiles == null) Debug.LogError("yabai");
            foreach (string conflictFile in conflictFiles) Debug.LogError(conflictFile);
            window.ConflictFiles = conflictFiles;
        }
    }
}
