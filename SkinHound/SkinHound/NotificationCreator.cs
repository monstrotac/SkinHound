using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;

namespace SkinHound
{
    enum NotificationType
    {
        DEFAULT = 0,
        REGULAR = 1,
        GOLDEN = 2,
        INCREDIBLE = 3
    }
    public class NotificationCreator
    {
        private const string IMG_RES_PATH = "\\resources\\Notifications\\";
        private const string WAV_RES_PATH = "\\resources\\Notifications\\";
        public async Task CreateDefaultDesiredNotification(Product product)
        {
            //We create the sound player to play the notification sound.
            SoundPlayer soundPlayer;
            new ToastContentBuilder()
            .AddArgument("Link", product.Item_Page)
            .AddInlineImage(new Uri($"{Directory.GetCurrentDirectory()}{IMG_RES_PATH}DefaultDesiredNotification.png"))
            .AddText($"\u2736 | {product.Market_Hash_Name}")
            .AddText($"Listed {product.Percentage_Off}% off for a total price of {product.Min_Price}$!")
            .AddButton(new ToastButton()
            .SetContent("Open item's page")
            .AddArgument("Link", product.Item_Page))
            .Show();
            soundPlayer = new SoundPlayer($"{Directory.GetCurrentDirectory()}{WAV_RES_PATH}DefaultDesiredNotification.wav");
            await Task.Run(() => { soundPlayer.Play(); });
            return;
        }

        public async Task CreateGoodNotification(Product product, bool isItemDesired)
        {
            //We create the sound player to play the notification sound.
            SoundPlayer soundPlayer;
            if (isItemDesired)
            {
                new ToastContentBuilder()
                .AddArgument("Link", product.Item_Page)
                .AddInlineImage(new Uri($"{Directory.GetCurrentDirectory()}{IMG_RES_PATH}RegularDesiredNotification.png"))
                .AddText($"\u2738 | {product.Market_Hash_Name}")
                .AddText($"Listed {product.Percentage_Off}% off for a total price of {product.Min_Price}$!")
                .AddButton(new ToastButton()
                .SetContent("Open item's page")
                .AddArgument("Link", product.Item_Page))
                .Show();
                soundPlayer = new SoundPlayer($"{Directory.GetCurrentDirectory()}{WAV_RES_PATH}RegularDesiredNotification.wav");
            }
            else
            {
                new ToastContentBuilder()
                .AddArgument("Link", product.Item_Page)
                .AddInlineImage(new Uri($"{Directory.GetCurrentDirectory()}{IMG_RES_PATH}RegularNotification.png"))
                .AddText($"\u269D | {product.Market_Hash_Name}")
                .AddText($"Listed {product.Percentage_Off}% off for a total price of {product.Min_Price}$!")
                .AddButton(new ToastButton()
                .SetContent("Open item's page")
                .AddArgument("Link", product.Item_Page))
                .Show();
                soundPlayer = new SoundPlayer($"{Directory.GetCurrentDirectory()}{WAV_RES_PATH}RegularNotification.wav");
            }
            await Task.Run(() => { soundPlayer.Play(); });
            return;
        }
        public async Task CreateGoldenNotification(Product product, bool isItemDesired)
        {
            //We create the sound player to play the notification sound.
            SoundPlayer soundPlayer;
            if (isItemDesired)
            {
                new ToastContentBuilder()
                .AddArgument("Link", product.Item_Page)
                .AddInlineImage(new Uri($"{Directory.GetCurrentDirectory()}{IMG_RES_PATH}GoldenDesiredNotification.png"))
                .AddText($"\u2739 | {product.Market_Hash_Name}")
                .AddText($"Listed {product.Percentage_Off}% off for a total price of {product.Min_Price}$!")
                .AddButton(new ToastButton()
                .SetContent("Open item's page")
                .AddArgument("Link", product.Item_Page))
                .Show();
                soundPlayer = new SoundPlayer($"{Directory.GetCurrentDirectory()}{WAV_RES_PATH}GoldenDesiredNotification.wav");
            }
            else
            {
                new ToastContentBuilder()
                .AddArgument("Link", product.Item_Page)
                .AddInlineImage(new Uri($"{Directory.GetCurrentDirectory()}{IMG_RES_PATH}GoldenNotification.png"))
                .AddText($"\u2605 | {product.Market_Hash_Name}")
                .AddText($"Listed {product.Percentage_Off}% off for {product.Min_Price}$!")
                .AddButton(new ToastButton()
                .SetContent("Open item's page")
                .AddArgument("Link", product.Item_Page))
                .Show();
                soundPlayer = new SoundPlayer($"{Directory.GetCurrentDirectory()}{WAV_RES_PATH}GoldenNotification.wav");
            }
            await Task.Run(() => { soundPlayer.Play(); });
            return;
        }
        public async Task CreateIncredibleNotification(Product product, bool isItemDesired)
        {
            //We create the sound player to play the notification sound.
            SoundPlayer soundPlayer;
            if (isItemDesired)
            {
                new ToastContentBuilder()
                .AddArgument("Link", product.Item_Page)
                .AddInlineImage(new Uri($"{Directory.GetCurrentDirectory()}{IMG_RES_PATH}IncredibleDesiredNotification.png"))
                .AddText($"\u273A | {product.Market_Hash_Name}")
                .AddText($"Listed {product.Percentage_Off}% off for {product.Min_Price}$!")
                .AddButton(new ToastButton()
                .SetContent("Open item's page")
                .AddArgument("Link", product.Item_Page))
                .Show();
                soundPlayer = new SoundPlayer($"{Directory.GetCurrentDirectory()}{WAV_RES_PATH}IncredibleDesiredNotification.wav");
            }
            else
            {
                new ToastContentBuilder()
                .AddArgument("Link", product.Item_Page)
                .AddInlineImage(new Uri($"{Directory.GetCurrentDirectory()}{IMG_RES_PATH}IncredibleNotification.png"))
                .AddText($"\u272A | {product.Market_Hash_Name}")
                .AddText($"Listed {product.Percentage_Off}% off for {product.Min_Price}$!")
                .AddButton(new ToastButton()
                .SetContent("Open item's page")
                .AddArgument("Link", product.Item_Page))
                .Show();
                soundPlayer = new SoundPlayer($"{Directory.GetCurrentDirectory()}{WAV_RES_PATH}IncredibleNotification.wav");
            }
            await Task.Run(() => { soundPlayer.Play(); });
            return;
        }
    }

}
