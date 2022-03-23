using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace eChess
{
    internal class MatchFinder
    {
        private readonly HttpClient client;

        public MatchFinder()
        {
            client = new HttpClient
            {
                BaseAddress = new Uri(Constants.ApiBaseAddress)
            };

        }

        public async Task<GameEntity> FindMatch(Guid playerGuid, string playerName, BackgroundWorker worker)
        {
            var response = await client.GetAsync("/MatchFinder/JoinGame?playerGuid=" + playerGuid + "&playerName=" + playerName).Result.Content.ReadAsStringAsync();
            GameEntity game = JsonConvert.DeserializeObject<GameEntity>(response);
            if (game.GameID == Guid.Empty)
            {
                return await CreateMatch(playerGuid, playerName, worker);
            }
            else
            {
                return game;
            }
        }
        private async Task<GameEntity> CreateMatch(Guid playerGuid, string playerName, BackgroundWorker worker)
        {
            GameEntity game = new GameEntity();
            while (worker.CancellationPending == false)
            {
                await Task.Delay(100);
                var response = await client.GetAsync("/MatchFinder/CreateGame?playerGuid=" + playerGuid + "&playerName=" + playerName).Result.Content.ReadAsStringAsync();
                game = JsonConvert.DeserializeObject<GameEntity>(response);
                if (game != null  && game.GameID != Guid.Empty)
                {
                    return game;
                }
            }
            return game;
        }
    }
}
