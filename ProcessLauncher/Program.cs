using System.Diagnostics;

var id = 42;
var startInfo = new ProcessStartInfo(@"pwsh.exe", $"Script.ps1 {id}")
{
    RedirectStandardOutput = true,
    RedirectStandardError = true,
};

void PrintThreadCount() => Console.WriteLine("Thread count: {0}", Process.GetCurrentProcess().Threads.Count);

PrintThreadCount();
var process = new Process { StartInfo = startInfo };

Console.WriteLine("Starting process");
PrintThreadCount();
process.Start();
Console.WriteLine("Process started");
PrintThreadCount();
var output = process.StandardOutput.ReadToEnd();
Console.WriteLine("Process output: {0}", output);
PrintThreadCount();
