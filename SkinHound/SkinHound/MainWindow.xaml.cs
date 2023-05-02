using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Devices.Usb;

namespace SkinHound
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Private Methods
        private SkinHoundConfiguration configuration;
        private SkinportApiFactory skinportApiFactory;
        private Buff163ApiFactory buff163ApiFactory;
        private bool buffCookieFunctionnal = false;
        private WrapPanel dealsGrid;
        //The default content for the config file, in case it does not already exist.
        private const string DEFAULT_CONFIG_FILE_CONTENT = "{" +
          "\n\t\"desired_weapons_min_discount_threshold\": 22.0," +
          "\n\t\"good_discount_threshold\": 25.0," +
          "\n\t\"great_discount_threshold\": 30.0," +
          "\n\t\"outstanding_discount_threshold\": 35.0," +
          "\n\t\"minimum_worth_value\": 3.00," +
          "\n\t\"minutes_between_queries\": 2," +
          "\n\t\"desired_weapons\":[" +
          "\n\t]," +
          "\n\t\"notify_on_all_desired_weapons\":true," +
          "\n\t\"notifications_enabled\":true," +
          "\n\t\"buff_cookie\":''" +
          "\n}";

        public MainWindow()
        {
            //The very first step is to acquire the configuration from the file, if it exists.
            GetUserConfigFromFile();
            skinportApiFactory = new SkinportApiFactory(configuration);
            buff163ApiFactory = new Buff163ApiFactory(configuration);
            InitializeComponent();
            //We obtain Buff's Market History here since doing it constantly is not good practice for this one, and is not needed.
            RefreshBuffMarketValue();
            //Initialize the methods linked to components of the application.
            dealsGrid = (WrapPanel)FindName("DealsGrid");
            DealsGridHandler();
            //We update the value of the rates for money conversion later on since everything the Buff163 API returns is in CNY
            Utils.UpdateRates();
        }
        private async void DealsGridHandler()
        {
            await RefreshDeals();
        }
        private async Task RefreshDeals()
        {
            RemoveDeals();
            Queue<Product> deals = new Queue<Product>();
            List<Product> list = await skinportApiFactory.AcquireProductList();
            if(list != null)
            {
                foreach (var element in list)
                {
                    deals.Enqueue(element);
                }
                await ShowDeals(deals);
            }
        }
        private async void RemoveDeals()
        {
            dealsGrid.Children.Clear();
        }
        //This Task takes care of updating the deals for the user and formats them with the values which have been placed inside of it.
        private async Task<Queue<Product>> ShowDeals(Queue<Product> productQueue)
        {
            BuffMarketHistory buffMarketHistory = buff163ApiFactory.GetBuffMarketHistory();
            if (productQueue == null || productQueue.Count > 0)
            {
                ItemDeal curDeal = new ItemDeal();
                //In this section we rename everything and keep them as a variable to give them a new value later on.
                ((Grid)curDeal.FindName("DealXGrid")).Name = $"Deal{productQueue.Count}Grid";
                Button itemButton = ((Button)curDeal.FindName("DealButtonX"));
                itemButton.Name = $"DealButton{productQueue.Count}";
                TextBlock itemName = ((TextBlock)curDeal.FindName("DealXItemName"));
                itemName.Name = $"Deal{productQueue.Count}ItemName";
                Image itemImage = ((Image)curDeal.FindName("DealXImage"));
                itemImage.Name = $"Deal{productQueue.Count}Image";
                //Skinport Variables
                TextBlock itemSkinportDiscount = ((TextBlock)curDeal.FindName("DealXSkinportDiscount"));
                itemSkinportDiscount.Name = $"Deal{productQueue.Count}SkinportDiscount"; 
                TextBlock itemPrice = ((TextBlock)curDeal.FindName("DealXSkinportPrice"));
                itemPrice.Name = $"Deal{productQueue.Count}SkinportPrice";
                TextBlock itemSkinportVolumeSold30Days = ((TextBlock)curDeal.FindName("DealXSkinportVolumeSoldLast30Days"));
                itemSkinportVolumeSold30Days.Name = $"Deal{productQueue.Count}SkinportVolumeSoldLast30Days";
                TextBlock itemSkinportMedianSold30Days = ((TextBlock)curDeal.FindName("DealXSkinportMedianSoldLast30Days"));
                itemSkinportMedianSold30Days.Name = $"Deal{productQueue.Count}SkinportMedianSoldLast30Days";
                //Buff163 Variables
                TextBlock itemBuffStartingAt = ((TextBlock)curDeal.FindName("DealXBuffStartingAt"));
                itemBuffStartingAt.Name = $"Deal{productQueue.Count}BuffStartingAt";
                TextBlock itemBuffHighestOrder = ((TextBlock)curDeal.FindName("DealXBuffHighestOrder"));
                itemBuffHighestOrder.Name = $"Deal{productQueue.Count}BuffHighestOrder";
                //SkinHound Variables
                TextBlock itemRecommendedDiscount = ((TextBlock)curDeal.FindName("DealXRecommendedDiscount"));
                itemRecommendedDiscount.Name = $"Deal{productQueue.Count}RecommendedDiscount";
                TextBlock itemRecommendedSalePrice = ((TextBlock)curDeal.FindName("DealXRecommendedSalePrice"));
                itemRecommendedSalePrice.Name = $"Deal{productQueue.Count}RecommendedSalePrice";
                TextBlock itemProfitPOnResale = ((TextBlock)curDeal.FindName("DealXProfitPOnResale"));
                itemProfitPOnResale.Name = $"Deal{productQueue.Count}ProfitPOnResale";
                TextBlock itemProfitCOnResale = ((TextBlock)curDeal.FindName("DealXProfitCOnResale"));
                itemProfitCOnResale.Name = $"Deal{productQueue.Count}ProfitCOnResale";
                TextBlock itemLongTermInvestmentIndicator = ((TextBlock)curDeal.FindName("DealXLTII"));
                itemLongTermInvestmentIndicator.Name = $"Deal{productQueue.Count}LTII";
                //We Dequeue the product and start assigning its values in the front-end
                Product curProduct = productQueue.Dequeue();
                itemName.Text = curProduct.Market_Hash_Name;
                itemButton.Tag = curProduct.Item_Page;
                itemSkinportDiscount.Text = $"{curProduct.Percentage_Off}%";
                itemPrice.Text = $"{curProduct.Min_Price}$";
                itemSkinportVolumeSold30Days.Text = $"{curProduct.productMarketHistory.Last_30_days.Volume}";
                itemSkinportMedianSold30Days.Text = $"{curProduct.productMarketHistory.Last_30_days.Median}$";
                itemRecommendedDiscount.Text = $"{curProduct.recommendedDiscount}";
                itemRecommendedSalePrice.Text = $"{curProduct.recommendedResellPrice}";
                itemProfitPOnResale.Text = $"{curProduct.profitPercentageOnResellPrice}";
                itemProfitCOnResale.Text = $"{curProduct.profitMoneyOnResellPrice}";
                itemLongTermInvestmentIndicator.Text = $"{await curProduct.productMarketHistory.GetLongTermPercentageProfit(curProduct)}%";
                //We check if Buff is functionnal, in the case where it is, the image source and information changes
                if(buffCookieFunctionnal)
                {
                    //We verify that the item in question isn't null before doing anything.
                    BuffItem buffItem = await buff163ApiFactory.GetItem(curProduct.Market_Hash_Name);
                    if (buffItem != null)
                    {
                        //We assign the values of our previously declared TextBox
                        itemBuffStartingAt.Text = $"{(buffItem.Sell_Min_Price / Utils.usdToCnyRate * Utils.usdToCadRate).ToString("0.00")}$";
                        itemBuffHighestOrder.Text = $"{(buffItem.Buy_Max_Price / Utils.usdToCnyRate * Utils.usdToCadRate).ToString("0.00")}$";
                        //This section gives the item its actual image, if it can't be found it assigns a placeholder which represents the deal's type.
                        if (buffItem.Goods_Info != null)
                            itemImage.Source = new BitmapImage(new Uri($"{buffItem.Goods_Info.Icon_Url}", UriKind.Absolute));
                        else
                            itemImage.Source = new BitmapImage(new Uri($"{curProduct.imagePath}", UriKind.Relative));
                    }
                }
                else
                {
                    //Here we have to initiate a new image in order to assign a new ImageSource
                    itemImage.Source = new BitmapImage(new Uri($"{curProduct.imagePath}", UriKind.Relative));
                    //We assign the values of our previously declared TextBox to Notify that Buff isn't connected.
                    itemBuffStartingAt.Text = $"BUFF COOKIE REQUIRED";
                    itemBuffHighestOrder.Text = $"BUFF COOKIE REQUIRED";
                }
                dealsGrid.Children.Add(curDeal);
                return await ShowDeals(productQueue);
            }
            else
                return productQueue;
        }
        private void DealClicked(object sender, RoutedEventArgs e)
        {

        }
        //Function used to refresh the Data from buff.
        private async void RefreshBuffMarketValue()
        {
            buffCookieFunctionnal = await buff163ApiFactory.VerifyCookie();
        }
        //This function is responsible for acquiring the user configs in /AppData/Roaming/SkinHound
        private void GetUserConfigFromFile()
        {
            //If the config file does not already exist on the machine, we create it.
            if (!Directory.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\SkinHound"))
                Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\SkinHound");
            if (!File.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\SkinHound\\config.json"))
            {
                var configFile = File.CreateText($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\SkinHound\\config.json");
                configFile.Write(DEFAULT_CONFIG_FILE_CONTENT);
                configFile.Flush();
            }
            configuration = JsonConvert.DeserializeObject<SkinHoundConfiguration>(File.ReadAllText($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\SkinHound\\config.json"));
        }
        //Sets the Configuration variable in all of the API factories
        private async void UpdateConfigurationInFactories()
        {
            skinportApiFactory.SetConfig(configuration);
            buff163ApiFactory.SetConfig(configuration);
        }
    }
}
