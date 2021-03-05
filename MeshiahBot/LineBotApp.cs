using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Line.Messaging;
using Line.Messaging.Webhooks;
using MeshiahBot.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace MeshiahBot
{
    public class LineBotApp : WebhookApplication
    {
        private LineMessagingClient messagingClient { get; }
        AppSettings appSettings;

        private string[] descriprionPettern = { "説明", "使い方", "どう", "なに", "何" };

        public LineBotApp(LineMessagingClient lineMessagingClient, AppSettings appSettings)
        {
            this.messagingClient = lineMessagingClient;
            this.appSettings = appSettings;
        }

        #region Handlers

        // 受信メッセージごとの分岐処理(現状テキストのみ)
        protected override async Task OnMessageAsync(MessageEvent ev)
        {
            switch (ev.Message.Type)
            {
                case EventMessageType.Text:
                    await HandleTextAsync(ev.ReplyToken, ((TextEventMessage)ev.Message).Text, ev.Source.UserId);
                    break;
            }
        }

        // 友達追加時の処理
        protected override async Task OnFollowAsync(FollowEvent ev)
        {
            // LINEのユーザ名を取得
            var userName = "";
            if (!string.IsNullOrEmpty(ev.Source.Id))
            {
                var userProfile = await messagingClient.GetUserProfileAsync(ev.Source.Id);
                userName = userProfile?.DisplayName ?? "";
            }

            // 使い方をリプライ
            await messagingClient.ReplyMessageAsync(ev.ReplyToken, 
                $"{userName}さん! 友達追加ありがとうございます!\n\n" +
                "本Botでは「おすすめ」と呟くと、ここ最近の人気レシピを4品お知らせします。\n" +
                "それ以外の言葉(食材名など)を呟くと、それを使用したレシピを4品お知らせします。");
        }

        #endregion

        // テキスト受信時の処理
        private async Task HandleTextAsync(string replyToken, string userMessage, string userId)
        {
            userMessage = userMessage.ToLower().Replace(" ", "");

            // 「おすすめ」の場合は総合ランキングから上位4品をリプライ
            // 予約されたキーワードの場合は使い方をリプライ
            // それ以外のテキストであれば、検索ワードとして扱う
            if (userMessage == "おすすめ")
            {
                // 楽天APIで取得した総合ランキングの上位4品をリプライ
                await messagingClient.ReplyMessageAsync(replyToken, MakeOverallRecipeRankingList());
            }
            else if (descriprionPettern.Contains(userMessage))
            {
                // 使い方をリプライ
                await messagingClient.ReplyMessageAsync(replyToken, 
                    "「おすすめ」と呟くと、ここ最近の人気レシピを4品お知らせします。\n" +
                    "それ以外の言葉(食材名など)を呟くと、それを使用したレシピを4品お知らせします。");
            }
            else
            {
                var replyMessageList = new List<ISendMessage>();

                // 受信テキストを検索ワードとしたリクエストURL(楽天レシピ)
                var requestURL = "https://recipe.rakuten.co.jp/search/" + userMessage;

                // リクエストURLからHttpリクエスト結果をGET
                using (var client = new HttpClient())
                using (var stream = await client.GetStreamAsync(new Uri(requestURL)))
                {
                    // リクエスト結果をHTMLへ変換
                    var parser = new HtmlParser();
                    IHtmlDocument document = await parser.ParseDocumentAsync(stream);

                    // 楽天レシピの検索結果内、id=structuredRecipeListのテキスト部分では
                    // ItemListElementという名前で表示対象レシピの情報一覧をJson形式で持っていたため
                    // structuredRecipeListのテキスト部分を対象にスクレイピングし、取得内容をモデル化する
                    var searchResult = JsonSerializer.Deserialize<RakutenRecipeSearchResult.Root[]>(document.GetElementById("structuredRecipeList").TextContent);

                    // 検索結果の上位4品(ItemListElementの上位4品のURL)を取得し、リストに追加
                    // searchResult[0]のitemListElementには楽天レシピトップや検索用のURLが設定されているのみ
                    // searchResult[1]にランキング化された検索結果の情報が入っているため、こちらからURLを取得
                    replyMessageList.Add(new TextMessage($"{userMessage}のレシピはこんなものがあるようです！"));
                    replyMessageList.Add(new TextMessage(searchResult[1].itemListElement[0].url));
                    replyMessageList.Add(new TextMessage(searchResult[1].itemListElement[1].url));
                    replyMessageList.Add(new TextMessage(searchResult[1].itemListElement[2].url));
                    replyMessageList.Add(new TextMessage(searchResult[1].itemListElement[3].url));
                }

                // 結果をリプライ
                await messagingClient.ReplyMessageAsync(replyToken, replyMessageList);
            }
        }

        // 楽天APIを使用して楽天レシピの総合ランキングから上位4品を取得し
        // リプライ用のメッセージリストに追加する処理
        [HttpGet]
        private List<ISendMessage> MakeOverallRecipeRankingList()
        {
            var replyMessageList = new List<ISendMessage>();

            // appsettingsからAPIへのリクエストに使用する各設定を取得
            var apiURL = appSettings.RakutenApiSettings.RecipeCategoryRankingApiURL;
            var appId = appSettings.RakutenApiSettings.AppId;

            // リクエスト用URL
            string requestURL = apiURL + "&format=json" + "&applicationId=" + appId + "&elements=recipeUrl";

            // APIへのリクエストを作成後、結果をGET
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestURL);
            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader streamReader = new StreamReader(response.GetResponseStream());

            // APIからのレスポンスを文字列化(Json)
            string apiRequestResult = streamReader.ReadToEnd();

            // 文字列化したレスポンスをモデル化
            var overallRankingResult = JsonSerializer.Deserialize<OverallRecipeRankingResult.Root>(apiRequestResult);

            // 楽天APIのクレジット
            // LINE上だと画像が大きすぎて見切れるが、加工禁止とのことなのでそのまま掲載
            var creditURL = "https://webservice.rakuten.co.jp/img/credit/200709/credit_22121.gif";
            replyMessageList.Add(new ImageMessage(creditURL, creditURL));

            // モデルからレシピのURLを取り出し、リストに追加
            foreach (var url in overallRankingResult.result)
            {
                replyMessageList.Add(new TextMessage(url.recipeUrl));
            }

            return replyMessageList;
        }
    }
}
