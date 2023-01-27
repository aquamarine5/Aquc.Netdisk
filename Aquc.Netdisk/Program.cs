using Aquc.AquaUpdater;
using Aquc.AquaUpdater.Pvder;
using Aquc.Netdisk.Aliyunpan;
using Aquc.Netdisk.Bilibili;
using Microsoft.Win32;
using Serilog;
using System.CommandLine;
using System.IO.Compression;

namespace Aquc.Netdisk;

internal class Program
{
    static void Main(string[] args)
    {
        if(!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "log"))){
            Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "log"));
        }
        using var logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "log", $"{DateTime.Today:yy-MM-dd HH-mm-ss}.log"))
            .CreateLogger();
        Log.Logger= logger;
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
        var register = new Command("register","Register necessary information to system to make sure it will use currectly.");
        var root = new RootCommand()
        {
            register,
            upload
        };
        
        register.SetHandler(() =>
        {
            var everyRegistry = Registry.CurrentUser.OpenSubKey("Software")?.OpenSubKey("Classes")?.OpenSubKey("*");
            var shellRegistry = everyRegistry!.OpenSubKey("shell", true) ?? everyRegistry!.CreateSubKey("shell", true);
            if (!shellRegistry!.GetSubKeyNames().Contains("Aquc.NetdiskUploader"))
            {
                var uploadRegistry = shellRegistry.CreateSubKey("Aquc.NetdiskUploader");
                uploadRegistry.SetValue("", "上传...");
                var commandRegistry = uploadRegistry.CreateSubKey("command");
                commandRegistry.SetValue("", $"\"{Environment.ProcessPath}\" upload \"%1\"");
                Log.Information("Successfully register.");
            }
            else
            {
                Log.Warning("Register failed. RegisterKey is already existed.");
            }
            // fix
            var _ = new Launch();
            SubscriptionController.RegisterSubscription(new SubscribeOption()
            {
                Args = "221821283",
                Directory = AppContext.BaseDirectory,
                Key = "Aquc.Netdisk",
                Provider = "bilibilimsgpvder",
                Program = Environment.ProcessPath,
                Version = Environment.Version.ToString(),
            });
        });

        upload.SetHandler(async (str) => {
            /*
            var zipPath = Path.Combine(AppContext.BaseDirectory, DateTime.Now.ToString("yy-MM-dd HH-mm-ss")) + ".zip";
            ZipFile.CreateFromDirectory(str, zipPath);
            */
            new AliyunpanNetdisk(
                new FileInfo(Path.Combine(AppContext.BaseDirectory, "aliyunpan.exe")),
                await BilibiliMsgPvder.Get("221831529")).Upload(str, "/upload").Wait();
            Log.Information("Upload successfully");
        }, uploadFileArg);
        root.Invoke(args);
    }
}