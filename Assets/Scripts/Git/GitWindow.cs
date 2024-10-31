using UnityEditor;
using UnityEngine;
using System.Diagnostics;

/// <summary>
/// Unity�G�f�B�^�[���Git��������s����J�X�^���G�f�B�^�E�B���h�E
/// ���󃆁[�U�[�͂��̃E�B���h�E��ʂ��āCGit��add, commit, push, pull�R�}���h�����s�\
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
        GUILayout.Label("Git����", EditorStyles.boldLabel);

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
            string message = "�R�~�b�g���b�Z�[�W";
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
    /// �w�肳�ꂽGit�R�}���h�����s���C�o�͌��ʂ܂��̓G���[���b�Z�[�W��Unity�̃R���\�[���ɕ\��
    /// </summary>
    /// <param name="command">���s����Git�R�}���h</param>
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
