using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace LoLCompanion
{
    public partial class MainWindow : Window
    {
        private LeagueApiClient _apiClient;
        private LockfileManager _lockfileManager;
        private ReadyCheckWatcher _readyCheckWatcher;
        private ChampionData _championData;
        private DispatcherTimer _gameStateTimer;
        private bool _wasInGame = false;
        private bool _isConnected = false;

        public MainWindow()
        {
            InitializeComponent();
            this.Title = "LoL Companion";
            this.Height = 600;
            this.Width = 350;

            _championData = new ChampionData();
            _apiClient = new LeagueApiClient();
            _lockfileManager = new LockfileManager();
            _readyCheckWatcher = new ReadyCheckWatcher(_apiClient);

            _gameStateTimer = new DispatcherTimer();
            _gameStateTimer.Interval = TimeSpan.FromSeconds(5);
            _gameStateTimer.Tick += async (s, e) => await CheckGameState();

            StartWatching();
        }

        private async void BuildTab_Click(object sender, RoutedEventArgs e)
        {
            // Just for future use if you want to expand the UI
        }

        private async void BenchTab_Click(object sender, RoutedEventArgs e)
        {
            // Just for future use if you want to expand the UI
        }

        private async void SettingsTab_Click(object sender, RoutedEventArgs e)
        {
            // Just for future use if you want to expand the UI
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async Task CheckGameState()
        {
            try
            {
                var response = await _apiClient.GetAsync("lol-gameflow/v1/gameflow-phase");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var phase = System.Text.Json.JsonDocument.Parse(content).RootElement.GetString();

                    if (phase == "InGame")
                    {
                        _wasInGame = true;
                    }
                    else if (_wasInGame && phase == "Lobby")
                    {
                        _wasInGame = false;
                        ResetBuild();
                    }
                }
            }
            catch { }
        }

        private void ResetBuild()
        {
            ChampionStatus.Text = "No champion selected";
            CoreItems.Text = "";
            OptionalItems.Text = "";
            RunesDisplay.Text = "";
        }

        private async Task MonitorChampionSelect()
        {
            while (true)
            {
                try
                {
                    var response = await _apiClient.GetAsync("lol-champ-select/v1/session");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var session = System.Text.Json.JsonDocument.Parse(content).RootElement;

                        if (session.TryGetProperty("myTeam", out var teamElement))
                        {
                            foreach (var player in teamElement.EnumerateArray())
                            {
                                if (player.TryGetProperty("championId", out var champIdElem))
                                {
                                    int champId = champIdElem.GetInt32();
                                    if (champId > 0)
                                    {
                                        var championName = _championData.GetChampionName(champId);
                                        if (!string.IsNullOrEmpty(championName) && ChampionStatus.Text != championName)
                                        {
                                            ChampionStatus.Text = championName;
                                            await DoImportBuild(champId);
                                        }
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }

                await Task.Delay(500);
            }
        }

        private async Task DoImportBuild(int championId)
        {
            try
            {
                var championName = _championData.GetChampionName(championId);
                var build = await _apiClient.GetChampionBuildAsync(championName);

                if (build != null && build.Success)
                {
                    var coreItems = build.ItemIds.Count > 0 ? string.Join(", ", build.ItemIds.GetRange(0, System.Math.Min(3, build.ItemIds.Count))) : "None";
                    var optionalItems = build.ItemIds.Count > 3 ? string.Join(", ", build.ItemIds.GetRange(3, System.Math.Min(3, build.ItemIds.Count - 3))) : "None";
                    var runes = build.RuneIds.Count > 0 ? string.Join(", ", build.RuneIds.GetRange(0, System.Math.Min(6, build.RuneIds.Count))) : "None";

                    CoreItems.Text = coreItems;
                    OptionalItems.Text = optionalItems;
                    RunesDisplay.Text = runes;

                    var port = _lockfileManager.GetClientPort();
                    var password = _lockfileManager.GetClientPassword();

                    if (!string.IsNullOrEmpty(port) && !string.IsNullOrEmpty(password))
                    {
                        _apiClient.UpdateLcuCredentials(port, password);
                        await _apiClient.ImportBuildToClientAsync(build, championId, championName);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugUtil.LogDebug($"Error importing build: {ex.Message}");
            }
        }

        private async void StartWatching()
        {
            try
            {
                await _lockfileManager.WatchForClientAsync();

                var port = _lockfileManager.GetClientPort();
                var password = _lockfileManager.GetClientPassword();

                if (!string.IsNullOrEmpty(port) && !string.IsNullOrEmpty(password))
                {
                    _apiClient.UpdateLcuCredentials(port, password);
                    _isConnected = true;
                    StatusText.Text = "League Client: Connected";
                    _gameStateTimer.Start();
                    
                    _ = MonitorChampionSelect();
                }
            }
            catch (Exception ex)
            {
                DebugUtil.LogDebug($"Error starting watchers: {ex.Message}");
            }
        }
    }
}




