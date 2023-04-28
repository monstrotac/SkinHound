using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkinportAnalyzer
{
    class NotificationManager
    {
        public NotificationCreator ToastNotificationCreator { get; set; }
        public List<string> NotificationHistory { get; set; }
        public NotificationManager(NotificationCreator notificationCreator, List<string> notifiedProducts)
        {
            this.ToastNotificationCreator = notificationCreator;
            this.NotificationHistory = notifiedProducts;
        }
        public NotificationManager()
        {
            ToastNotificationCreator = new NotificationCreator();
            NotificationHistory = new List<string>();
        }
        public async Task SendNotification(Product product, NotificationType notificationType, bool isDesiredProduct)
        {
            //We make sure that the notification history isn't empty to avoid raising out of bounds errors.
            if (NotificationHistory.Count > 0)
                if (VerifyNotificationHistory(product, NotificationHistory.Count - 1))
                    return;
            //We create the appropriate type of notification if the notification doesn't already exist.
            switch (notificationType)
            {
                case NotificationType.DEFAULT:
                    await ToastNotificationCreator.CreateDefaultDesiredNotification(product);
                    break;
                case NotificationType.REGULAR:
                    await ToastNotificationCreator.CreateGoodNotification(product, isDesiredProduct);
                    break;
                case NotificationType.GOLDEN:
                    await ToastNotificationCreator.CreateGoldenNotification(product, isDesiredProduct);
                    break;
                case NotificationType.INCREDIBLE:
                    await ToastNotificationCreator.CreateIncredibleNotification(product, isDesiredProduct);
                    break;
            }
            NotificationHistory.Add(product.ToString());
        }
        private bool VerifyNotificationHistory(Product product, int i)
        {
            if (product.ToString().Equals(NotificationHistory.ElementAt(i)))
                return true;
            else if (i > 0)
                return VerifyNotificationHistory(product, i - 1);
            else return false;
        }
    }
}
