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
    private string commitMessage = "コミットメッセージ";
    private HashSet<string> fetchedFiles = new HashSet<string>();
    private FileSystemWatcher fileWatcher;

    [MenuItem("Tools/Git")]
    public static void ShowWindow()
    {
        GetWindow<GitWindow>("Git");
    }

    private void OnGUI()
    {
        GUILayout.Label("Git操作", EditorStyles.boldLabel);

        if (GUILayout.Button("Add"))
        {
            ExecuteGitCommand("add .");
        }

        commitMessage = EditorGUILayout.TextField("Commit Message", commitMessage);
        if (GUILayout.Button("Commit"))
        {
            ExecuteGitCommand($"commit -m \"{commitMessage}\"");
            commitMessage = "コミットメッセージ";
        }

        if (GUILayout.Button("Push"))
        {
            ExecuteGitCommand("push");
        }

        if (GUILayout.Button("Pull"))
        {
            ExecuteGitCommand("pull");
        }

        GUILayout.Space(10);
        if (GUILayout.Button("Status"))
        {
            ExecuteGitCommand("status");
        }

        if (GUILayout.Button("Log"))
        {
            ExecuteGitCommand("log --oneline -n 10"); // 最新10件のログを簡易表示
        }
    }

    private void OnEnable()
    {
        StartFileWatcher(); // ファイル監視を開始
    }

    private void OnDisable()
    {
        StopFileWatcher(); // ファイル監視を停止
    }

    private void StartFileWatcher()
    {
        string path = Application.dataPath; // 監視対象のパス
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
                .Select(line => line.Substring(3).Trim())
                .ToArray();

            // 新しい変更ファイルをチェック
            foreach (var file in changedFiles)
            {
                if (!fetchedFiles.Contains(file))
                {
                    UnityEngine.Debug.Log(file); // 新しい変更ファイルをログに表示
                    fetchedFiles.Add(file); // 取得したファイルとして追加
                }
            }
        }
        else
        {
            UnityEngine.Debug.Log("No changed files.");
        }
    }

    private string GetChangedFiles(string command)
    {
        return "";
    }

    /// <summary>
    /// 指定されたGitコマンドを実行し，出力結果またはエラーメッセージをUnityのコンソールに表示
    /// </summary>
    /// <param name="command">実行するGitコマンド</param>
    private void ExecuteGitCommand(string command)
    {
        ProcessStartInfo psi = new ProcessStartInfo("git", command)
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
        return message.StartsWith("To https://") || Regex.IsMatch(message, @"^\s+\d+\.\.\d+");
    }
}
