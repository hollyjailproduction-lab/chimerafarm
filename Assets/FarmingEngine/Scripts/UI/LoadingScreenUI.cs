using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using FarmingEngine;

public class LoadingScreenUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject loadingCanvas;
    public Slider loadingBar;
    public CanvasGroup canvasGroup;

    // Variabel statis untuk menyimpan perintah dari Main Menu
    private static string targetSceneName;
    private static bool isNewGame;

    void Start()
    {
        if (loadingCanvas != null) loadingCanvas.SetActive(true);
        
        // Jika ada perintah dari scene sebelumnya, mulai loading
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            StartCoroutine(LoadSceneAsync(targetSceneName));
        }
    }

    // FUNGSI STATIS: Ini yang akan dipanggil oleh tombol di Main Menu
    public static void StartLoading(string sceneName, bool newGame)
    {
        Debug.Log($"Mempersiapkan loading ke: {sceneName} | New Game: {newGame}");
        
        targetSceneName = sceneName;
        isNewGame = newGame;
        
        // Pindah ke Scene Loading
        SceneManager.LoadScene("LoadingScreen"); 
    }

        private IEnumerator LoadSceneAsync(string sceneName)
    {
        if (canvasGroup != null) { canvasGroup.blocksRaycasts = true; canvasGroup.alpha = 1f; }
        if (loadingBar != null) loadingBar.value = 0f;

        yield return new WaitForSeconds(0.2f);

        // HAPUS BAGIAN isNewGame DI SINI, karena TheGame.cs sudah mengurusnya.

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        
        if (operation == null) yield break; // Jaga-jaga

        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float progressValue = Mathf.Clamp01(operation.progress / 0.9f);
            
            if (loadingBar != null)
                loadingBar.value = Mathf.MoveTowards(loadingBar.value, progressValue, Time.deltaTime * 2f);

            if (operation.progress >= 0.9f)
            {
                if (loadingBar != null) loadingBar.value = 1f;
                yield return new WaitForSeconds(1f); 
                operation.allowSceneActivation = true; 
            }

            yield return null;
        }
    }
}