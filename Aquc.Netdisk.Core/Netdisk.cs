namespace Aquc.Netdisk.Core;

public interface INetdisk:IUploadNetdisk,IDownloadNetdisk
{
    
}
public interface IUploadNetdisk
{
    void Upload(string file,string toDirectory);
}
public interface IDownloadNetdisk
{
    string Download(string path,params object[] args);
}
public interface IMessageProvider
{
    string Get(params object[] args);
}