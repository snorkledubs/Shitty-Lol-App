using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LoLCompanion
{
    public class LeagueApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly HttpClient _lcuClient;

        public LeagueApiClient()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
            var lcuHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };
            _lcuClient = new HttpClient(lcuHandler);
        }

        public Task InitializeAsync()
        {
            DebugUtil.LogDebug("[API] Initialized");
            return Task.CompletedTask;
        }

        private string _lastPort = "";
        private string _lastPassword = "";

        public void UpdateLcuCredentials(string port, string password)
        {
            if (_lastPort == port && _lastPassword == password)
                return;

            _lastPort = port;
            _lastPassword = password;

            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"riot:{password}"));
            _lcuClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
            _lcuClient.BaseAddress = new Uri($"https://127.0.0.1:{port}/");
            DebugUtil.LogDebug($"[LCU] Credentials updated. Port: {port}");
        }

        public async Task<HttpResponseMessage> GetAsync(string endpoint)
        {
            try
            {
                return await _lcuClient.GetAsync(endpoint);
            }
            catch (Exception ex)
            {
                DebugUtil.LogDebug($"[API] GetAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task<HttpResponseMessage> PatchAsync(string endpoint, HttpContent content)
        {
            try
            {
                var contentStr = await content.ReadAsStringAsync();
                DebugUtil.LogDebug($"[API-PATCH] Endpoint: {endpoint}");
                DebugUtil.LogDebug($"[API-PATCH] Payload: {contentStr}");
                
                var request = new HttpRequestMessage(HttpMethod.Patch, endpoint)
                {
                    Content = new StringContent(contentStr, Encoding.UTF8, "application/json")
                };
                var response = await _lcuClient.SendAsync(request);
                
                var responseStr = await response.Content.ReadAsStringAsync();
                DebugUtil.LogDebug($"[API-PATCH] Response Status: {response.StatusCode}");
                if (!string.IsNullOrEmpty(responseStr))
                {
                    DebugUtil.LogDebug($"[API-PATCH] Response Body: {responseStr}");
                }
                
                return response;
            }
            catch (Exception ex)
            {
                DebugUtil.LogDebug($"[API] PatchAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> SwapBenchChampionAsync(int championId)
        {
            try
            {
                var endpoint = $"lol-champ-select/v1/session/bench/swap/{championId}";
                DebugUtil.LogDebug($"[SWAP] Attempting to swap bench champion ID: {championId}");
                
                var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent("", Encoding.UTF8, "application/json")
                };
                
                var response = await _lcuClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    DebugUtil.LogDebug($"[SWAP] Successfully swapped champion {championId}");
                    return true;
                }
                else
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    DebugUtil.LogDebug($"[SWAP] Failed to swap champion. Status: {response.StatusCode}, Body: {responseBody}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                DebugUtil.LogDebug($"[SWAP] Error swapping bench champion: {ex.Message}");
                return false;
            }
        }

        public async Task<ChampionBuild> GetChampionBuildAsync(string championName, string role = "")
        {
            var build = new ChampionBuild();
            build.ChampionName = championName;
            build.Role = role;

            // Fallback build
            build.ItemIds = new List<long> { 3031, 3046, 3089, 3135, 3139, 3157 };
            build.RuneIds = new List<long> { 8005, 9111, 9104, 8014, 8224, 8226, 5008, 5005, 5003 };
            build.Success = true;

            return build;
        }

        public async Task<bool> ImportBuildToClientAsync(ChampionBuild build, int championId, string championName)
        {
            if (_lcuClient.BaseAddress == null)
            {
                DebugUtil.LogDebug("[LCU] LCU client not initialized. Cannot import build.");
                return false;
            }

            try
            {
                DebugUtil.LogDebug($"[LCU] Starting build import for {championName}");

                // Step 1: Import Runes
                bool runesSuccess = await ImportRunesAsync(build);

                // Step 2: Import Items
                bool itemsSuccess = await ImportItemSetAsync(build, championId, championName);

                return runesSuccess && itemsSuccess;
            }
            catch (Exception ex)
            {
                DebugUtil.LogDebug($"[LCU] CRITICAL: Failed to import build for {championName}. Exception: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> ImportRunesAsync(ChampionBuild build)
        {
            try
            {
                var response = await _lcuClient.GetAsync("lol-perks/v1/pages");
                if (!response.IsSuccessStatusCode)
                {
                    DebugUtil.LogDebug("[LCU] Failed to get current rune pages.");
                    return false;
                }

                var content = await response.Content.ReadAsStringAsync();
                var pages = JsonDocument.Parse(content).RootElement;

                foreach (var page in pages.EnumerateArray())
                {
                    if (page.TryGetProperty("name", out var nameElem) && nameElem.GetString() == "LoLCompanion")
                    {
                        if (page.TryGetProperty("id", out var idElem) && idElem.TryGetInt64(out var pageId))
                        {
                            DebugUtil.LogDebug($"[LCU] Deleting existing rune page with ID: {pageId}");
                            await _lcuClient.DeleteAsync($"lol-perks/v1/pages/{pageId}");
                        }
                    }
                }

                var allRunes = build.RuneIds.Where(id => id >= 8000 && id < 10000).ToList();
                var statShards = build.RuneIds.Where(id => id >= 5000 && id < 6000).ToList();
                
                DebugUtil.LogDebug($"[LCU] Runes: {allRunes.Count}, Shards: {statShards.Count}");

                if (allRunes.Count < 6)
                {
                    DebugUtil.LogDebug($"[LCU] Not enough runes. Need at least 6, got {allRunes.Count}");
                    return false;
                }

                var primaryStyleId = GetRuneStyleId(allRunes[0]);
                int secondaryStyleId = 0;
                for (int i = 4; i < allRunes.Count && i < 6; i++)
                {
                    var style = GetRuneStyleId(allRunes[i]);
                    if (style != primaryStyleId && style != 0)
                    {
                        secondaryStyleId = style;
                        break;
                    }
                }

                DebugUtil.LogDebug($"[LCU] Primary style: {primaryStyleId}, Secondary style: {secondaryStyleId}");

                var selectedPerkIds = allRunes.Take(6).Concat(statShards.Take(3)).ToList();
                
                var newPage = new
                {
                    name = "LoLCompanion",
                    primaryStyleId,
                    subStyleId = secondaryStyleId,
                    selectedPerkIds
                };

                var jsonPayload = JsonSerializer.Serialize(newPage);
                DebugUtil.LogDebug($"[LCU] Creating rune page");
                var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var postResponse = await _lcuClient.PostAsync("lol-perks/v1/pages", httpContent);

                if (postResponse.IsSuccessStatusCode)
                {
                    DebugUtil.LogDebug("[LCU] Successfully created rune page.");
                    return true;
                }
                else
                {
                    var error = await postResponse.Content.ReadAsStringAsync();
                    DebugUtil.LogDebug($"[LCU] Failed to create rune page. Status: {postResponse.StatusCode}, Error: {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                DebugUtil.LogDebug($"[LCU] Error importing runes: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> ImportItemSetAsync(ChampionBuild build, int championId, string championName)
        {
            try
            {
                var summonerId = await GetCurrentSummonerId();
                if (summonerId == 0)
                {
                    DebugUtil.LogDebug("[LCU] Could not get current summoner ID.");
                    return false;
                }

                var coreItems = build.ItemIds.Take(3).ToList();
                var situationalItems = build.ItemIds.Skip(3).ToList();

                var itemSet = new
                {
                    title = "LoLCompanion",
                    type = "custom",
                    map = "any",
                    mode = "any",
                    associatedMaps = new int[] { 11, 12 },
                    associatedChampions = new int[] { championId },
                    blocks = new[]
                    {
                        new
                        {
                            type = "Core Build",
                            items = coreItems.Select(id => new { id = id.ToString(), count = 1 }).ToArray()
                        },
                        new
                        {
                            type = "Situational",
                            items = situationalItems.Select(id => new { id = id.ToString(), count = 1 }).ToArray()
                        }
                    }
                };

                var payload = new { itemSet };
                var jsonPayload = JsonSerializer.Serialize(payload);
                DebugUtil.LogDebug($"[LCU] Item set payload being sent");
                var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var url = $"lol-item-sets/v1/item-sets/{summonerId}/sets";
                DebugUtil.LogDebug($"[LCU] Sending to: {url}");

                var getResponse = await _lcuClient.GetAsync(url);
                if (getResponse.IsSuccessStatusCode)
                {
                    var existingSetsContent = await getResponse.Content.ReadAsStringAsync();
                    var existingSetsDoc = JsonDocument.Parse(existingSetsContent);
                    if (existingSetsDoc.RootElement.TryGetProperty("itemSets", out var itemSetsArray))
                    {
                        foreach (var set in itemSetsArray.EnumerateArray())
                        {
                            if (set.TryGetProperty("title", out var titleElem) && titleElem.GetString() == "LoLCompanion" &&
                                set.TryGetProperty("associatedChampions", out var champs))
                            {
                                var champIds = champs.EnumerateArray().Select(c => c.GetInt32()).ToList();
                                if (champIds.Contains(championId) && set.TryGetProperty("uid", out var uidElem))
                                {
                                    var uid = uidElem.GetString();
                                    DebugUtil.LogDebug($"[LCU] Deleting existing set with UID: {uid}");
                                    await _lcuClient.DeleteAsync($"{url}/{uid}");
                                }
                            }
                        }
                    }
                }

                DebugUtil.LogDebug("[LCU] Creating new item set.");
                var postResponse = await _lcuClient.PostAsync(url, httpContent);

                if (postResponse.IsSuccessStatusCode)
                {
                    DebugUtil.LogDebug("[LCU] Successfully created item set.");
                    return true;
                }
                else
                {
                    var error = await postResponse.Content.ReadAsStringAsync();
                    DebugUtil.LogDebug($"[LCU] Failed to create item set. Status: {postResponse.StatusCode}, Error: {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                DebugUtil.LogDebug($"[LCU] Error importing items: {ex.Message}");
                return false;
            }
        }

        private async Task<long> GetCurrentSummonerId()
        {
            try
            {
                var response = await _lcuClient.GetAsync("lol-summoner/v1/current-summoner");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var summoner = JsonDocument.Parse(content).RootElement;
                    if (summoner.TryGetProperty("summonerId", out var idElement))
                    {
                        DebugUtil.LogDebug($"[LCU] Got Summoner ID: {idElement.GetInt64()}");
                        return idElement.GetInt64();
                    }
                }
                DebugUtil.LogDebug("[LCU] Failed to get summoner profile.");
                return 0;
            }
            catch (Exception ex)
            {
                DebugUtil.LogDebug($"[LCU] Error getting summoner ID: {ex.Message}");
                return 0;
            }
        }

        private int GetRuneStyleId(long runeId)
        {
            if (runeId == 8410) return 8300;
            if (runeId == 8009 || runeId == 8014 || runeId == 8017 || runeId == 8299) return 8000;
            
            if (runeId >= 8000 && runeId < 8100) return 8000;
            if (runeId >= 8100 && runeId < 8200) return 8100;
            if (runeId >= 8200 && runeId < 8300) return 8200;
            if (runeId >= 8300 && runeId < 8400) return 8300;
            if (runeId >= 8400 && runeId < 8500) return 8400;
            
            if (runeId >= 9101 && runeId <= 9111) return 8000;
            if (runeId >= 9103 && runeId <= 9105) return 8000;
            
            return 0;
        }

        public async Task<List<int>> GetAvailableSwapChampions()
        {
            try
            {
                var response = await _lcuClient.GetAsync("lol-champ-select/v1/session");
                if (!response.IsSuccessStatusCode)
                {
                    DebugUtil.LogDebug("[SWAP] Failed to get session");
                    return new List<int>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var session = JsonDocument.Parse(content).RootElement;

                var benchChampions = new List<int>();
                if (session.TryGetProperty("benchChampions", out var benchElement))
                {
                    foreach (var champ in benchElement.EnumerateArray())
                    {
                        if (champ.TryGetProperty("championId", out var champIdElem))
                        {
                            benchChampions.Add(champIdElem.GetInt32());
                        }
                    }
                }

                return benchChampions;
            }
            catch (Exception ex)
            {
                DebugUtil.LogDebug($"[SWAP] Error getting available champions: {ex.Message}");
                return new List<int>();
            }
        }
    }
}
