using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
        private const int MAX_ACTIVITY_DISPLAYED = 500;
        private ClientWebSocket webSocket;
        private CancellationTokenSource cancellationTokenSource;
        private TextBlock block;
        private string webSocketString;
        private bool currentlyListening = false;
        //Filters determined by the checkbox
        private bool untrackedOn = true;
        private bool dealsOn = true;
        private bool desiredOn = true;
        private bool salesOn = true;
        private bool listingOn = true;
        public bool UntrackedOn
        {
            get { return untrackedOn; }
            set
            {
                untrackedOn = value;
                OnPropertyChanged(nameof(untrackedOn));
            }
        }
        public bool DealsOn
        {
            get { return dealsOn; }
            set
            {
                dealsOn = value;
                OnPropertyChanged(nameof(dealsOn));
            }
        }
        public bool DesiredOn
        {
            get { return desiredOn; }
            set
            {
                desiredOn = value;
                OnPropertyChanged(nameof(desiredOn));
            }
        }
        public bool SalesOn
        {
            get { return salesOn; }
            set
            {
                salesOn = value;
                OnPropertyChanged(nameof(salesOn));
            }
        }
        public bool ListingOn
        {
            get { return listingOn; }
            set
            {
                listingOn = value;
                OnPropertyChanged(nameof(listingOn));
            }
        }
        //This is used to handle null values.
        JsonSerializerSettings settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
        private List<SaleFeedActivity> saleFeed = new List<SaleFeedActivity>();
        private ObservableCollection<SaleFeedItem> _displayedSaleFeed;
        public ObservableCollection<SaleFeedItem> DisplayedSaleFeed
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
        public WsClient(TextBlock textBlock)
        {
            block = textBlock;
            DisplayedSaleFeed = new ObservableCollection<SaleFeedItem>();
        }

        public async Task Connect(string url)
        {
            webSocketString = url;
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
                            block.Text = "Connection established";
                            break;
                        case "2":
                            // Received a ping frame (server's "2" ping frame)
                            // Respond with a pong frame containing the payload "3"
                            var pongPayload = System.Text.Encoding.UTF8.GetBytes("3");
                            block.Text = "Ping received, responding with Pong";
                            await webSocket.SendAsync(pongPayload, WebSocketMessageType.Text, true, CancellationToken.None);
                            break;
                        case "40":
                            break;
                        case "42":
                            if (message.Contains("\"operational\""))
                            {
                                block.Text = "Steam status operational, now listening";
                                await SendMessage($"42[\"saleFeedJoin\",{{\"appid\":730,\"currency\":\"{SkinHoundConfiguration.Currency}\",\"locale\":\"en\"}}]");
                                currentlyListening = true;
                            }
                            else if (message.Contains("saleFeed") && message != null)
                            {
                                string formatedMsg = message;
                                formatedMsg = formatedMsg.Remove(formatedMsg.Length - 1);
                                formatedMsg = formatedMsg.Replace("42[\"saleFeed\",", "");
                                JObject obj = JObject.Parse(formatedMsg);
                                saleFeed = JsonConvert.DeserializeObject<List<SaleFeedActivity>>(obj["sales"].ToString(), settings);
                                await UpdateSaleActivity(saleFeed, obj["eventType"].ToString());
                            }
                            break;
                    }
                }
                else if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    break;
                }
            }
        }
        //This method takes care of handling the modifications made to the displayedSaleFeed
        private async Task UpdateSaleActivity(List<SaleFeedActivity> saleFeed, string eventType)
        {
            foreach (SaleFeedActivity sale in saleFeed)
            {
                if (DisplayedSaleFeed.Count == MAX_ACTIVITY_DISPLAYED)
                {
                    DisplayedSaleFeed.RemoveAt(MAX_ACTIVITY_DISPLAYED-1);
                }
                sale.EventType = eventType;
                //We process verifications here before inserting the newest activity.
                if((eventType.Contains("sold") && salesOn) || (eventType.Contains("listed") && listingOn))
                {
                    if(await Utils.VerifyIfDesired(sale.MarketHashName, 0))
                        if(desiredOn)
                        {
                            sale.IsDesired = true;
                            DisplayedSaleFeed.Insert(0, new SaleFeedItem(sale));
                        }
                    if (((1 - sale.SalePrice / sale.SuggestedPrice) * 100) >= SkinHoundConfiguration.Good_Discount_Threshold && sale.SalePrice >= SkinHoundConfiguration.Minimum_Worth_Value)
                    {
                        if (dealsOn)
                            DisplayedSaleFeed.Insert(0, new SaleFeedItem(sale));
                    } else if (untrackedOn)
                        DisplayedSaleFeed.Insert(0, new SaleFeedItem(sale));
                }
            }
        }

        public async Task SendMessage(string message)
        {
            var buffer = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(message));
            await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        public async Task UpdateFilters()
        {
            DisplayedSaleFeed.Clear();
            return;
        }
        public async Task CloseConnection()
        {
            cancellationTokenSource.Cancel();
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            block.Text = "Connection to WebSocket lost, attempting to reconnect";
            await Connect(webSocketString);
            currentlyListening = false;
        }
    }
}
