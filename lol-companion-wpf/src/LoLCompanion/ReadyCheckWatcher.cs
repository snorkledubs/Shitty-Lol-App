using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace LoLCompanion
{
    public class ReadyCheckWatcher
    {
        private LeagueApiClient _apiClient;
        private bool _wasInChampSelect = false;

        public ReadyCheckWatcher(LeagueApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task Start(int port, string password)
        {
            while (true)
            {
                try
                {
                    var response = await _apiClient.GetAsync("lol-champ-select/v1/session");
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        _wasInChampSelect = true;
                    }
                    else if (_wasInChampSelect && response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _wasInChampSelect = false;
                        DebugUtil.LogDebug("[CHAMP-SELECT] Game has started, keeping build visible");
                    }
                }
                catch { }

                await Task.Delay(1000);
            }
        }
    }
}
