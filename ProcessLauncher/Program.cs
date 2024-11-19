using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;

public class Program
{
    private static void Log(string message) => Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss.ffffff} [{Thread.CurrentThread.ManagedThreadId,3}] - {message}");
    private static void LogThreadCount() => Log($"Thread count: {Process.GetCurrentProcess().Threads.Count}");
    private static string CleanupOutput(string output) => output.Trim().Replace("\r\n", " ").Replace("\n", " ");

    private static async Task<int> Main()
    {
        const int Parallelism = 50;
        //ThreadPool.SetMaxThreads(5, 5);

        var path = LocateOrBuildFastApp();

        var sw = Stopwatch.StartNew();
        LogThreadCount();

        var outputChannel = Channel.CreateUnbounded<(int id, string message)>();

        var writerTask = StartOutputWriter(outputChannel.Reader);
        var tasks = Enumerable.Range(1, Parallelism).Select(i => RunProcess(i, outputChannel.Writer, path));

        LogThreadCount();
        await Task.WhenAll(tasks);
        outputChannel.Writer.Complete();

        await writerTask;

        LogThreadCount();
        Log($"All done in {sw.Elapsed.TotalSeconds:N2}s!");
        return 0;
    }

    private static Task StartOutputWriter(ChannelReader<(int id, string message)> reader)
    {
        return Task.Run(async () =>
        {
            await foreach (var (id, message) in reader.ReadAllAsync())
            {
                Log($"From process {id,3}: {message}");
            }
        });
    }

    private static Task RunProcess(int id, ChannelWriter<(int id, string message)> writer, string path)
    {
        var runner = new ProcessRunner(id, writer, path);
        return Task.Factory.StartNew(runner.Run, TaskCreationOptions.LongRunning);
    }

    class ProcessRunner(int id, ChannelWriter<(int id, string message)> writer, string path)
    {
        private readonly StringBuilder _outputBuilder = new(16384);

        public void Run()
        {
            var startInfo = new ProcessStartInfo(path, id.ToString())
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            var process = new Process { StartInfo = startInfo };

            Log($"Starting process {id}");
            process.Start();
            Log($"Process {id} started");

            process.OutputDataReceived += HandleStreamOutput;
            process.ErrorDataReceived += HandleStreamOutput;

            LogThreadCount();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            Log($"Process {id} BeginOutput/ErrorReadLine called");

            LogThreadCount();
            process.WaitForExit();

            Log($"Process {id} output: {CleanupOutput(_outputBuilder.ToString())}");
        }

        private async void HandleStreamOutput(object sender, DataReceivedEventArgs e)
        {
            lock (_outputBuilder)
            {
                _outputBuilder.AppendLine(e.Data);
            }
            await writer.WriteAsync((id, e.Data ?? ""));

        }
    }

    private static string LocateOrBuildFastApp()
    {
        var path = GetExecutablePath();
        if (File.Exists(path))
            return path;
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
            throw new Exception("Unable to find or build Fast App");

        return path;
    }

    private static string GetExecutablePath()
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
}