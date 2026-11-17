using UnityEngine;

namespace FarmingEngine
{
    public class MainMenu : MonoBehaviour
    {
        public string start_scene = "Farm"; // ganti sesuai nama scene awal game kamu

        void Awake()
        {
            TheGame.afterNewGame += OnAfterNewGame;
        }

        void OnDestroy()
        {
            TheGame.afterNewGame -= OnAfterNewGame;
        }

        public void OnClickNewGame()
        {
            gameObject.SetActive(false);
            NewGamePanel.Get()?.Show();
        }

        //Dipanggil otomatis setelah PlayerData baru dibuat, tapi sebelum pindah scene
        private void OnAfterNewGame()
        {
            PlayerData.Get().SetCustomInt("gold", 0); // gold awal
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