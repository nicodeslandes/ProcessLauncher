using System.Diagnostics;
using System.Runtime.InteropServices;

void Log(string message) => Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss.ffffff} [{Thread.CurrentThread.ManagedThreadId,3}] - {message}");
void LogThreadCount() => Log($"Thread count: {Process.GetCurrentProcess().Threads.Count}");
string CleanupOutput(string output) => output.Trim().Replace("\r\n", " ").Replace("\n", " ");

ThreadPool.SetMinThreads(200, 200);

string GetExecutablePath()
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        return @"..\FastApp\bin\Release\net9.0\win-x64\publish\FastApp.exe";
    }

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        return "../FastApp/bin/Release/net9.0/linux-x64/publish/FastApp";
    }

    throw new Exception("Unknown OS Platform");
}

var path = GetExecutablePath();
if (!File.Exists(path))
{
    Console.Error.WriteLine("Couldn't find executable {0}", path);
    Console.Error.WriteLine("Current path: {0}", Directory.GetCurrentDirectory());

    Console.WriteLine("Will try to build it");
    Process.Start(new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = "publish -c Release",
        WorkingDirectory = "../FastApp",
    })!.WaitForExit();

    if (!File.Exists(path))
        return 1;
}

var sw = Stopwatch.StartNew();
LogThreadCount();
var tasks = Enumerable.Range(1, 40).Select(RunProcess);

LogThreadCount();
Task.WaitAll(tasks);

LogThreadCount();
Log($"All done in {sw.Elapsed.TotalSeconds:N2}s!");
return 0;

Task RunProcess(int id)
{
    var startInfo = new ProcessStartInfo(path, id.ToString())
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