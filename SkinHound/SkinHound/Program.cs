/*
 *  SKINPORTANALYZER.EXE
 *  CREATED BY SIMON-OLIVIER "BOBCALVERY" LACHANCE-GAGNÉ
 *  THIS SOFTWARE WAS CREATED UNDER THE USAGE OF THE MIT LICENSE
 *  QC,CANADA NOVEMBER 2022
*/
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;
using System.Linq;
using System.IO;

namespace SkinHound
{
    internal class Program
    {
        //The default content for the config file, in case it does not already exist.
        const string DEFAULT_CONFIG_FILE_CONTENT = "{" +
          "\n\t\"desired_weapons_min_discount_threshold\": 22.0," +
          "\n\t\"good_discount_threshold\": 29.0," +
          "\n\t\"great_discount_threshold\": 33.0," +
          "\n\t\"outstanding_discount_threshold\": 39.0," +
          "\n\t\"minimum_worth_value\": 1.00," +
          "\n\t\"minutes_between_queries\": 2," +
          "\n\t\"desired_weapons\":[" +
          "\n\t]," +
          "\n\t\"notify_on_all_desired_weapons\":true" +
          "\n}";
        //The notification manager takes care of recording the history of what we have already been notified of and sends the notifications to his NotificationCreator
        private static NotificationManager notificationManager = new NotificationManager();
        //Our configuration info which we acquire from config.json
        public static SkinportAnalyzerConfiguration userConfiguration;
        //This setting is created publicly to avoid repetition, it is used to avoid null values.
        private static JsonSerializerSettings settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
        //The following const is a path to make our life easier.
        public const string SKINPORT_MARKET_HISTORY_PATH = "/v1/sales/history?currency=CAD&limit=30";
        //For safety measures, we store our token info in the environement variables of the system, in order for the software to work for you, you'll have to create variables with the same name with your tokens as their value.
        private const string SKINPORT_TOKEN_CLIENT_ENV_VAR = "skinport_tk_id";
        private const string SKINPORT_TOKEN_SECRET_ENV_VAR = "skinport_tk_secret";
        //Here we declare the client which allows us to make requests to the API.
        private static HttpClient client = new HttpClient();
        //We keep a copy of the current product list in memory for price checking.
        private static List<Product> productListInMemory = new List<Product>();


