using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GitFileWatcher : IDisposable
{
    private readonly FileSystemWatcher fileWatcher;
    private readonly HashSet<string> fetchedFiles = new();
    private readonly Action onFileChanged;

    public GitFileWatcher(Action _onFileChanged)
    {
        this.onFileChanged = _onFileChanged;
        fileWatcher = new FileSystemWatcher(Path.GetFullPath(Application.dataPath + "/.."))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            Filter = "*.*",
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };
        fileWatcher.Changed += (_, _) => onFileChanged();
    }

    public void FetchChangedFiles()
    {
        var result = CommandExecutor.GetChangedFiles();
        var changedFiles = result.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToArray();
        foreach (var file in changedFiles) if (fetchedFiles.Add(file)) Debug.Log(file);
    }

    public void Dispose() => fileWatcher.Dispose();
}
