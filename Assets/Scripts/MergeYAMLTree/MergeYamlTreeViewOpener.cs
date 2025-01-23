using System.Collections.Generic;
using UnityEditor;

namespace MergeYamlTree
{
    public class MergeYamlTreeViewOpener
    {
        MergeYamlTreeViewWindow window;

        public void OpenYamlTreeView(List<string> conflictFiles)
        {
            window = EditorWindow.GetWindow<MergeYamlTreeViewWindow>(nameof(MergeYamlTreeViewWindow));
            window.ConflictFiles = conflictFiles;
        }
    }
}
