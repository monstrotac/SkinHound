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
        private ControlTemplate dealTemplate;

        public MainWindow()
        {
            skinportApiFactory = new SkinportApiFactory();
            InitializeComponent();
            //Initialize the methods linked to components of the application.
            dealsGrid = (WrapPanel)FindName("DealsGrid");
            dealTemplate = (ControlTemplate)FindResource("dealTemplate");
            DealsGridHandler();
        }
        private async void DealsGridHandler()
        {
            RefreshDeals();
        }
        private async void RefreshDeals()
        {
            RemoveDeals();
            Queue<Product> deals = new Queue<Product>();
            foreach (var element in await skinportApiFactory.AcquireProductList())
            {
                deals.Enqueue(element);
            }
            await ShowDeals(deals);
        }
        private async void RemoveDeals()
        {
            dealsGrid.Children.Clear();
        }
        private async Task ShowDeals(Queue<Product> productList)
        {
            if(productList == null || productList.Count > 0)
            {
                while(productList.Count > 0)
                {
                    ControlTemplate template = dealTemplate;
                    dealTemplate.VisualTree.FirstChild.Name = $"Deal`{productList.Count}`Grid";
                    Product curProduct = productList.Dequeue();
                    dealsGrid.Children.Add(new UserControl1());
                }
            }
                //DealsGrid.Children.Add();
        }
        private void DealClicked(object sender, RoutedEventArgs e)
        {

        }


    }
}
