using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FarmingEngine
{
    public class SaveSlotUI : MonoBehaviour
    {
        public TMP_Text info_text;
        public Button select_button; // taruh di ROOT object (nutupin seluruh kotak)
        public Button delete_button; // child, kotak kecil, misal pojok kanan atas
        public TMP_Text farm_name_text;

        private string save_name;

        public void Setup(string save_name, string farm_name, int day, int gold)
        {
            this.save_name = save_name;
            farm_name_text.text = farm_name;
            info_text.text = $"Day {day}   Coin: {gold}";

            select_button.onClick.RemoveAllListeners();
            select_button.onClick.AddListener(() => TheGame.Load(save_name));

            if (delete_button != null)
            {
                delete_button.onClick.RemoveAllListeners();
                delete_button.onClick.AddListener(() =>
                {
                    TheGame.DeleteGame(save_name);
                    LoadPanel.Get()?.Show();
                });
            }
        }
    }
}