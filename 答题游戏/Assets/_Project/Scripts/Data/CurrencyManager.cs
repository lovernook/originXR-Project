using UnityEngine;

namespace OriginXR.Data
{
    /// <summary>
    /// 货币管理器（PlayerPrefs 持久化）
    /// </summary>
    public static class CurrencyManager
    {
        private const string KEY_GOLD = "Currency_Gold";
        private const string KEY_DIAMOND = "Currency_Diamond";

        public static int Gold
        {
            get => PlayerPrefs.GetInt(KEY_GOLD, 500);
            set { PlayerPrefs.SetInt(KEY_GOLD, value); PlayerPrefs.Save(); }
        }

        public static int Diamond
        {
            get => PlayerPrefs.GetInt(KEY_DIAMOND, 20);
            set { PlayerPrefs.SetInt(KEY_DIAMOND, value); PlayerPrefs.Save(); }
        }

        public static bool SpendGold(int amount)
        {
            if (Gold < amount) return false;
            Gold -= amount;
            return true;
        }

        public static bool SpendDiamond(int amount)
        {
            if (Diamond < amount) return false;
            Diamond -= amount;
            return true;
        }

        public static void AddGold(int amount) { Gold += amount; }
        public static void AddDiamond(int amount) { Diamond += amount; }
    }
}
