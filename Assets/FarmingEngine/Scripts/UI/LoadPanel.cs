using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{
    public class LoadPanel : MonoBehaviour
    {
        public GameObject panel_root;
        public Transform slot_container;
        public GameObject slot_prefab;  
        public Button close_button;
        public GameObject main_menu;
        private static LoadPanel instance;
        private List<GameObject> spawned = new List<GameObject>();

        void Awake()
        {
            instance = this;
            panel_root.SetActive(false);
            close_button.onClick.AddListener(OnClose);
        }

        public void Show()
        {
            panel_root.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            panel_root.SetActive(false);
        }

        private void OnClose()
        {
            Hide();
            main_menu.SetActive(true); // referensi GameObject MainMenu, drag di Inspector
        }

        private void Refresh()
        {
            foreach (GameObject go in spawned)
                Destroy(go);
            spawned.Clear();

            List<string> files = SaveTool.GetAllSave(PlayerData.extension);
            foreach (string file in files)
            {
                PlayerData preview = SaveTool.LoadFile<PlayerData>(file);
                if (preview != null)
                {
                    string save_name = file.Replace(PlayerData.extension, "");

                    int gold = 0;
                    if (preview.player_characters.TryGetValue(0, out PlayerCharacterData pcdata))
                        gold = pcdata.gold;

                    string farm_name = preview.HasCustomString("farm_name") 
                        ? preview.GetCustomString("farm_name") 
                        : "Unnamed Farm";

                    GameObject slot = Instantiate(slot_prefab, slot_container);
                    slot.GetComponent<SaveSlotUI>().Setup(save_name, farm_name, preview.day, gold);
                    spawned.Add(slot);
                }
            }
        }
        

        public static LoadPanel Get() { return instance; }
    }
}