        //Shows the content of the product.
        private static async Task ShowProduct(Product product)
        {
            //These variables are used to determine what type of notification we will be sending.
            bool shouldSendNotification = false, notificationIsFromDesired = false;
            NotificationType notificationType = NotificationType.REGULAR;
            //We force tĥe product to update its percentage off to make sure it is up to date and already calculated.
            product.UpdatePercentageOff();
            //At this point we discover what we're dealing with and if we should share it with the user or not.
            if (product.Percentage_Off == 100 || product.Suggested_Price < userConfiguration.Minimum_Worth_Value || product.Market_Hash_Name.Contains("Case Hardened") || product.Market_Hash_Name.Contains("Doppler") || product.Market_Hash_Name.Contains("Marble Fade"))
                return;
            //Before anything happens, we start by verfying if it's a desired weapon.
            if (VerifyIfDesired(product.Market_Hash_Name) && product.Percentage_Off > userConfiguration.Desired_Weapons_Min_Discount_Threshold)
            {
                //Now that it is relevant, we acquire details about the last sales of the products.
                ProductMarketHistory productMarketHistory = await GetProductMarketHistory(product.Market_Hash_Name);
                double recommendedDiscount = await productMarketHistory.GetRecommendedResellDiscount(product);
                switch (product.Percentage_Off)
                {
                    case var _ when product.Percentage_Off >= userConfiguration.Outstanding_Discount_Threshold:
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.BackgroundColor = ConsoleColor.Black;
                        notificationIsFromDesired = true; shouldSendNotification = true;
                        notificationType = NotificationType.INCREDIBLE;
                        break;
                    case var _ when product.Percentage_Off >= userConfiguration.Great_Discount_Threshold:
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.BackgroundColor = ConsoleColor.Black;
                        notificationIsFromDesired = true; shouldSendNotification = true;
                        notificationType = NotificationType.GOLDEN;
                        break;

                    case var _ when product.Percentage_Off >= userConfiguration.Good_Discount_Threshold:
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.BackgroundColor = ConsoleColor.Black;
                        notificationIsFromDesired = true; shouldSendNotification = true;
                        notificationType = NotificationType.REGULAR;
                        break;
                    default:
                        if (userConfiguration.Notify_On_All_Desired_Weapons)
                        {
                            shouldSendNotification = true;
                            notificationType = NotificationType.DEFAULT;
                        }
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.BackgroundColor = ConsoleColor.Black;
                        break;
                }
                Console.WriteLine($"- Desired Item -\n{product.Market_Hash_Name}\n\tDiscount: {product.Percentage_Off}%\n\tListed for: {product.Min_Price.ToString("0.00")}$\n\tSuggested price: {product.Suggested_Price.ToString("0.00")}$\n\tMarket page: {product.Item_Page}");
                //Financial information about the item in perticular.
                Console.WriteLine($"\t- Reselling information -" +
                    $"\n\t\t{new string('*', 50)}" +
                    $"\n\t\tAVG sold for (Last 7 days): {productMarketHistory.Last_7_days.Avg}$" +
                    $"\n\t\tVolume sold (Last 7 days): {productMarketHistory.Last_7_days.Volume}" +
                    $"\n\t\tAVG sold for (Last 30 days): {productMarketHistory.Last_30_days.Avg}$" +
                    $"\n\t\tVolume sold (Last 30 days): {productMarketHistory.Last_30_days.Volume}" +
                    $"\n\t\tMEDIAN sold for ({productMarketHistory.Sales.Count} Last sales): {(await productMarketHistory.GetMedian()).ToString("0.00")}$" +
                    $"\n\t\t{new string('*', 50)}" +
                    $"\n\t\tRecommended discount % on resell: {recommendedDiscount}%" +
                    $"\n\t\tRecommended resell price: {((1 - recommendedDiscount / 100) * (double)product.Suggested_Price).ToString("0.00")}$" +
                    $"\n\t\tProfit % on resell: {Math.Round((((1 - (double)recommendedDiscount / 100) * (double)product.Suggested_Price * GetSkinPortCut(product) - (double)product.Min_Price) / (double)product.Min_Price * 100), 2)}%" +
                    $"\n\t\tProfit $ on resell: {((1 - (double)recommendedDiscount / 100) * (double)product.Suggested_Price * GetSkinPortCut(product) - (double)product.Min_Price).ToString("0.00")}$" +
                    $"\n\t\t{new string('*', 50)}");
            }
            else if (product.Percentage_Off >= userConfiguration.Good_Discount_Threshold)
            {
                //Now that it is relevant, we acquire details about the last sales of the products.
                ProductMarketHistory productMarketHistory = await GetProductMarketHistory(product.Market_Hash_Name);
                double recommendedDiscount = await productMarketHistory.GetRecommendedResellDiscount(product);
                switch (product.Percentage_Off)
                {
                    case var _ when product.Percentage_Off >= userConfiguration.Outstanding_Discount_Threshold:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.BackgroundColor = ConsoleColor.Black;
                        shouldSendNotification = true;
                        notificationType = NotificationType.INCREDIBLE;
                        break;
                    case var _ when product.Percentage_Off >= userConfiguration.Great_Discount_Threshold:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.BackgroundColor = ConsoleColor.Black;
                        shouldSendNotification = true;
                        notificationType = NotificationType.GOLDEN;
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.BackgroundColor = ConsoleColor.Black;
                        //To receive notifications for all the products listed in the application, including regular deals uncomment the lines bellow.
                        shouldSendNotification = true;
                        notificationType = NotificationType.REGULAR;
                        break;
                }
                Console.WriteLine($"- Great Deal -\n{product.Market_Hash_Name}\n\tDiscount: {product.Percentage_Off}%\n\tListed for: {product.Min_Price.ToString("0.00")}$\n\tSuggested price: {product.Suggested_Price.ToString("0.00")}$\n\tMarket page: {product.Item_Page}");
                //Financial information about the item in perticular.
                Console.WriteLine($"\t- Reselling information -" +
                    $"\n\t\t{new string('*', 50)}" +
                    $"\n\t\tAVG sold for (Last 7 days): {productMarketHistory.Last_7_days.Avg}$" +
                    $"\n\t\tVolume sold (Last 7 days): {productMarketHistory.Last_7_days.Volume}" +
                    $"\n\t\tAVG sold for (Last 30 days): {productMarketHistory.Last_30_days.Avg}$" +
                    $"\n\t\tVolume sold (Last 30 days): {productMarketHistory.Last_30_days.Volume}" +
                    $"\n\t\tMEDIAN sold for ({productMarketHistory.Sales.Count} Last sales): {(await productMarketHistory.GetMedian()).ToString("0.00")}$" +
                    $"\n\t\t{new string('*', 50)}" +
                    $"\n\t\tRecommended discount % on resell: {recommendedDiscount}%" +
                    $"\n\t\tRecommended resell price: {((1 - recommendedDiscount / 100) * (double)product.Suggested_Price).ToString("0.00")}$" +
                    $"\n\t\tProfit % on resell: {Math.Round((((1 - (double)recommendedDiscount / 100) * (double)product.Suggested_Price * GetSkinPortCut(product) - (double)product.Min_Price) / (double)product.Min_Price * 100), 2)}%" +
                    $"\n\t\tProfit $ on resell: {((1 - (double)recommendedDiscount / 100) * (double)product.Suggested_Price * GetSkinPortCut(product) - (double)product.Min_Price).ToString("0.00")}$" +
                    $"\n\t\t{new string('*', 50)}");
            }
            //We finish off by sending a notification if the deal was determined as good enough.
            if (shouldSendNotification)
            {
                //We notify our notification manager to take care of the notification process.
                await notificationManager.SendNotification(product, notificationType, notificationIsFromDesired);
            }
            return;
        }
        //Returns the multiplier needed to know how much skinport will take.
        private static double GetSkinPortCut(Product product)
        {
            if (product.Min_Price > 1000)
                return 0.94;
            else return 0.88;
        }

