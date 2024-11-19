using System.Diagnostics;

void PrintThreadCount() => Console.WriteLine("Thread count: {0}", Process.GetCurrentProcess().Threads.Count);

PrintThreadCount();
var tasks = Enumerable.Range(1, 25).Select(RunProcess);

PrintThreadCount();
Task.WaitAll(tasks);

Console.WriteLine("All done!");
PrintThreadCount();

Task RunProcess(int id)
{
    var startInfo = new ProcessStartInfo(@"pwsh.exe", $"Script.ps1 {id}")
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };

    return Task.Run(() =>
    {
        var process = new Process { StartInfo = startInfo };
        Console.WriteLine("Starting process {0}", id);
        process.Start();
        Console.WriteLine("Process {0} started", id);
        PrintThreadCount();
        var output = process.StandardOutput.ReadToEnd();
        Console.WriteLine("Process {0} output: {1}", id, output);
        var error = process.StandardError.ReadToEnd();
        Console.WriteLine("Process {0} error: {1}", id, error);
    });
}