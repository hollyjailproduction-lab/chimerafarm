using UnityEngine;
using UnityEngine.SceneManagement;
using FarmingEngine;
using TMPro; // Tambahkan ini untuk Input Field

namespace FarmingEngine
{
    public class MainMenu : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject newGamePanel; // Drag NewGamePanel ke sini
        public TMP_InputField nameInput; // Drag InputField ke sini

        // Variabel sementara untuk menyimpan nama yang diketik
        public static string playerInputName = "Farmer"; 

        void Awake()
        {
            TheGame.afterNewGame += OnAfterNewGame;
        }

        void OnDestroy()
        {
            TheGame.afterNewGame -= OnAfterNewGame;
        }

        // 1. TOMBOL NEW GAME: Cuma buka panel, jangan load game dulu
        public void OnClickNewGame()
        {
            if (newGamePanel != null)
                newGamePanel.SetActive(true); 
        }

        // 2. TOMBOL CONFIRM: Dipencet setelah player selesai ngisi nama
        public void OnConfirmName()
        {
            // Simpan nama yang diketik ke variabel statis
            if (nameInput != null)
                playerInputName = nameInput.text;

            // Tutup panel
            if (newGamePanel != null)
                newGamePanel.SetActive(false);

            // Baru panggil Loading Screen
            LoadingScreenUI.StartLoading("ChimeraFarm1", true); 
        }

        // Dipanggil otomatis setelah PlayerData baru dibuat
        private void OnAfterNewGame()
        {
            PlayerData.Get().SetCustomInt("gold", 0); // gold awal
            
            // Simpan nama yang tadi diinput ke Save File PlayerData
            // Kita pakai CustomString bawaan FarmingEngine
            PlayerData.Get().SetCustomString("player_name", playerInputName);
            
            Debug.Log("Game Baru Dibuat! Nama Player: " + playerInputName);
        }

        public void OnClickLoadGame()
        {
            LoadPanel.Get()?.Show();
        }

        public void OnClickExit()
        {
            Application.Quit();
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
        }
    }
}