        //This method is used to verify if the item is desired by looping through the elements inside of the list of desired weapons, if it isn't, it returns false.
        private static bool VerifyIfDesired(string itemName, int i = 0)
        {
            if (userConfiguration.Desired_Weapons.Count == 0)
                return false;
            if (itemName.Contains($"{userConfiguration.Desired_Weapons.ElementAt(i)}"))
                return true;
            else if (userConfiguration.Desired_Weapons.Count - 1 > i)
                return VerifyIfDesired(itemName, i + 1);
            return false;
        }
        //Gets the global product list which has little details to it.
        private static async Task<List<Product>> GetGlobalProductList(string path)
        {
            //This is used to handle null values.

            //The list that we will eventually return.
            List<Product> productList = new List<Product>();
            string products;
            HttpResponseMessage response = await client.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                products = await response.Content.ReadAsStringAsync();
                productList = JsonConvert.DeserializeObject<List<Product>>(products, settings);
            }
            return productList;
        }
        private static async Task<ProductMarketHistory> GetProductMarketHistory(string marketHashName)
        {
            //This is used to handle null values.
            JsonSerializerSettings settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            //The list that we will eventually return.
            List<ProductMarketHistory> productMarketHistory = new List<ProductMarketHistory>();
            string marketHistory;
            HttpResponseMessage response = await client.GetAsync($"{SKINPORT_MARKET_HISTORY_PATH}&market_hash_name={marketHashName}");
            if (response.IsSuccessStatusCode)
            {
                marketHistory = await response.Content.ReadAsStringAsync();
                productMarketHistory = JsonConvert.DeserializeObject<List<ProductMarketHistory>>(marketHistory, settings);
            }
            if (productMarketHistory.Count != 0)
                return productMarketHistory.First();
            else return null;
        }
        //Async task runner where the good shit happens.
        private static async Task RunAsync()
        {
            try
            {
                //We clear the console and put a message announcing that we are updating the entries.
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.WriteLine("Updating entries...");
                List<Product> globalProductList = await GetGlobalProductList("/v1/items?currency=CAD");
                //We sort the list in order to have the most expensive outputs at the end.
                globalProductList.Sort((x, y) => x.Suggested_Price.CompareTo(y.Suggested_Price));
                //We send the product list into the memory var
                productListInMemory = globalProductList;
                //We clear the console once again before showing the result of our last request.
                Console.Clear();
                Console.WriteLine($"\r\n░██████╗██╗░░██╗██╗███╗░░██╗██████╗░░█████╗░██████╗░████████╗\r\n██╔════╝██║░██╔╝██║████╗░██║██╔══██╗██╔══██╗██╔══██╗╚══██╔══╝\r\n╚█████╗░█████═╝░██║██╔██╗██║██████╔╝██║░░██║██████╔╝░░░██║░░░\r\n░╚═══██╗██╔═██╗░██║██║╚████║██╔═══╝░██║░░██║██╔══██╗░░░██║░░░\r\n██████╔╝██║░╚██╗██║██║░╚███║██║░░░░░╚█████╔╝██║░░██║░░░██║░░░\r\n╚═════╝░╚═╝░░╚═╝╚═╝╚═╝░░╚══╝╚═╝░░░░░░╚════╝░╚═╝░░╚═╝░░░╚═╝░░░\r\n\r\n░█████╗░███╗░░██╗░█████╗░██╗░░░░░██╗░░░██╗███████╗███████╗██████╗░\r\n██╔══██╗████╗░██║██╔══██╗██║░░░░░╚██╗░██╔╝╚════██║██╔════╝██╔══██╗\r\n███████║██╔██╗██║███████║██║░░░░░░╚████╔╝░░░███╔═╝█████╗░░██████╔╝\r\n██╔══██║██║╚████║██╔══██║██║░░░░░░░╚██╔╝░░██╔══╝░░██╔══╝░░██╔══██╗\r\n██║░░██║██║░╚███║██║░░██║███████╗░░░██║░░░███████╗███████╗██║░░██║\r\n╚═╝░░╚═╝╚═╝░░╚══╝╚═╝░░╚═╝╚══════╝░░░╚═╝░░░╚══════╝╚══════╝╚═╝░░╚═╝\n");
                Console.WriteLine($"{new String('-', 30)} Last updated: {DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt")} {new String('-', 31)}");

                foreach (Product product in globalProductList)
                {
                    //Filter and show all of the products which meet our designated requirements.
                    await ShowProduct(product);
                }
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.WriteLine(new String('-', 100));
                Console.Write("Enter a skin name to price check it: ");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        //Handler of the good shit takes care of repeating the process every X minutes.
        private static void handleProcess(object state)
        {
            RunAsync().GetAwaiter().GetResult();
        }
        //Base64 utilities.
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
        //This method gets the current user config via the help of config.json
        public static void GetConfig()
        {
            //If the config file does not already exist on the machine, we create it.
            if (!Directory.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\SkinportAnalyzer"))
                Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\SkinportAnalyzer");
            if (!File.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\SkinportAnalyzer\\config.json"))
            {
                var configFile = File.CreateText($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\SkinportAnalyzer\\config.json");
                configFile.Write(DEFAULT_CONFIG_FILE_CONTENT);
                configFile.Flush();
            }
            userConfiguration = JsonConvert.DeserializeObject<SkinportAnalyzerConfiguration>(File.ReadAllText($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\SkinportAnalyzer\\config.json"));
        }
        //Main
        public static void ApiMain()
        {
            //We start by acquiring the configs
            GetConfig();
            // Listen to notification activation
            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                //The default link.
                string link = "https://skinport.com/";
                // Obtain the arguments from the notification
                ToastArguments.Parse(toastArgs.Argument).TryGetValue("Link", out link);
                //We attempt to start a browser to item.
                var webSurfer = new ProcessStartInfo("chrome.exe");
                webSurfer.Arguments = link;
                Process.Start(webSurfer);
            };
            // Update port # in the following line.
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string authHeader = Base64Encode($"{Environment.GetEnvironmentVariable(SKINPORT_TOKEN_CLIENT_ENV_VAR)}:{Environment.GetEnvironmentVariable(SKINPORT_TOKEN_SECRET_ENV_VAR)}");
            client.DefaultRequestHeaders.Add("Authorization", "Basic " + authHeader);
            client.BaseAddress = new Uri("https://api.skinport.com/v1/");
            //Launch the timer.
            int timeInterval = 1000 * 60 * userConfiguration.Minutes_Between_Queries;
            //Timer timer = new Timer(handleProcess, null, 0, timeInterval);
            //while (true)
            //{
                //RunPriceChecker();
            //}
        }
        //This method is used to run manual price checks on items, it is incredibly useful to sell items fast at a decent price.
        private async static void RunPriceChecker()
        {
            string skinName = Console.ReadLine();
            if (productListInMemory.Count > 0)
            {
                bool productFound = false;
                foreach (Product product in productListInMemory)
                {
                    productFound = await PriceCheck(product, skinName, productFound);
                }
                if (!productFound)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.WriteLine($"No skin with the name \"{skinName}\" found.");
                }
            }
            else
            {
                Console.WriteLine("Product list is empty please try again");
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write("Enter a skin name to price check it: ");
        }
        private static async Task<bool> PriceCheck(Product product, string skinName, bool productAlreadyFound)
        {
            //We make sure that the product's discount percentage is up to date.
            product.UpdatePercentageOff();
            //Before anything happens, we start by verfying if it's the skin we're searching for.
            if (!product.Market_Hash_Name.Contains(skinName))
                return productAlreadyFound;
            //Now that it is relevant, we acquire details about the last sales of the products.
            ProductMarketHistory productMarketHistory = await GetProductMarketHistory(product.Market_Hash_Name);
            double recommendedDiscount = await productMarketHistory.GetRecommendedResellDiscount(product);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine($"Item name: {product.Market_Hash_Name}\n\tDiscount: {product.Percentage_Off}%\n\tListed for: {product.Min_Price.ToString("0.00")}$\n\tSuggested price: {product.Suggested_Price.ToString("0.00")}$\n\tMarket page: {product.Item_Page}");
            //Financial information about the item in perticular.
            Console.WriteLine($"\n\t\t{new string('*', 50)}" +
                $"\n\t\tAVG sold for (Last 7 days): {productMarketHistory.Last_7_days.Avg}$" +
                $"\n\t\tVolume sold (Last 7 days): {productMarketHistory.Last_7_days.Volume}" +
                $"\n\t\tAVG sold for (Last 30 days): {productMarketHistory.Last_30_days.Avg}$" +
                $"\n\t\tVolume sold (Last 30 days): {productMarketHistory.Last_30_days.Volume}" +
                $"\n\t\tMEDIAN sold for ({productMarketHistory.Sales.Count} Last sales): {(await productMarketHistory.GetMedian()).ToString("0.00")}$" +
                $"\n\t\t{new string('*', 50)}" +
                $"\n\t\tRecommended discount % on resell: {recommendedDiscount}%" +
                $"\n\t\tRecommended resell price: {((1 - recommendedDiscount / 100) * (double)product.Suggested_Price).ToString("0.00")}$" +
                $"\n\t\tProfit % on resell: {Math.Round((((1 - (double)recommendedDiscount / 100) * (double)product.Suggested_Price * GetSkinPortCut(product) - (double)product.Min_Price) / (double)product.Min_Price * 100), 2)}%" +
                $"\n\t\tProfit $ on resell: {((1 - (double)recommendedDiscount / 100) * (double)product.Suggested_Price * GetSkinPortCut(product) - (double)product.Min_Price).ToString("0.00")}$" +
                $"\n\t\t{new string('*', 50)}");
            return true;
        }
        //Executes when the program exits.
        static void OnProcessExit(object sender, EventArgs e)
        {
            Console.WriteLine("I'm out of here");
        }
    }
}
