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
    public partial class ItemDeal : UserControl
    {
        public ItemDeal()
        {
            InitializeComponent();
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
    }
}
