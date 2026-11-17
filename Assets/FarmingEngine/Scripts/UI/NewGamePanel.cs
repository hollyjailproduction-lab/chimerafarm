using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FarmingEngine
{
    public class NewGamePanel : MonoBehaviour
    {
        public GameObject panel_root;
        public TMP_InputField name_input;
        public Button confirm_button;
        public Button cancel_button; // optional, buat batal balik ke MainMenu

        public string start_scene = "Farm";
        public GameObject main_menu; // referensi buat toggle balik

        private static NewGamePanel instance;

        void Awake()
        {
            instance = this;
            panel_root.SetActive(false);

            confirm_button.onClick.AddListener(OnConfirm);
            if (cancel_button != null)
                cancel_button.onClick.AddListener(Hide);

            TheGame.afterNewGame += OnAfterNewGame;
        }

        void OnDestroy()
        {
            TheGame.afterNewGame -= OnAfterNewGame;
        }

        public void Show()
        {
            panel_root.SetActive(true);
            name_input.text = "";
            name_input.Select();
        }

        public void Hide()
        {
            panel_root.SetActive(false);
        }

        private string pending_name = "";

        private void OnConfirm()
        {
            string farm_name = name_input.text.Trim();
            if (string.IsNullOrEmpty(farm_name))
                farm_name = "My Farm"; // default kalau dikosongin

            pending_name = farm_name;

            string filename = "save_" + System.DateTime.Now.Ticks;
            TheGame.NewGame(filename, start_scene);
        }

        //Dipanggil otomatis oleh TheGame setelah PlayerData baru dibuat, sebelum pindah scene
        private void OnAfterNewGame()
        {
            PlayerData.Get().SetCustomString("farm_name", pending_name);
        }

        public static NewGamePanel Get() { return instance; }
    }
}