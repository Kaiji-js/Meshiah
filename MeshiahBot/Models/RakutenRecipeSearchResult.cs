using Newtonsoft.Json;
using System.Collections.Generic;

namespace MeshiahBot.Models
{
    // スクレイピング結果を扱う為のModelクラス
    public class RakutenRecipeSearchResult
    {
        // 検索結果のHTML内 id=structuredRecipeList のテキスト部分の取得結果を入れるクラス
        public class ItemListElement
        {
            public int position { get; set; }
            public string name { get; set; }
            public string item { get; set; }
            [JsonProperty("@type")]
            public string Type { get; set; }
            public string url { get; set; }
        }

        public class Root
        {
            public List<ItemListElement> itemListElement { get; set; }
            [JsonProperty("@context")]
            public string Context { get; set; }
            [JsonProperty("@type")]
            public string Type { get; set; }
        }

    }
}
