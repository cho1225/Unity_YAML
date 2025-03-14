using System;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;

public static class CommandExecutor
{
    public static void Execute(string command)
    {
        var result = Run(command);
        if (!string.IsNullOrEmpty(result.output)) Debug.Log(result.output);
        if (!string.IsNullOrEmpty(result.error)) Debug.LogWarning(result.error);
    }

    public static string[] GetBranches()
    {
        var result = Run("branch --format=\"%(refname:short)\"");
        return result.output.Split('\n').Where(b => !string.IsNullOrEmpty(b)).ToArray();
    }

    public static int GetCurrentBranchIndex(string[] branches)
    {
        var result = Run("rev-parse --abbrev-ref HEAD");
        return Array.IndexOf(branches, result.output.Trim());
    }

    public static bool HasConflicts(string branch)
    {
        Run("fetch");
        var mergeResult = Run($"merge --no-commit --no-ff origin/{branch}");
        if (mergeResult.exitCode == 0) { Run("merge --abort"); return false; }
        Run("merge --abort");
        return true;
    }

    public static List<string> GetConflictFiles(string branch)
    {
        // コンフリクト中の可視化ファイルのパス
        List<string> conflictFiles = new List<string>();

        Run("fetch");
        var mergeResult = Run($"merge --no-commit --no-ff origin/{branch}");

        string output = Run("status --porcelain").output;
        string[] lines = output.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            // コンフリクト中のファイルはUUから始まる
            if (line.StartsWith("UU"))
            {
                string filePath = line.Substring(3).Trim();
                File.Copy(filePath, $"{filePath}.txt", true);
                conflictFiles.Add($"{filePath}.txt");
            }
        }

        Run("merge --abort");

        return conflictFiles;
    }

    public static string GetChangedFiles()
    {
        return Run("status --porcelain").output;
    }

    private static (int exitCode, string output, string error) Run(string arguments)
    {
        var psi = new ProcessStartInfo("git", arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };
        using var process = Process.Start(psi);
        var output = process.StandardOutput.ReadToEnd().Trim();
        var error = process.StandardError.ReadToEnd().Trim();
        process.WaitForExit();
        return (process.ExitCode, output, error);
    }
}

