using System.Diagnostics;

if (args.Length == 0)
{
    Console.WriteLine("Please provide a script ID.");
    return;
}

var scriptId = args[0];
Console.WriteLine($"Starting script {scriptId}");

var sw = Stopwatch.StartNew();
Thread.Sleep(3000);
Console.Error.WriteLine($"Script {scriptId}: error output");
Thread.Sleep(3000);
Console.WriteLine($"Terminating script {scriptId}");
Thread.Sleep(3000);
Console.WriteLine($"Elapsed time: {sw.Elapsed.TotalSeconds:N2}s");
