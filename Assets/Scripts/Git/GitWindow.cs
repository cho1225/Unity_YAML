using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class GitWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private string commitMessage = "コミットメッセージ";
    private string newBranchName = "";
    private string[] branches = new[] { "main" };
    private int mergeTargetIndex;
    private int switchBranchIndex;
    private int currentBranchIndex;
    private GitFileWatcher fileWatcher;

    [MenuItem("Tools/Git")]
    public static void ShowWindow() => GetWindow<GitWindow>("Git");

    private void OnEnable()
    {
        RefreshBranches();
        currentBranchIndex = CommandExecutor.GetCurrentBranchIndex(branches);
        fileWatcher = new GitFileWatcher(RefreshChangedFiles);
    }

    private void OnDisable() => fileWatcher?.Dispose();

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        EditorGUILayout.BeginVertical("box");

        DrawBranchInfo();
        DrawGitControls();
        DrawBranchManagement();
        DrawMergeOptions();

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();
    }

    private void DrawBranchInfo()
    {
        EditorGUILayout.LabelField("Git操作", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(branches[currentBranchIndex], EditorStyles.boldLabel);
    }

    private void DrawGitControls()
    {
        if (GUILayout.Button("Add")) CommandExecutor.Execute("add .");
        commitMessage = EditorGUILayout.TextField(commitMessage);
        if (GUILayout.Button("Commit")) { CommandExecutor.Execute($"commit -m \"{commitMessage}\""); commitMessage = "コミットメッセージ"; }
        if (GUILayout.Button("Push")) CommandExecutor.Execute($"push origin {branches[currentBranchIndex]}");
        if (GUILayout.Button("Pull")) CommandExecutor.Execute("pull");
        if (GUILayout.Button("Status")) CommandExecutor.Execute("status");
        if (GUILayout.Button("Log")) CommandExecutor.Execute("log --oneline -n 10");
    }

    private void DrawBranchManagement()
    {
        newBranchName = EditorGUILayout.TextField(newBranchName);
        if (GUILayout.Button("ブランチを作成") && !string.IsNullOrEmpty(newBranchName))
        {
            CommandExecutor.Execute($"branch {newBranchName}");
            RefreshBranches();
            newBranchName = "";
        }

        switchBranchIndex = EditorGUILayout.Popup(switchBranchIndex, branches);
        if (GUILayout.Button("ブランチを切り替え"))
        {
            CommandExecutor.Execute($"switch {branches[switchBranchIndex]}");
            currentBranchIndex = CommandExecutor.GetCurrentBranchIndex(branches);
            AssetDatabase.Refresh();
        }
    }

    private void DrawMergeOptions()
    {
        mergeTargetIndex = EditorGUILayout.Popup(mergeTargetIndex, branches);
        if (GUILayout.Button("ブランチをマージ"))
        {
            if (!CommandExecutor.HasConflicts(branches[mergeTargetIndex]))
            {
                CommandExecutor.Execute($"merge {branches[mergeTargetIndex]} --no-ff");
                CommandExecutor.Execute($"push origin {branches[currentBranchIndex]}");
            }
            else Debug.Log("コンフリクトがあります");
        }
    }

    private void RefreshBranches() => branches = CommandExecutor.GetBranches();
    private void RefreshChangedFiles() => fileWatcher.FetchChangedFiles();
}


///// <summary>
///// エラーメッセージが警告かどうかを判断する。
///// </summary>
///// <param name="message">エラーメッセージ</param>
///// <returns>Warningであればtrue、そうでなければfalse</returns>
//private bool IsWarning(string message)
//    {
//        // "warning" という単語が含まれている場合はWarningとする
//        return Regex.IsMatch(message, @"\bwarning\b", RegexOptions.IgnoreCase);
//    }

//    /// <summary>
//    /// メッセージが成功メッセージかを確認する
//    /// </summary>
//    /// <param name="message">エラーメッセージ</param>
//    /// <returns>成功メッセージならtrue、そうでなければfalse</returns>
//    private bool IsSuccessMessage(string message)
//    {
//        return message.StartsWith("To https://") || message.StartsWith("Switched") || Regex.IsMatch(message, @"^\s+\d+\.\.\d+");
//    }
//}
