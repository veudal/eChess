using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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
            try
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
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }
        private async Task<GameEntity> CreateMatch(Guid playerGuid, string playerName, BackgroundWorker worker)
        {
            GameEntity game = new GameEntity();
            try
            {
                while (worker.CancellationPending == false)
                {
                    await Task.Delay(500);
                    var response = await client.GetAsync("/MatchFinder/CreateGame?playerGuid=" + playerGuid + "&playerName=" + playerName).Result.Content.ReadAsStringAsync();
                    game = JsonConvert.DeserializeObject<GameEntity>(response);
                    if (game != null && game.GameID != Guid.Empty)
                    {
                        return game;
                    }
                }

            }
            catch
            {
                return null;
            }
            return game;
        }
    }
}
