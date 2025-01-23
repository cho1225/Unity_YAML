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
            Debug.Log(conflictFiles);
            window.ConflictFiles = conflictFiles;
        }
    }
}
