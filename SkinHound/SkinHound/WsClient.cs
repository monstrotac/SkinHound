using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Windows.Storage.Streams;

namespace SkinHound
{

    public class WsClient
    {
        private const int ReceiveBufferSize = 390108192;
        private const int PingIntervalSeconds = 20;

        private ClientWebSocket webSocket;
        private CancellationTokenSource cancellationTokenSource;

        private TextBlock block;

        public WsClient(TextBlock block)
        {
            this.block = block;
        }

        public async Task Connect(string url)
        {
            webSocket = new ClientWebSocket();
            await webSocket.ConnectAsync(new Uri(url), CancellationToken.None);

            cancellationTokenSource = new CancellationTokenSource();
            await StartListening();
        }

        private async Task StartListening()
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var buffer = new ArraySegment<byte>(new byte[ReceiveBufferSize]);
                var receiveResult = await webSocket.ReceiveAsync(buffer, cancellationTokenSource.Token);

                if (receiveResult.MessageType == WebSocketMessageType.Text)
                {
                    var message = System.Text.Encoding.UTF8.GetString(buffer.Array, 0, receiveResult.Count);
                    block.Text = ("Received message: " + message);
                    //We simply acquire the code in the string beforehand.
                    JObject activity;
                    string code = "";
                    foreach (char character in message)
                    {
                        if (character == '[' || character == '{')
                            break;
                        else code += character;
                    }
                    //Handle the code's value
                    switch (code)
                    {
                        case "0":
                            await SendMessage("40");
                            break;
                        case "2":
                            // Received a ping frame (server's "2" ping frame)
                            block.Text = ("Received ping frame. Responding with pong frame.");
                            // Respond with a pong frame containing the same payload
                            var pongPayload = System.Text.Encoding.UTF8.GetBytes("3");
                            await webSocket.SendAsync(pongPayload, WebSocketMessageType.Text, true, CancellationToken.None);
                            break;
                        case "40":
                            break;
                        case "42":
                            if (message.Contains("\"operational\""))
                                await SendMessage("42[\"saleFeedJoin\",{\"appid\":730,\"currency\":\"CAD\",\"locale\":\"en\"}]");
                            else if (message.Contains("saleFeed"))
                            {
                                string formatedMsg = message;
                                formatedMsg = formatedMsg.Remove(formatedMsg.Length - 1);
                                formatedMsg = formatedMsg.Replace("42[\"saleFeed\",", "");
                                activity = JObject.Parse(formatedMsg);
                                //saleFeed.Add(activity);
                            }
                            break;
                    }
                }
                else if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    block.Text = ("Connection closed by the server.");
                    break;
                }
            }
        }

        public async Task SendMessage(string message)
        {
            var buffer = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(message));
            await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async Task CloseConnection()
        {
            cancellationTokenSource.Cancel();
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        }
    }
}
