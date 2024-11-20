using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;

const int Parallelism = 40;

void Log(string message) => Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss.ffffff} [{Thread.CurrentThread.ManagedThreadId,3}] - {message}");
void LogThreadCount() => Log($"Thread count: {Process.GetCurrentProcess().Threads.Count}");
string CleanupOutput(string output) => output.Trim().Replace("\r\n", " ").Replace("\n", " ");

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

// Move to the first parent folder named "ProcessLauncher"
while (true)
{
    var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
    if (currentDirectory.Name == "ProcessLauncher")
    {
        break;
    }

    if (currentDirectory.Parent is null)
    {
        throw new Exception("Failed to locate ProcessLauncher directory");
    }

    Directory.SetCurrentDirectory(currentDirectory.Parent.FullName);
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

var outputChannel = Channel.CreateUnbounded<(int id, string message)>();

var writerTask = StartOutputWriter(outputChannel.Reader);
var tasks = Enumerable.Range(1, Parallelism).Select(i => RunProcess(i, outputChannel.Writer));

LogThreadCount();
await Task.WhenAll(tasks);
outputChannel.Writer.Complete();

await writerTask;

LogThreadCount();
Log($"All done in {sw.Elapsed.TotalSeconds:N2}s!");
return 0;

async Task RunProcess(int id, ChannelWriter<(int id, string message)> writer)
{
    var startInfo = new ProcessStartInfo(path, id.ToString())
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };

    Task<Process> StartProcess()
    {
        return Task.Factory.StartNew(() =>
        {
            var process = new Process { StartInfo = startInfo };

            Log($"Starting process {id}");
            process.Start();
            Log($"Process {id} started");
            return process;
        }, TaskCreationOptions.LongRunning);
    }

    var process = await StartProcess();

    var outputBuilder = new StringBuilder(16384);

    Task ReadStreamMessages(StreamReader stream)
    {
        return Task.Run(async () =>
        {
            while (true)
            {
                var line = await stream.ReadLineAsync();
                if (line == null)
                {
                    break;
                }

                lock (outputBuilder)
                {
                    outputBuilder.Append(line);
                }
                await writer.WriteAsync((id, line));
            }
        });
    }

    var outputTask = ReadStreamMessages(process.StandardOutput);
    var errorTask = ReadStreamMessages(process.StandardError);

    Log($"Started read message loops for stdout/stderr for process {id}");
    LogThreadCount();

    await Task.WhenAll(process.WaitForExitAsync(), outputTask, errorTask);

    Log($"Process {id} output: {CleanupOutput(outputBuilder.ToString())}");
}

Task StartOutputWriter(ChannelReader<(int id, string message)> reader)
{
    return Task.Run(async () =>
    {
        await foreach (var (id, message) in reader.ReadAllAsync())
        {
            Log($"From process {id, 3}: {message}");
        }
    });
}