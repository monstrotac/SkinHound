using System;
using System.Collections.Generic;
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

namespace SkinHound
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Private Methods
        private SkinportApiFactory skinportApiFactory;
        private WrapPanel dealsGrid;

        public MainWindow()
        {
            skinportApiFactory = new SkinportApiFactory();
            InitializeComponent();
            //Initialize the methods linked to components of the application.
            dealsGrid = (WrapPanel)FindName("DealsGrid");
            DealsGridHandler();
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
        private async Task<Queue<Product>> ShowDeals(Queue<Product> productQueue)
        {
            if (productQueue == null || productQueue.Count > 0)
            {
                ItemDeal curDeal = new ItemDeal();
                ((Grid)curDeal.FindName("DealXGrid")).Name = $"Deal{productQueue.Count}Grid";
                Button itemButton = ((Button)curDeal.FindName("DealButtonX")); 
                itemButton.Name = $"DealButton{productQueue.Count}";
                TextBlock itemName = ((TextBlock)curDeal.FindName("DealXItemName"));  
                itemName.Name = $"Deal{productQueue.Count}ItemName";

                Product curProduct = productQueue.Dequeue();
                itemName.Text = curProduct.Market_Hash_Name;
                itemButton.Tag = curProduct.Item_Page;
                dealsGrid.Children.Add(curDeal);

                return await ShowDeals(productQueue);
            }
            else
                return productQueue;
        }
        private void DealClicked(object sender, RoutedEventArgs e)
        {

        }


    }
}
