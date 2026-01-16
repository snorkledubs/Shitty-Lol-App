using System;
using System.Collections.Generic;

namespace LoLCompanion
{
    public class ChampionData
    {
        private Dictionary<int, string> _champions = new Dictionary<int, string>
        {
            { 1, "Annie" },
            { 2, "Olaf" },
            { 3, "Galio" },
            { 4, "Twisted Fate" },
            { 5, "Xin Zhao" },
            { 6, "Urgot" },
            { 7, "LeBlanc" },
            { 8, "Vladimir" },
            { 9, "Fiddlesticks" },
            { 10, "Kayle" },
            { 11, "Master Yi" },
            { 12, "Alistar" },
            { 13, "Ryze" },
            { 14, "Sion" },
            { 15, "Sivir" },
            { 16, "Soraka" },
            { 17, "Teemo" },
            { 18, "Tristana" },
            { 19, "Twitch" },
            { 20, "Nunu & Willump" },
            { 21, "Miss Fortune" },
            { 22, "Ashe" },
            { 23, "Tryndamere" },
            { 24, "Jax" },
            { 25, "Mordekaiser" },
            { 26, "Zilean" },
            { 27, "Singed" },
            { 28, "Rammus" },
            { 29, "Blitzcrank" },
            { 30, "Karthus" },
            { 31, "Cho'Gath" },
            { 32, "Amumu" },
            { 33, "Rammus" },
            { 34, "Anivia" },
            { 35, "Shaco" },
            { 36, "Dr. Mundo" },
            { 37, "Skarner" },
            { 38, "Kassadin" },
            { 39, "Nidalee" },
            { 40, "Janna" },
            { 41, "Gangplank" },
            { 42, "Corki" },
            { 43, "Karma" },
            { 44, "Taric" },
            { 45, "Veigar" },
            { 48, "Trundle" },
            { 50, "Swain" },
            { 51, "Caitlyn" },
            { 53, "Blitzcrank" },
            { 54, "Malphite" },
            { 55, "Katarina" },
            { 56, "Nocturne" },
            { 57, "Maokai" },
            { 58, "Renekton" },
            { 59, "Jarvan IV" },
            { 60, "Elise" },
            { 61, "Orianna" },
            { 62, "Wukong" },
            { 63, "Brand" },
            { 64, "Lee Sin" },
            { 67, "Vayne" },
            { 68, "Rumble" },
            { 69, "Cassiopeia" },
            { 72, "Skarner" },
            { 74, "Heimerdinger" },
            { 75, "Nasus" },
            { 76, "Nidalee" },
            { 77, "Udyr" },
            { 78, "Poppy" },
            { 79, "Gragas" },
            { 80, "Pantheon" },
            { 81, "Ezreal" },
            { 82, "Mordekaiser" },
            { 83, "Yorick" },
            { 84, "Akali" },
            { 85, "Kennen" },
            { 86, "Garen" },
            { 89, "Leona" },
            { 90, "Talon" },
            { 91, "Talon" },
            { 92, "Riven" },
            { 96, "Kog'Maw" },
            { 98, "Shen" },
            { 99, "Lux" },
            { 101, "Xerath" },
            { 102, "Shyvana" },
            { 103, "Ahri" },
            { 104, "Graves" },
            { 105, "Fizz" },
            { 106, "Volibear" },
            { 107, "Rengar" },
            { 110, "Varus" },
            { 111, "Nautilus" },
            { 112, "Viktor" },
            { 113, "Sejuani" },
            { 114, "Fiora" },
            { 115, "Ziggs" },
            { 117, "Lulu" },
            { 119, "Draven" },
            { 120, "Hecarim" },
            { 121, "Kha'Zix" },
            { 122, "Darius" },
            { 123, "Thresh" },
            { 126, "Jayce" },
            { 127, "Lissandra" },
            { 131, "Diana" },
            { 133, "Quinn" },
            { 134, "Syndra" },
            { 136, "Aurelion Sol" },
            { 141, "Kayn" },
            { 142, "Zoe" },
            { 143, "Yone" },
            { 145, "Kai'Sa" },
            { 147, "Seraphine" },
            { 150, "Gnar" },
            { 151, "Taliyah" },
            { 152, "Akshan" },
            { 154, "Zac" },
            { 155, "Annie" },
            { 157, "Yasuo" },
            { 158, "Twilight Shroud" },
            { 161, "Vel'Koz" },
            { 163, "Taliyah" },
            { 164, "Camille" },
            { 166, "Yuumi" },
            { 167, "Taric" },
            { 168, "Yuumi" },
            { 169, "Yuumi" },
            { 170, "Yuumi" },
            { 171, "Yuumi" },
            { 172, "Yuumi" },
            { 200, "Ekko" },
            { 201, "Braum" },
            { 202, "Jhin" },
            { 203, "Kindred" },
            { 204, "Evelynn" },
            { 205, "Tahm Kench" },
            { 206, "Illaoi" },
            { 207, "Kha'Zix" },
            { 208, "Kalista" },
            { 209, "Sion" },
            { 210, "Ivern" },
            { 211, "Rakan" },
            { 212, "Xayah" },
            { 213, "Evelynn" },
            { 214, "Kha'Zix" },
            { 215, "Kha'Zix" },
            { 216, "Kha'Zix" },
            { 217, "Kha'Zix" },
            { 218, "Kha'Zix" },
            { 219, "Kha'Zix" },
            { 220, "Kha'Zix" },
            { 221, "Kha'Zix" },
            { 222, "Kha'Zix" },
            { 223, "Kha'Zix" },
            { 224, "Kha'Zix" },
            { 225, "Kha'Zix" },
            { 226, "Kha'Zix" },
            { 227, "Kha'Zix" },
            { 228, "Kha'Zix" },
            { 229, "Kha'Zix" },
            { 230, "Kha'Zix" }
        };

        public ChampionData()
        {
        }

        public Dictionary<int, string> GetAllChampions()
        {
            return _champions;
        }

        public string GetChampionName(int championId)
        {
            if (_champions.TryGetValue(championId, out var name))
            {
                return name;
            }
            return "";
        }

        public int GetChampionId(string championName)
        {
            foreach (var kvp in _champions)
            {
                if (kvp.Value.Equals(championName, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Key;
                }
            }
            return 0;
        }
    }
}
