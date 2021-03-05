using Line.Messaging;
using Line.Messaging.Webhooks;
using MeshiahBot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Threading.Tasks;

namespace MeshiahBot.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class LineBotController : Controller
    {
        private static LineMessagingClient lineMessagingClient;
        AppSettings appSettings;

        public LineBotController(IOptions<AppSettings> options)
        {
            appSettings = options.Value;
            lineMessagingClient = new LineMessagingClient(appSettings.LineSettings.ChannelAccessToken);
        }

        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]JsonElement req)
        {
            var events = WebhookEventParser.Parse(req.ToString());
            var app = new LineBotApp(lineMessagingClient, appSettings);
            await app.RunAsync(events);
            return new OkResult();
        }
    }
}