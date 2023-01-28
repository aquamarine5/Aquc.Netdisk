﻿using Aquc.Netdisk.Core;
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

    public async Task Upload(string filepath,string toDirectory)
    {
        await LoginWhenNLI();
        await RunExecAsync($"upload -ow \"{filepath}\" \"{toDirectory}\"");

    }
    public async Task<string> Download(string filePath,DirectoryInfo targetDirectory,bool printProgress=true)
    {
        await LoginWhenNLI();
        _taskHandler = new TaskCompletionSource<string>();
        
        RunExecIter($"download -ow \"{filePath}\" --saveto \"{targetDirectory.FullName}\"",
            (sender, args) =>
            {
                if (string.IsNullOrEmpty(args.Data)) return;
                if (args.Data.Contains("文件不存在"))
                    throw new FileNotFoundException(filePath);
                if (args.Data.Contains("下载开始") && printProgress)
                    readyPrintProgress = true;
                if (readyPrintProgress)
                    Console.WriteLine(args.Data);
            },
            (sender, args) =>
            {
                readyPrintProgress = false;
                _taskHandler.TrySetResult(Path.Combine(targetDirectory.FullName,Path.GetFileName(filePath)));
            });
        return await _taskHandler.Task;
    }
    public async Task LoginWhenNLI()
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
            throw new InvalidDataException(result);
        }
        _logger.LogInformation("Aliyunpan login.");
    }
    public async Task<bool> IsLogged()
    {
        // TODO
        return (await RunExecAsync("loglist")).Contains("1");
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
        await process.WaitForExitAsync();
        _logger.LogInformation($"run {args}");
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
        process.BeginOutputReadLine();
        process.WaitForExit();
        _logger.LogInformation($"run {args}");
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