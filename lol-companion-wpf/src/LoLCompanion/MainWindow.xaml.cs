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
        private bool _autoClickEnabled = false;
        private DispatcherTimer _gameStateTimer;
        private bool _wasInGame = false;

        public MainWindow()
        {
            InitializeComponent();
            this.Title = "LoL Companion";
            this.Height = 600;
            this.Width = 400;

            _championData = new ChampionData();
            _apiClient = new LeagueApiClient();
            _lockfileManager = new LockfileManager();
            _readyCheckWatcher = new ReadyCheckWatcher(_apiClient);

            _gameStateTimer = new DispatcherTimer();
            _gameStateTimer.Interval = TimeSpan.FromSeconds(5);
            _gameStateTimer.Tick += async (s, e) => await CheckGameState();

            InitializeUI();
            StartWatching();
        }

        private void InitializeUI()
        {
            ChampionPanel.Children.Clear();

            foreach (var champion in _championData.GetAllChampions())
            {
                var button = new Button
                {
                    Content = champion.Value,
                    Tag = champion.Key,
                    Width = 80,
                    Height = 80,
                    Margin = new Thickness(5)
                };

                button.Click += (s, e) => OnChampionClick(champion.Key);
                button.MouseEnter += (s, e) => OnChampionHover(champion.Key);

                ChampionPanel.Children.Add(button);
            }
        }

        private async void OnChampionClick(int championId)
        {
            await DoImportBuild(championId);
        }

        private async void OnChampionHover(int championId)
        {
            if (championId > 0)
            {
                var championName = _championData.GetChampionName(championId);
                if (!string.IsNullOrEmpty(championName))
                {
                    BuildDisplay.Text = $"Loading build for {championName}...";
                    await DoImportBuild(championId);
                }
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
                    BuildDisplay.Text = $"{championName}\nItems: {string.Join(", ", build.ItemIds)}\nRunes: {string.Join(", ", build.RuneIds)}";
                    
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
                BuildDisplay.Text = $"Error: {ex.Message}";
            }
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
            BuildDisplay.Text = "Build cleared - ready for next game";
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
                    _readyCheckWatcher.Start(int.Parse(port), password);
                    _gameStateTimer.Start();
                }
            }
            catch (Exception ex)
            {
                DebugUtil.LogDebug($"Error starting watchers: {ex.Message}");
            }
        }

        private void AutoClickToggle_Click(object sender, RoutedEventArgs e)
        {
            _autoClickEnabled = !_autoClickEnabled;
            AutoClickToggle.Content = _autoClickEnabled ? "Auto-Click: ON" : "Auto-Click: OFF";
        }
    }
}




