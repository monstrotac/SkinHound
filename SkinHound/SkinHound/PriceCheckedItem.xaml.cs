using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
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
    public partial class PriceCheckedItem : UserControl
    {
        private ScrollViewer dealScrollBar;
        public PriceCheckedItem(ScrollViewer dealScroll)
        {
            InitializeComponent();
            dealScrollBar = dealScroll;
        }

        private void PriceCheckedClicked(object sender, RoutedEventArgs e)
        {
            //The default link.
            string link = "https://skinport.com/";
            // Obtain the arguments from the button
            link = (string)PriceCheckedButtonX.Tag;
            //We attempt to start a browser to item.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                link = link.Replace("&", "^&");
                Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
            }
        }
        private bool _scrolling = true;
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
    }
}
