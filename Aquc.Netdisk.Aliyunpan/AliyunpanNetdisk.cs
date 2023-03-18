using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Aquc.Netdisk.Aliyunpan;

public class AliyunpanNetdisk:IHostedService
{
    bool readyPrintProgress = false;
    TaskCompletionSource<string>? _taskHandler;
    readonly ILogger<AliyunpanNetdisk> _logger;

    readonly FileInfo aliyunpanFile;
    public readonly string token;
    public AliyunpanNetdisk(FileInfo aliyunpanInit, string token,ILogger<AliyunpanNetdisk> logger)
    {
        aliyunpanFile=aliyunpanInit;
        this.token= token;
        _logger = logger;
    }
    public async Task RegisterUpdateTokenSchtask()
    {
        using var process = new Process()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = "schtasks",
                Arguments = $"/Create /F /SC weekly /D MON /TR \"'{aliyunpanFile.FullName + "' token update -mode 2"}\" /TN \"Aquacore\\Aquc.AquaUpdater.Aliyunpan.UpdateToken\"",
                CreateNoWindow = true
            }
        };
        process.Start();
        await process.WaitForExitAsync();
        _logger.LogInformation("Success schedule aliyunpan-token-update");
    }
    public async Task Upload(string filepath,string toDirectory)
    {
        await LoginWhenLogout();
        _logger.LogInformation("from aliyunpan.exe: \n{}",await RunExecAsync($"upload \"{filepath}\" \"{toDirectory}\" --ow "));

    }
    public async Task<string> Download(string filePath,DirectoryInfo targetDirectory,bool printProgress=true)
    {
        await LoginWhenLogout();
        _taskHandler = new TaskCompletionSource<string>();
        var t = targetDirectory.FullName;
        if (t.EndsWith("\\")) t=t[..^1];
        RunExecIter($"download \"{filePath}\" --ow --saveto \"{t}\"",
            (sender, args) =>
            {
                if (string.IsNullOrEmpty(args.Data)) return;
                if (args.Data.Contains("文件不存在"))
                    throw new FileNotFoundException(filePath);
                if (args.Data.Contains("下载开始") && printProgress)
                    readyPrintProgress = true;
                if (readyPrintProgress)
                    _logger.LogInformation("{data}",args.Data);
            },
            (sender, args) =>
            {
                readyPrintProgress = false;
                _taskHandler.TrySetResult(Path.Combine(targetDirectory.FullName,Path.GetFileName(filePath)));
            });
        return await _taskHandler.Task;
    }
    public async Task LoginWhenLogout()
    {
        if (!await IsLogged())
        {
            await Login();
        }
    }
    public async Task Login()
    {
        var result = await RunExecAsync($"login -RefreshToken {token}");
        if (result.Contains("登录失败"))
        {
            _logger.LogError("Failed to login aliyunpan. {response}", result);
            throw new InvalidDataException(result);
        }
        _logger.LogInformation("Aliyunpan login successfully.");
    }
    public async Task<bool> IsLogged()
    {
        return (await RunExecAsync("loglist")).AsEnumerable().Count(c=>c=='\n')>2;
    }
    protected async Task<string> RunExecAsync(string args)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = aliyunpanFile.FullName,
                Arguments = args,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding=Encoding.UTF8
            }
        };
        process.Start();
        _logger.LogInformation("Do aliyunpan interaction start: aliyunpan.exe {args}", args);
        await process.WaitForExitAsync();
        _logger.LogInformation("Do aliyunpan interaction finish: aliyunpan.exe {args}",args);
        return process.StandardOutput.ReadToEnd();
    }
    protected void RunExecIter(string args,DataReceivedEventHandler dataReceivedEventHandler,EventHandler exitEventHandler)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = aliyunpanFile.FullName,
                Arguments = args,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.UTF8,
            },
            EnableRaisingEvents= true
        };
        process.OutputDataReceived += dataReceivedEventHandler;
        process.Exited += exitEventHandler;
        process.Start();
        _logger.LogInformation("Do aliyunpan iter interaction start: aliyunpan.exe {args}", args);
        process.BeginOutputReadLine();
        process.WaitForExit();
        _logger.LogInformation("Do aliyunpan iter interaction finish: aliyunpan.exe {args}", args);
        process.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}