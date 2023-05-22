using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkinHound
{
    public class SkinHoundConfiguration
    {
        public SkinHoundConfiguration()
        {
            Desired_Weapons = new List<string>();
            Desired_Weapons_Min_Discount_Threshold = 0;
            Good_Discount_Threshold = 0;
            Great_Discount_Threshold = 0;
            Outstanding_Discount_Threshold = 0;
            Minimum_Worth_Value = 0;
            Minutes_Between_Queries = 5;
            Notify_On_All_Desired_Weapons = false;
            Notifications_Enabled = true;
            Currency = "CAD";
        }

        //The list of desired weapons, which will bypass the default discount.
        public static List<string> Desired_Weapons { get; set; }
        //This value is used to bypass 'Great discounts' or 'Oustanding discounts' and allows us to show items we've listed as desired in the listOfDesiredWeapons.
        public static double Desired_Weapons_Min_Discount_Threshold { get; set; }
        //The minimum discount which needs to be applied for the offer to be shown.
        public static double Good_Discount_Threshold { get; set; }
        //Great discounts are usually discounts which you can make a decent profit off of.
        public static double Great_Discount_Threshold { get; set; }
        //Outstanding discounts are discounts that rarely happen. 
        public static double Outstanding_Discount_Threshold { get; set; }
        //Any item with a value lower than the min desired price will not be shown even if they have a great discount.
        public static double Minimum_Worth_Value { get; set; }
        //This var dictates at which interval (in minutes) we make calls to Skinport to refresh the store.
        public static int Minutes_Between_Queries { get; set; }
        //Having this set to true will make it so that you will receive notifications for all desired weapons above or equal the Desired_Weapons_Min_Discount_Threshold.
        public static bool Notify_On_All_Desired_Weapons { get; set; }
        //Are Windows notifications enabled or not
        public static bool Notifications_Enabled { get; set; }
        //In what currency should the prices be shown?
        public static string Currency { get; set; }
        public static void SetNewConfig(ConfigurationObject obj)
        {
            Desired_Weapons = obj.Desired_Weapons;
            Desired_Weapons_Min_Discount_Threshold = obj.Desired_Weapons_Min_Discount_Threshold;
            Good_Discount_Threshold = obj.Good_Discount_Threshold;
            Great_Discount_Threshold = obj.Great_Discount_Threshold;
            Outstanding_Discount_Threshold = obj.Outstanding_Discount_Threshold;
            Minimum_Worth_Value = obj.Minimum_Worth_Value;
            Minutes_Between_Queries = obj.Minutes_Between_Queries;
            Notify_On_All_Desired_Weapons = obj.Notify_On_All_Desired_Weapons;
            Notifications_Enabled = obj.Notifications_Enabled;
            Currency = obj.Currency;
        }
    }
    //This class is used to quickly and effortlessly set a new config.
    public class ConfigurationObject
    {
        //The list of desired weapons, which will bypass the default discount.
        public List<string> Desired_Weapons { get; set; }
        //This value is used to bypass 'Great discounts' or 'Oustanding discounts' and allows us to show items we've listed as desired in the listOfDesiredWeapons.
        public double Desired_Weapons_Min_Discount_Threshold { get; set; }
        //The minimum discount which needs to be applied for the offer to be shown.
        public double Good_Discount_Threshold { get; set; }
        //Great discounts are usually discounts which you can make a decent profit off of.
        public double Great_Discount_Threshold { get; set; }
        //Outstanding discounts are discounts that rarely happen. 
        public double Outstanding_Discount_Threshold { get; set; }
        //Any item with a value lower than the min desired price will not be shown even if they have a great discount.
        public double Minimum_Worth_Value { get; set; }
        //This var dictates at which interval (in minutes) we make calls to Skinport to refresh the store.
        public int Minutes_Between_Queries { get; set; }
        //Having this set to true will make it so that you will receive notifications for all desired weapons above or equal the Desired_Weapons_Min_Discount_Threshold.
        public bool Notify_On_All_Desired_Weapons { get; set; }
        //Are Windows notifications enabled or not
        public bool Notifications_Enabled { get; set; }
        //In what currency should the prices be shown?
        public string Currency { get; set; }
    }
}
