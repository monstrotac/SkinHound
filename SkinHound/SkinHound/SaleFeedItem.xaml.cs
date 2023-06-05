using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static System.Net.WebRequestMethods;

namespace SkinHound
{
    public partial class SaleFeedItem
    {
        SaleFeedActivity activity;
        string currencySymbol = "$"; 
        public SaleFeedItem(SaleFeedActivity saleFeedActivity)
        {
            InitializeComponent();
            activity = saleFeedActivity;
            InitializeAllValues();
            currencySymbol = Utils.GetCurrencySymbol(SkinHoundConfiguration.Currency);
        }
        private async void InitializeAllValues()
        {
            //We begin initializing the values.
            FeedXName.Text = activity.MarketHashName;
            FeedButtonX.Tag = activity.Url;
            FeedXDiscount.Text = $"{((1 - activity.SalePrice/activity.SuggestedPrice)*100).ToString("0.00")}%";
            FeedXEventType.Content = $"{activity.EventType.ToUpper()}";
            FeedXRecommendedPrice.Text = $"{(activity.SuggestedPrice/100).ToString("0.00")}{currencySymbol}";
            FeedXSalePrice.Text = $"{(activity.SalePrice/100).ToString("0.00")}{currencySymbol}";
            FeedXPattern.Text = $"{activity.Pattern}";
            FeedXVersion.Text = $"{activity.Version}";

            //Need to implement a request to an API which will show images, apparently the Base64 code is used for such thing and not for generating an image.
            //FeedXImage.Source = await Utils.ConvertBinaryToImage(activity.Image);
            FeedXSaleType.Text = activity.SaleType;
            FeedXWear.Text = $"{activity.Wear}";
            if (activity.IsDesired)
            {
                if (((1 - activity.SalePrice / activity.SuggestedPrice) * 100) >= SkinHoundConfiguration.Outstanding_Discount_Threshold)
                    FeedButtonX.Background = Brushes.HotPink;
                else if (((1 - activity.SalePrice / activity.SuggestedPrice) * 100) >= SkinHoundConfiguration.Great_Discount_Threshold)
                    FeedButtonX.Background = Brushes.AliceBlue;
                else if (((1 - activity.SalePrice / activity.SuggestedPrice) * 100) >= SkinHoundConfiguration.Good_Discount_Threshold)
                    FeedButtonX.Background = Brushes.Cyan;
                else if (((1 - activity.SalePrice / activity.SuggestedPrice) * 100) >= SkinHoundConfiguration.Desired_Weapons_Min_Discount_Threshold)
                    FeedButtonX.Background = Brushes.LightBlue;
            }
            else if (((1 - activity.SalePrice / activity.SuggestedPrice) * 100) >= SkinHoundConfiguration.Outstanding_Discount_Threshold && activity.SuggestedPrice / 100 >= (double)SkinHoundConfiguration.Minimum_Worth_Value)
                FeedButtonX.Background = Brushes.LightGreen;
            else if (((1 - activity.SalePrice / activity.SuggestedPrice) * 100) >= SkinHoundConfiguration.Great_Discount_Threshold && activity.SuggestedPrice/100 >= (double)SkinHoundConfiguration.Minimum_Worth_Value)
                FeedButtonX.Background = Brushes.LightYellow;
            else if (((1 - activity.SalePrice / activity.SuggestedPrice) * 100) >= SkinHoundConfiguration.Good_Discount_Threshold && activity.SuggestedPrice / 100 >= (double)SkinHoundConfiguration.Minimum_Worth_Value)
                FeedButtonX.Background = Brushes.Snow;
        }
        private void FeedClicked(object sender, RoutedEventArgs e)
        {
            //The default link.
            string link = "https://skinport.com/item/";
            // Obtain the arguments from the button
            link = $"{link}{(string)FeedButtonX.Tag}/{activity.SaleId}";
            //We attempt to start a browser to item.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                link = link.Replace("&", "^&");
                Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
            }
        }
    }
}
