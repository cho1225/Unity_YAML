using UnityEditor;
using UnityEngine;
using System.Diagnostics;

/// <summary>
/// Unityエディター上でGit操作を実行するカスタムエディタウィンドウ
/// 現状ユーザーはこのウィンドウを通じて，Gitのadd, commit, push, pullコマンドを実行可能
/// </summary>
public class GitWindow : EditorWindow
{
    [MenuItem("Tools/Git")]
    public static void ShowWindow()
    {
        GetWindow<GitWindow>("Git");
    }

    private void OnGUI()
    {
        GUILayout.Label("Git操作", EditorStyles.boldLabel);

        if (GUILayout.Button("Status"))
        {
            ExecuteGitCommand("status");
        }

        if (GUILayout.Button("Add"))
        {
            ExecuteGitCommand("add .");
        }

        if (GUILayout.Button("Commit"))
        {
            string message = "コミットメッセージ";
            ExecuteGitCommand($"commit -m \"{message}\"");
        }

        if (GUILayout.Button("Push"))
        {
            ExecuteGitCommand("push");
        }

        if (GUILayout.Button("Pull"))
        {
            ExecuteGitCommand("pull");
        }
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
            CreateNoWindow = true
        };

        Process process = Process.Start(psi);
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (!string.IsNullOrEmpty(output))
            UnityEngine.Debug.Log(output);

        if (!string.IsNullOrEmpty(error))
            UnityEngine.Debug.LogError(error);
    }
}
