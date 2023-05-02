using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Usb;
using Windows.Media.Protection.PlayReady;

namespace SkinHound
{
    class Buff163ApiFactory
    {
        private SkinHoundConfiguration configuration;
        private BuffMarketHistory buffMarketHistory;
        //Here we declare the client which allows us to make requests to the API.
        private static HttpClient client = new HttpClient();
        private const string BUFF_MARKET_PATH = "market/goods?game=csgo&page_num=1&page_size=80&sort_by=price";
        private const string BUFF_API_PATH = "https://buff.163.com/api/";

        public Buff163ApiFactory(SkinHoundConfiguration skinHoundConfiguration)
        {
            //We start by setting our configuration
            configuration = skinHoundConfiguration;
            // Update port # in the following line.
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            //string authHeader = Utils.Base64Encode($"{Environment.GetEnvironmentVariable(SKINPORT_TOKEN_CLIENT_ENV_VAR)}:{Environment.GetEnvironmentVariable(SKINPORT_TOKEN_SECRET_ENV_VAR)}");
            if(configuration != null)
                if(configuration.Buff_Cookie!= "" && configuration.Buff_Cookie!=null)
                    client.DefaultRequestHeaders.Add("Cookie", configuration.Buff_Cookie);
            client.BaseAddress = new Uri(BUFF_API_PATH);
        }
        public async Task<bool> VerifyCookie()
        {
            //This is used to handle null values.
            JsonSerializerSettings settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            //The list that we will eventually return.
            string marketHistory;
            HttpResponseMessage response = await client.GetAsync($"{BUFF_MARKET_PATH}");
            if (response.IsSuccessStatusCode)
            {
                marketHistory = await response.Content.ReadAsStringAsync();
                if (marketHistory.Contains("Login Required"))
                    return false;
                buffMarketHistory = JsonConvert.DeserializeObject<BuffMarketHistory>(marketHistory, settings);
                return true;
            }
            return false;
        }
        public async Task<BuffItem> GetItem(string name)
        {            
            //This is used to handle null values.
            JsonSerializerSettings settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            //The list that we will eventually return.
            string marketHistory;
            HttpResponseMessage response = await client.GetAsync($"{BUFF_MARKET_PATH}&search={name}");
            if (response.IsSuccessStatusCode)
            {
                marketHistory = await response.Content.ReadAsStringAsync();
                buffMarketHistory = JsonConvert.DeserializeObject<BuffMarketHistory>(marketHistory, settings);
            }
            else return null;
            foreach (BuffItem bItem in buffMarketHistory.Data.Items)
            {
                if (bItem.Market_Hash_Name.Contains(name))
                    return bItem;
            }
            return null;
        }
        public BuffMarketHistory GetBuffMarketHistory()
        {
            return buffMarketHistory;
        }
        public void SetConfig(SkinHoundConfiguration houndConfiguration)
        {
            configuration = houndConfiguration;
        }
    }
}
