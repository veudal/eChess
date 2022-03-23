using eChessServer.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace eChess
{
    class GameController
    {
        private static readonly HttpClient client = new HttpClient()
        {
            BaseAddress = new Uri(Constants.ApiBaseAddress)
        };


        public static async void PostMove(Guid gameID, Guid playerGuid, Point currentPos, Point newPos)
        {
            var response = await client.GetAsync("/Game/PostMove?gameID=" + gameID + "&playerGuid=" + playerGuid + "&currentPosX=" + currentPos.X + "&currentPosY=" + currentPos.Y + "&newPosX=" + newPos.X + "&newPosY=" + newPos.Y);
            var content = response.Content.ReadAsStringAsync().Result;
            if (content != "true")
            {

            }

        }
        public static async Task<MoveEntity> ReceiveMove(Guid gameID, Guid playerGuid, BackgroundWorker worker)
        {
            bool received = false;
            while (received == false)
            {
                await Task.Delay(500);
                var response = await client.GetAsync("/Game/ReceiveMove?gameID=" + gameID + "&playerGuid=" + playerGuid).Result.Content.ReadAsStringAsync();
                MoveEntity move = JsonConvert.DeserializeObject<MoveEntity>(response);
                if (move.currentPos != Point.Empty || move.newPos != Point.Empty)
                {
                    received = true;
                    return move;
                }
            }
            return new MoveEntity();
        }
    }
}