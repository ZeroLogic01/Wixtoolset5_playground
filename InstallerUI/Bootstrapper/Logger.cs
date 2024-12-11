using System;
using Newtonsoft.Json;
using WixToolset.BootstrapperApplicationApi;

namespace Microsoft.Tools.WindowsInstallerXml.Bootstrapper
{
    public static class Logger
    {
        public static void LogEvent(this Engine engine, string eventName, EventArgs arguments = null)
        {
            engine.Log(
                LogLevel.Verbose,
                arguments == null
                    ? string.Format("EVENT: {0}", eventName)
                    : string.Format("EVENT: {0} ({1})",
                        eventName,
                        JsonConvert.SerializeObject(arguments))
            );
        }
    }
}