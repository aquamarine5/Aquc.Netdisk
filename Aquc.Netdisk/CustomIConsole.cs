using System.CommandLine;
using System.CommandLine.IO;
using System.Runtime.InteropServices;

namespace Aquc.Netdisk;
public class CustomAllocatedConsole : IConsole
{
    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool AllocConsole();
    #region IConsole

    bool IStandardIn.IsInputRedirected => false;

    bool IStandardOut.IsOutputRedirected => false;
    IStandardStreamWriter IStandardOut.Out
    {
        get
        {
            EnsureConsoleIsSetup();
            return _out;
        }
    }

    bool IStandardError.IsErrorRedirected => false;
    IStandardStreamWriter IStandardError.Error
    {
        get
        {
            EnsureConsoleIsSetup();
            return _error;
        }
    }

    #endregion IConsole


    private IStandardStreamWriter _out;
    private IStandardStreamWriter _error;
    private bool _hasConsoleBeenAllocated = false;


    private void EnsureConsoleIsSetup()
    {
        if (_hasConsoleBeenAllocated)
            return;

        AllocConsole();
        //
        // ... plus any steps possibly required to setup the console
        // (like considering possible redirection scenarios and whatnot...)
        //

        //
        // You will also have to account for the situation where the program has been invoked
        // from within an existing console shell, and where therefore the program should attach
        // to the existing console instead of allocating a new one...
        //

        _hasConsoleBeenAllocated = true;

        //
        // If SystemConsole.Out and SystemConsole.Error don't work
        // you will have to set up the output and error writers for
        // your allocated console manually and create respective
        // IStandardStreamWriter instances yourself...
        //
        var sysConsole = new System.CommandLine.IO.SystemConsole();
        _out = sysConsole.Out;
        _error = sysConsole.Error;
    }
}