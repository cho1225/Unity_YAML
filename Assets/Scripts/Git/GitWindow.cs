using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Unityエディター上でGit操作を実行するカスタムエディタウィンドウ
/// 現状ユーザーはこのウィンドウを通じて，Gitのadd, commit, push, pullコマンドを実行可能
/// </summary>
public class GitWindow : EditorWindow
{
    private Vector2 scrollPosition;

    private string commitMessage = "コミットメッセージ";
    private string newBranchName = "NewBranch";
    private string[] branches = new string[] { "main" };
    private int branchIndex1 = 0;
    private int branchIndex2 = 0;
    private int currentBranchIndex = 0;

    private readonly HashSet<string> fetchedFiles = new();
    private FileSystemWatcher fileWatcher;

    [MenuItem("Tools/Git")]
    public static void ShowWindow()
    {
        GetWindow<GitWindow>("Git");
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.BeginVertical("box");

        GUILayout.Label("Git操作", EditorStyles.boldLabel);

        if (GUILayout.Button("Add", GUILayout.Width(200)))
        {
            ExecuteGitCommand("add .");
        }

        commitMessage = EditorGUILayout.TextField(commitMessage, GUILayout.Width(200), GUILayout.Height(60));
        if (GUILayout.Button("Commit", GUILayout.Width(200)))
        {
            ExecuteGitCommand($"commit -m \"{commitMessage}\"");
            commitMessage = "コミットメッセージ";
        }

        if (GUILayout.Button("Push", GUILayout.Width(200)))
        {
            ExecuteGitCommand("push");
        }

        if (GUILayout.Button("Pull", GUILayout.Width(200)))
        {
            ExecuteGitCommand("pull");
        }

        if (GUILayout.Button("Status", GUILayout.Width(200)))
        {
            ExecuteGitCommand("status");
        }

        if (GUILayout.Button("Log", GUILayout.Width(200)))
        {
            ExecuteGitCommand("log --oneline -n 10"); // 最新10件のログを簡易表示
        }

        newBranchName = EditorGUILayout.TextField(newBranchName, GUILayout.Width(200));
        if (GUILayout.Button("ブランチを作成", GUILayout.Width(200)))
        {
            ExecuteGitCommand($"branch {newBranchName}");
            FreshBranches();
        }

        currentBranchIndex = EditorGUILayout.Popup(currentBranchIndex, branches, GUILayout.Width(200));
        if (GUILayout.Button("ブランチを切り替え", GUILayout.Width(200)))
        {
            ExecuteGitCommand($"switch {branches[currentBranchIndex]}");
        }

        EditorGUILayout.BeginHorizontal();
        branchIndex1 = EditorGUILayout.Popup(branchIndex1, branches, GUILayout.Width(150));
        GUILayout.Label("←", new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter }, GUILayout.Width(20));
        branchIndex2 = EditorGUILayout.Popup(branchIndex2, branches, GUILayout.Width(150));
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("ブランチをマージ", GUILayout.Width(200)))
        {
            //MergeBranches(branches[branchAIndex], branches[branchBIndex]);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndScrollView();
    }

    private void OnEnable()
    {
        FreshBranches();
        StartFileWatcher(); // ファイル監視を開始
    }

    private void OnDisable()
    {
        StopFileWatcher(); // ファイル監視を停止
    }

    private void FreshBranches()
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = "branch --format=\"%(refname:short)\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(startInfo))
        {
            string output = process.StandardOutput.ReadToEnd();
            branches = output.Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        }
    }

    private void StartFileWatcher()
    {
        string path = Path.GetFullPath(Application.dataPath + "/.."); // 監視対象のパス
        fileWatcher = new FileSystemWatcher(path)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            Filter = "*.*",
            IncludeSubdirectories = true
        };

        fileWatcher.Changed += OnChanged;
        fileWatcher.Created += OnChanged;
        fileWatcher.Deleted += OnChanged;
        fileWatcher.Renamed += OnChanged;

        fileWatcher.EnableRaisingEvents = true; // 変更通知を有効にする
    }

    private void StopFileWatcher()
    {
        if (fileWatcher != null)
        {
            fileWatcher.EnableRaisingEvents = false;
            fileWatcher.Dispose();
            fileWatcher = null;
        }
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        // Gitの変更を取得
        FetchChangedFiles();
    }

    private void FetchChangedFiles()
    {
        string output = GetChangedFiles("status --porcelain");

        if (!string.IsNullOrEmpty(output))
        {
            string[] changedFiles = output.Split('\n')
                .Where(line => !string.IsNullOrEmpty(line))
                .Select(line => line[3..].Trim())
                .ToArray();

            // 新しい変更ファイルをチェック
            foreach (var file in changedFiles)
            {
                if (!fetchedFiles.Contains(file))
                {
                    UnityEngine.Debug.Log(file);
                    fetchedFiles.Add(file);
                }
            }
        }
    }

    private string GetChangedFiles(string arguments)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "git",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8
        };

        using Process process = new();
        process.StartInfo = startInfo;
        process.Start();


        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (!string.IsNullOrEmpty(error))
        {
            UnityEngine.Debug.LogError(error);
        }

        return output;
    }

    /// <summary>
    /// 指定されたGitコマンドを実行し，出力結果またはエラーメッセージをUnityのコンソールに表示
    /// </summary>
    /// <param name="command">実行するGitコマンド</param>
    private void ExecuteGitCommand(string command)
    {
        ProcessStartInfo psi = new("git", command)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        Process process = Process.Start(psi);
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        bool hasOutput = !string.IsNullOrEmpty(output);
        bool hasError = !string.IsNullOrEmpty(error);
        bool hasSuccessMessage = hasError && IsSuccessMessage(error);
        bool hasWarning = hasError && IsWarning(error);

        if (hasOutput)
            UnityEngine.Debug.Log(output);

        if (hasSuccessMessage)
        {
            UnityEngine.Debug.Log(error);
        }
        else if (hasWarning)
        {
            UnityEngine.Debug.LogWarning(error);
        }
        else if (hasError)
        {
            UnityEngine.Debug.LogError(error);
        }
    }

    /// <summary>
    /// エラーメッセージが警告かどうかを判断する。
    /// </summary>
    /// <param name="message">エラーメッセージ</param>
    /// <returns>Warningであればtrue、そうでなければfalse</returns>
    private bool IsWarning(string message)
    {
        // "warning" という単語が含まれている場合はWarningとする
        return Regex.IsMatch(message, @"\bwarning\b", RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// メッセージが成功メッセージかを確認する
    /// </summary>
    /// <param name="message">エラーメッセージ</param>
    /// <returns>成功メッセージならtrue、そうでなければfalse</returns>
    private bool IsSuccessMessage(string message)
    {
        return message.StartsWith("To https://") || message.StartsWith("Switched") || Regex.IsMatch(message, @"^\s+\d+\.\.\d+");
    }
}
