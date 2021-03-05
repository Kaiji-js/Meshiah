namespace MeshiahBot.Models
{
    // Appsettingsを扱う為のModelクラス
    public class AppSettings
    {
        public LineSettings LineSettings { get; set; }
        public RakutenApiSettings RakutenApiSettings { get; set; }
    }
    public class LineSettings
    {
        public string ChannelSecret { get; set; }
        public string ChannelAccessToken { get; set; }
    }

    public class RakutenApiSettings
    {
        public string RecipeCategoryRankingApiURL { get; set; }
        public string AppId { get; set; }
    }
}
