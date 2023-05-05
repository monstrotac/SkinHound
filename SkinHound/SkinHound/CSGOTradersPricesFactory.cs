using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Web.Http;
using HttpClient = Windows.Web.Http.HttpClient;

namespace SkinHound
{
    public class CSGOTradersPricesFactory
    {
        private HttpClient client = new HttpClient();
        private const string CSGO_TRADERS_DATA_URL = "https://prices.csgotrader.app/latest/prices_v6.json";
        private JObject globalMarketData;
        public CSGOTradersPricesFactory()
        {
            client.DefaultRequestHeaders.Accept.Clear();
            VerifyIfRequireUpdates();
            PrepareData();
        }
        public async void PrepareData()
        {
            //We make sure that the file exists and that it is up to date.
            await VerifyIfRequireUpdates();
            //Once we are certain, we open it and parse it to an object.
            var dataFileList = Directory.EnumerateFiles($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\SkinHound", "*-data.json");
            if (dataFileList.Count() == 0)
                return;
            string textInDataFile = File.ReadAllText(dataFileList.First());
            //We have to convert the characters code into their symbol to verify with the market hash name directly later on.
            textInDataFile = textInDataFile.Replace("\\u2122", "\u2122");
            textInDataFile = textInDataFile.Replace("\\u2605", "\u2605");
            //We parse the object and assign it into our field.
            globalMarketData = JObject.Parse(textInDataFile);
        }
        public async Task<GlobalMarketDataObject> GetItemGlobalData(string marketHashName)
        {
            if (marketHashName == null)
                return null;
            //This is used to handle null values.
            JsonSerializerSettings settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            GlobalMarketDataObject currentItem = new GlobalMarketDataObject();
            var olga = globalMarketData;
            currentItem.Steam = JsonConvert.DeserializeObject<SteamHistory>(globalMarketData[marketHashName]["steam"].ToString(), settings);
            currentItem.Buff163 = new Buff163History();
            //We must do a little bit of validation before hand for this part.
            if (globalMarketData[marketHashName]["buff163"]["starting_at"]["price"].ToString() != "")
                currentItem.Buff163.Starting_At = (double)globalMarketData[marketHashName]["buff163"]["starting_at"]["price"];
            else
                currentItem.Buff163.Starting_At = 0.00;
            if(globalMarketData[marketHashName]["buff163"]["highest_order"]["price"].ToString() != "")
                currentItem.Buff163.Highest_Order = (double)globalMarketData[marketHashName]["buff163"]["highest_order"]["price"];
            else
                currentItem.Buff163.Highest_Order = 0.00;
            return currentItem;
        }
        private async Task VerifyIfRequireUpdates()
        {
            if (Directory.EnumerateFiles($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\SkinHound","*-data.json").Count() != 0)
            {
                var filesArray = Directory.EnumerateFiles($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\SkinHound","*-data.json");
                if (filesArray.ElementAt(0).Contains($"{DateTime.Now.ToString("d")}-data.json"))
                    return;
                else File.Delete(filesArray.ElementAt(0));
            }
            await DownloadDataFile();
            return;
        }
        private async Task DownloadDataFile()
        {
            //This is used to handle null values.
            JsonSerializerSettings settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            //The list that we will eventually return.
            var response = await client.GetAsync(new Uri($"{CSGO_TRADERS_DATA_URL}"));
            if (response.IsSuccessStatusCode)
            {
                string market = await response.Content.ReadAsStringAsync();
                File.WriteAllText($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\SkinHound\\{DateTime.Now.ToString("d")}-data.json", market);
            }
            return;
        }
    }
}
