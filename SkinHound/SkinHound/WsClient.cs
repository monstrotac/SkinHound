using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

    public class WsClient : INotifyPropertyChanged
    {
        private const int ReceiveBufferSize = 390108192;
        private const int PingIntervalSeconds = 20;

        private ClientWebSocket webSocket;
        private CancellationTokenSource cancellationTokenSource;
        private TextBlock block;
        //This is used to handle null values.
        JsonSerializerSettings settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
        private List<SaleFeedActivity> saleFeed = new List<SaleFeedActivity>();
        private ObservableCollection<SaleFeedActivity> _displayedSaleFeed;
        public ObservableCollection<SaleFeedActivity> DisplayedSaleFeed
        {
            get { return _displayedSaleFeed; }
            set
            {
                _displayedSaleFeed = value;
                OnPropertyChanged(nameof(DisplayedSaleFeed));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
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
                            // Respond with a pong frame containing the same payload
                            var pongPayload = System.Text.Encoding.UTF8.GetBytes("3");
                            await webSocket.SendAsync(pongPayload, WebSocketMessageType.Text, true, CancellationToken.None);
                            break;
                        case "40":
                            break;
                        case "42":
                            if (message.Contains("\"operational\""))
                                await SendMessage("42[\"saleFeedJoin\",{\"appid\":730,\"currency\":\"CAD\",\"locale\":\"en\"}]");
                            else if (message.Contains("saleFeed") && message != null)
                            {
                                string formatedMsg = message;
                                formatedMsg = formatedMsg.Remove(formatedMsg.Length - 1);
                                formatedMsg = formatedMsg.Replace("42[\"saleFeed\",", "");
                                saleFeed = JsonConvert.DeserializeObject<List<SaleFeedActivity>>(formatedMsg, settings);
                                await UpdateSaleActivity(saleFeed);
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
        //This method takes care of handling the modifications made to the displayedSaleFeed
        private async Task UpdateSaleActivity(List<SaleFeedActivity> saleFeed)
        {
            foreach (SaleFeedActivity sale in saleFeed)
            {
                if (DisplayedSaleFeed.Count == 50)
                {
                    DisplayedSaleFeed.RemoveAt(0);
                }
                DisplayedSaleFeed.Add(sale);
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
