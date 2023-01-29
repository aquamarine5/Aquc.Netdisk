using Aquc.AquaUpdater;
using Aquc.AquaUpdater.Pvder;
using Aquc.Netdisk.Aliyunpan;
using Aquc.Netdisk.Bilibili;
using Huanent.Logging.File;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Win32;
using System.CommandLine;
using System.IO.Compression;
using System.Reflection;

namespace Aquc.Netdisk;

internal class NetdiskProgram
{
    static void Main(string[] args)
    {
        using IHost host = new HostBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddSimpleConsole((options) => { options.UseUtcTimestamp = true; });
                logging.AddFile();
            })
            
            .ConfigureAppConfiguration(builder =>
            {
                builder.AddJsonFile("Aquc.AquaUpdater.config.json");
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton<UpdaterService>();
                services.AddScoped(container => {
                    var token = BilibiliMsgPvder.Get("221831529");
                    token.Wait();
                    return new AliyunpanNetdisk(
                        new FileInfo(Path.Combine(AppContext.BaseDirectory, "aliyunpan.exe")),
                        token.Result,
                        container.GetRequiredService<ILogger<AliyunpanNetdisk>>());
                });
            })
            .Build();
        var _logger = host.Services.GetRequiredService<ILogger<NetdiskProgram>>();

        var uploadFileArg = new Argument<string>("file or directory path", parse: (value) =>
        {
            var v = value.Tokens.Single().Value;
            if (!File.Exists(v))
            {
                if (!Directory.Exists(v))
                {
                    value.ErrorMessage = "It must be a available file or directory path.";
                    return string.Empty;
                }
            }
            return v;
        })
        {
            Arity = ArgumentArity.ExactlyOne
        };
        var upload = new Command("upload", "Upload a file or a directory as a zip file.")
        {
            uploadFileArg
        };
        var register = new Command("register", "Register necessary information to system to make sure it will use currectly.");
        var root = new RootCommand()
        {
            register,
            upload
        };

        register.SetHandler(() =>
        {
            var everyRegistry = Registry.CurrentUser.OpenSubKey("Software")?.OpenSubKey("Classes")?.OpenSubKey("*");
            var shellRegistry = everyRegistry!.OpenSubKey("shell", true) ?? everyRegistry!.CreateSubKey("shell", true);
            if (!shellRegistry!.GetSubKeyNames().Contains("Aquc.Netdisk"))
            {
                var uploadRegistry = shellRegistry.CreateSubKey("Aquc.Netdisk");
                uploadRegistry.SetValue("", "上传...");
                var commandRegistry = uploadRegistry.CreateSubKey("command");
                commandRegistry.SetValue("", $"\"{Environment.ProcessPath}\" upload \"%1\"");
                _logger.LogInformation("Successfully register.");
            }
            else
            {
                _logger.LogWarning("Register failed. RegisterKey is already existed.");
                return;
            }
            //fix
            var _ = new Launch();

            host.Services.GetRequiredService<UpdaterService>().RegisterSubscription(new SubscribeOption()
            {
                Args = "221821283",
                Directory = AppContext.BaseDirectory,
                Key = "Aquc.Netdisk",
                Provider = "bilibilimsgpvder",
                Program = Environment.ProcessPath,
                Version = Assembly.GetExecutingAssembly().GetName().Version!.ToString(),
            });
            Launch.UpdateLaunchConfig();
            
        });

        upload.SetHandler(async (str) => {
            /*
            var zipPath = Path.Combine(AppContext.BaseDirectory, DateTime.Now.ToString("yy-MM-dd HH-mm-ss")) + ".zip";
            ZipFile.CreateFromDirectory(str, zipPath);
            */
            await host.Services.GetRequiredService<AliyunpanNetdisk>().Upload(str, ".");
            _logger.LogInformation("Upload successfully");
        }, uploadFileArg);
        root.Invoke(args);
        
    }
}