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
using Windows.Storage.Streams;

namespace SkinHound
{

    public class WsClient : IDisposable
    {
        private const int bufferSize = 1024;
        readonly byte[] sendBuffer = new byte[bufferSize];
        public int ReceiveBufferSize { get; set; } = 8192;
        private List<JObject> skinportMarketActivity = new List<JObject>();
        public async Task ConnectAsync(string url)
        {
            if (WS != null)
            {
                if (WS.State == WebSocketState.Open) return;
                else WS.Dispose();
            }
            WS = new ClientWebSocket();
            if (CTS != null) CTS.Dispose();
            CTS = new CancellationTokenSource();
            await WS.ConnectAsync(new Uri(url), CTS.Token);
            await Task.Factory.StartNew(ReceiveLoop, CTS.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public async Task DisconnectAsync()
        {
            if (WS is null) return;
            // TODO: requests cleanup code, sub-protocol dependent.
            if (WS.State == WebSocketState.Open)
            {
                CTS.CancelAfter(TimeSpan.FromSeconds(2));
                await WS.CloseOutputAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None);
                await WS.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
            WS.Dispose();
            WS = null;
            CTS.Dispose();
            CTS = null;
        }

        private async Task ReceiveLoop()
        {
            var loopToken = CTS.Token;
            MemoryStream outputStream = null;
            WebSocketReceiveResult receiveResult = null;
            var buffer = new byte[ReceiveBufferSize];
            try
            {
                while (!loopToken.IsCancellationRequested)
                {
                    outputStream = new MemoryStream(ReceiveBufferSize);
                    do
                    {
                        receiveResult = await WS.ReceiveAsync(buffer, CTS.Token);
                        if (receiveResult.MessageType != WebSocketMessageType.Close)
                            outputStream.Write(buffer, 0, receiveResult.Count);
                    }
                    while (!receiveResult.EndOfMessage);
                    if (receiveResult.MessageType == WebSocketMessageType.Close) break;
                    outputStream.Position = 0;
                    ResponseReceived(outputStream);
                }
            }
            catch (TaskCanceledException) { }
            finally
            {
                outputStream?.Dispose();
            }
        }

        private async Task SendMessageAsync(string message, CancellationToken token)
        {
            var messageLength = message.Length;
            var messageCount = (int)Math.Ceiling((double)messageLength / bufferSize);
            for (var i = 0; i < messageCount; i++)
            {
                var offset = bufferSize * i;
                var count = bufferSize;
                var lastMessage = i + 1 == messageCount;
                if (count * (i + 1) > messageLength)
                    count = messageLength - offset;
                var segmentLength = Encoding.UTF8.GetBytes(message, offset, count, sendBuffer, 0);
                var segment = new ArraySegment<byte>(sendBuffer, 0, segmentLength);
                await WS.SendAsync(segment, WebSocketMessageType.Text, lastMessage, token);
            }
        }

        private async void ResponseReceived(Stream inputStream)
        {
            // TODO: handle deserializing responses and matching them to the requests.
            // IMPORTANT: DON'T FORGET TO DISPOSE THE inputStream!
            StreamReader streamReader = new StreamReader(inputStream);
            string strResponse = await streamReader.ReadLineAsync();
            if (strResponse != null)
            {
                //We simply acquire the code in the string beforehand.
                JObject activity;
                string code = "";
                foreach(char character in strResponse)
                {
                    if (character == '[' || character == '{')
                        break;
                    else code += character;
                }
                //Handle the code's value
                switch (code)
                {
                    case "0":
                        await SendMessageAsync("40", CancellationToken.None);
                        break;
                    case "40":
                        break;
                    case "42":
                        if (strResponse.Contains("\"operational\""))
                            await SendMessageAsync("42[\"saleFeedJoin\",{\"appid\":730,\"currency\":\"CAD\",\"locale\":\"en\"}]", CancellationToken.None);
                        else if (strResponse.Contains("saleFeed"))
                        {
                            strResponse = strResponse.Replace("\\", "");
                            strResponse = strResponse.Remove(strResponse.Length - 1);
                            strResponse = strResponse.Replace("42[\"saleFeed\",", "");
                            activity = JObject.Parse(strResponse);
                            skinportMarketActivity.Add(activity);
                        }
                        break;
                }
            }
            await SendMessageAsync("42", CancellationToken.None);
        }

        public void Dispose() => DisconnectAsync().Wait();
        private ClientWebSocket WS;
        private CancellationTokenSource CTS;
        public void FilterMarketActivity()
        {
            List<JObject> newList = new List<JObject>();
            foreach(JObject activity in skinportMarketActivity)
            {
                if (activity["eventType"].ToString() == "listed")
                {
                    newList.Add(activity);
                }
            }
            skinportMarketActivity = newList;
        }
        public List<JObject> GetMarketActivity()
        {
            return skinportMarketActivity;
        }
    }
}
