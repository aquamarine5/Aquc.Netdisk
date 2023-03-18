using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aquc.Netdisk.Mail
{
    public class MailProgram
    {
        static async Task Main(string[] args)
        {
            var register = new Command("register");
            var root = new RootCommand()
            {
                register
            };
            register.SetHandler(() =>
            {

            });
            await root.InvokeAsync(args);
        }
    }
}
