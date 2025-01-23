using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MergeYamlTree;
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
        InitializeBranchIndex();
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
            InitializeBranchIndex();
            newBranchName = "";
        }

        switchBranchIndex = EditorGUILayout.Popup(switchBranchIndex, branches);
        if (GUILayout.Button("ブランチを切り替え"))
        {
            CommandExecutor.Execute($"switch {branches[switchBranchIndex]}");
            InitializeBranchIndex();
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
            else
            {
                var windowOpener = new MergeYamlTreeViewOpener();
                List<string> conflictFiles = CommandExecutor.GetConflictFiles(branches[mergeTargetIndex]);
                windowOpener.OpenYamlTreeView(conflictFiles);
            }
        }
    }

    private void InitializeBranchIndex()
    {
        currentBranchIndex = CommandExecutor.GetCurrentBranchIndex(branches);
        switchBranchIndex = 0;
        mergeTargetIndex = 0;
    }

    private void RefreshBranches() => branches = CommandExecutor.GetBranches();

    private void RefreshChangedFiles() => fileWatcher.FetchChangedFiles();
}
