using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LoLCompanion
{
    public class LockfileManager
    {
        private const string LockfileDir = @"C:\Riot Games\League of Legends";
        private string _clientPort = "";
        private string _clientPassword = "";

        public async Task WatchForClientAsync()
        {
            while (true)
            {
                try
                {
                    var lockfilePath = Path.Combine(LockfileDir, "lockfile");
                    if (File.Exists(lockfilePath))
                    {
                        var content = File.ReadAllText(lockfilePath);
                        var parts = content.Split(':');
                        if (parts.Length >= 5)
                        {
                            _clientPort = parts[2];
                            _clientPassword = parts[3];
                            DebugUtil.LogDebug($"[LOCKFILE] Found client: Port={_clientPort}");
                        }
                    }
                }
                catch { }

                await Task.Delay(5000);
            }
        }

        public string GetClientPort()
        {
            return _clientPort;
        }

        public string GetClientPassword()
        {
            return _clientPassword;
        }
    }
}
