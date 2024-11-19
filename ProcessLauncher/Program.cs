﻿using System.Diagnostics;
using System.Net.WebSockets;

void Log(string message) => Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss.ffff} - {message}");
void LogThreadCount() => Log($"Thread count: {Process.GetCurrentProcess().Threads.Count}");
string CleanupOutput(string output) => output.Trim().Replace("\r\n", " ").Replace("\n", " ");

var sw = Stopwatch.StartNew();
LogThreadCount();
var tasks = Enumerable.Range(1, 25).Select(RunProcess);

LogThreadCount();
Task.WaitAll(tasks);

LogThreadCount();
Log($"All done in {sw.Elapsed.TotalSeconds:N2}s!");

Task RunProcess(int id)
{
    var startInfo = new ProcessStartInfo(@"pwsh.exe", $"Script.ps1 {id}")
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };

    return Task.Factory.StartNew(() =>
    {
        var process = new Process { StartInfo = startInfo };
        Log($"Starting process {id}");
        process.Start();
        Log($"Process {id} started");
        LogThreadCount();
        var output = process.StandardOutput.ReadToEnd();
        Log($"Process {id} output: {CleanupOutput(output)}");
        var error = process.StandardError.ReadToEnd();
        Log($"Process {id} error: {CleanupOutput(error)}");
    }, TaskCreationOptions.LongRunning);
}