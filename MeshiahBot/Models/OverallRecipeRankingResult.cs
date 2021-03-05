using System.Collections.Generic;

namespace MeshiahBot.Models
{
    // 楽天レシピの総合ランキングからAPIで取得した結果を扱う為のMedelクラス
    public class OverallRecipeRankingResult
    {
        public class Result
        {
            public string recipeUrl { get; set; }
        }

        public class Root
        {
            public List<Result> result { get; set; }
        }

    }
}
