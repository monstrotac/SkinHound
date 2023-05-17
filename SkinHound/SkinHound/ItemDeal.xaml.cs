using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Policy;
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
using Windows.Storage.Pickers;

namespace SkinHound
{
    /// <summary>
    /// Interaction logic for ItemDeal.xaml
    /// </summary>
    public partial class ItemDeal : UserControl, INotifyPropertyChanged
    {
        public Product product;
        private ScrollViewer dealScrollBar;
        public ItemDeal(ScrollViewer dealScroll, Product curProduct, string currencySymbol)
        {
            InitializeComponent();
            dealScrollBar = dealScroll;
            product = curProduct;
            //We begin initializing the values.
            InitializeAllValues(currencySymbol, curProduct);
        }
        private async void InitializeAllValues(string currencySymbol, Product product)
        {
            if (product.isNew)
                DealXNewOrNot.Content = "*NEW*";
            DealXItemName.Text = product.Market_Hash_Name;
            DealButtonX.Tag = product.Item_Page;
            DealXSkinportDiscount.Text = $"{product.Percentage_Off}%";
            DealXSkinportPrice.Text = $"{product.Min_Price.ToString("0.00")}{currencySymbol}";
            DealXSkinportVolumeSoldLast30Days.Text = $"{product.productMarketHistory.Last_30_days.Volume}";
            DealXSkinportMedianSoldLast30Days.Text = $"{product.productMarketHistory.Last_30_days.Median.ToString("0.00")}{currencySymbol}";
            DealXBuffStartingAt.Text = $"{(product.GlobalMarketData.Buff163.Starting_At * Utils.GetCurrencyRateFromUSD(SkinHoundConfiguration.Currency)).ToString("0.00")}{currencySymbol}";
            DealXBuffHighestOrder.Text = $"{(product.GlobalMarketData.Buff163.Highest_Order * Utils.GetCurrencyRateFromUSD(SkinHoundConfiguration.Currency)).ToString("0.00")}{currencySymbol}";
            DealXSteamLast7Days.Text = $"{(product.GlobalMarketData.Steam.Last_7d * Utils.GetCurrencyRateFromUSD(SkinHoundConfiguration.Currency)).ToString("0.00")}{currencySymbol}";
            DealXSteamLast30Days.Text = $"{(product.GlobalMarketData.Steam.Last_30d * Utils.GetCurrencyRateFromUSD(SkinHoundConfiguration.Currency)).ToString("0.00")}{currencySymbol}";
            DealXRecommendedDiscount.Text = $"{product.recommendedDiscount}";
            DealXRecommendedSalePrice.Text = $"{product.recommendedResellPrice}{currencySymbol}";
            DealXProfitPOnResale.Text = $"{product.profitPercentageOnResellPrice}";
            DealXProfitCOnResale.Text = $"{product.profitMoneyOnResellPrice}{currencySymbol}";
            DealXLTII.Text = $"{await product.productMarketHistory.GetLongTermPercentageProfit()}%";
            DealXMovingAverage.Text = $"{await product.productMarketHistory.GetLongMovingMedian()}{currencySymbol}";
            DealXImage.Source = new BitmapImage(new Uri($"{product.imagePath}", UriKind.Relative));
        }

        private void DealClicked(object sender, RoutedEventArgs e)
        {
            //The default link.
            string link = "https://skinport.com/";
            // Obtain the arguments from the button
            link = (string)DealButtonX.Tag;
            //We attempt to start a browser to item.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                link = link.Replace("&", "^&");
                Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
            }
        }
        private bool _scrolling = true;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (this._scrolling)
            {
                dealScrollBar.ScrollToVerticalOffset(dealScrollBar.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }
        private void ScrollViewer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            this._scrolling = false;
            e.Handled = true;
        }

        private void ScrollViewer_MouseLeave(object sender, MouseEventArgs e)
        {
            this._scrolling = true;
            e.Handled = true;
        }
        public override string ToString()
        {
            return $"Skin: {product.Market_Hash_Name}";
        }
    }
}
