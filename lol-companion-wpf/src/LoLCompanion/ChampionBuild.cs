using System;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace LoLCompanion
{
    public class ChampionBuild
    {
        [JsonPropertyName("championName")]
        public string ChampionName { get; set; } = "";

        [JsonPropertyName("role")]
        public string Role { get; set; } = "";

        [JsonPropertyName("itemIds")]
        public List<long> ItemIds { get; set; }

        [JsonPropertyName("runeIds")]
        public List<long> RuneIds { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        public ChampionBuild()
        {
            ItemIds = new List<long>();
            RuneIds = new List<long>();
            Success = false;
        }
    }
}
