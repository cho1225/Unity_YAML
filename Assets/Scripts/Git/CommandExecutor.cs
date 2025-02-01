using System;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using System.Text.RegularExpressions;

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
                //MergeYamlFiles(filePath);
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

    public static void MergeYamlFiles(string filePath)
    {
        string yaml1 = Run($"show HEAD:{filePath}").output;
        string yaml2 = Run($"show MERGE_HEAD:{filePath}").output;


        // すべてのオブジェクトの定義部分を抽出する正規表現
        string pattern = "(--- !u!\\d+ &\\d+\\n[^\n]+:.*?)((?:\\n  [^\n]+)*)";
        var matches1 = Regex.Matches(yaml1, pattern, RegexOptions.Singleline);
        var matches2 = Regex.Matches(yaml2, pattern, RegexOptions.Singleline);

        var mergedYaml = yaml1;

        // file2のオブジェクト定義を1つずつチェック
        foreach (Match match2 in matches2)
        {
            string objHeader = match2.Groups[1].Value; // オブジェクトのヘッダー部分
            string props2 = match2.Groups[2].Value;    // プロパティ部分

            if (yaml1.Contains(objHeader)) // すでに存在するオブジェクトの場合
            {
                mergedYaml = Regex.Replace(mergedYaml, $"({Regex.Escape(objHeader)})((?:\\n  [^\n]+)*)", match1 =>
                {
                    string props1 = match1.Groups[2].Value; // 既存のプロパティ部分
                    string mergedProps = MergeProperties(props1, props2); // プロパティを統合
                    return objHeader + "\n  <<<<< Merged" + mergedProps;
                });
            }
            else // 新しいオブジェクトの場合、追記する
            {
                mergedYaml += "\n" + objHeader + "\n  <<<<< Merged" + props2;
            }
        }

        File.WriteAllText($"{filePath}A.txt", yaml1);
        File.WriteAllText($"{filePath}B.txt", yaml2);
        File.WriteAllText($"{filePath}.txt", mergedYaml);
    }

    private static string MergeProperties(string props1, string props2)
    {
        var propDict = new System.Collections.Generic.Dictionary<string, string>();

        // props1のプロパティを辞書に格納
        foreach (var line in props1.Split('\n'))
        {
            if (line.Trim().Length > 0)
            {
                var parts = line.Split(':');
                if (parts.Length >= 2)
                {
                    propDict[parts[0].Trim()] = parts[1].Trim();
                }
            }
        }

        // props2のプロパティを辞書に上書き
        foreach (var line in props2.Split('\n'))
        {
            if (line.Trim().Length > 0)
            {
                var parts = line.Split(':');
                if (parts.Length >= 2)
                {
                    propDict[parts[0].Trim()] = parts[1].Trim();
                }
            }
        }

        // 統合結果を組み立て
        string mergedProps = "";
        foreach (var kvp in propDict)
        {
            mergedProps += $"\n  {kvp.Key}: {kvp.Value}";
        }
        return mergedProps;
    }
}

