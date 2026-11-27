using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FarmingEngine;

public class ConfirmSellPanel : MonoBehaviour
{
    public static ConfirmSellPanel Instance { get; private set; }

    [Header("UI References")]
    public GameObject panelRoot;
    public TextMeshProUGUI messageText;
    public Button yesButton;
    public Button noButton;

    // Menyimpan data hewan sementara
    private AnimalLivestock targetAnimal;
    private int sellPrice;
    private PlayerCharacter currentPlayer;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (panelRoot != null) panelRoot.SetActive(false);
        
        yesButton.onClick.AddListener(OnConfirmSell);
        noButton.onClick.AddListener(OnCancelSell);
    }

        public void ShowConfirmation(AnimalLivestock animal, int price, PlayerCharacter player)
    {
        targetAnimal = animal;
        sellPrice = price;
        currentPlayer = player;

        string animalName = animal.name; // Default: nama GameObject

        // Ambil komponen Character untuk mendapatkan nama dari Data
        Character charComp = animal.GetComponent<Character>();
        if (charComp != null && charComp.data != null)
        {
            animalName = charComp.data.title; // Misal: "Jackalope Adult"
        }

        // Mengubah teks
        messageText.text = $"Apakah kamu yakin ingin menjual {animalName} seharga {price} gold?";
        
        panelRoot.SetActive(true);
    }

    private void OnConfirmSell()
    {
        if (targetAnimal != null && currentPlayer != null)
        {
            // 1. Beri Uang
            currentPlayer.SaveData.gold += sellPrice;
            Debug.Log($"Berhasil menjual {targetAnimal.name} seharga {sellPrice} gold!");

            // 2. Hapus dari Save File
            string animalUID = targetAnimal.GetUID();
            PlayerData.Get().RemoveCharacter(animalUID);
            PlayerData.Get().RemoveObject(animalUID);

            // 3. Hancurkan Objek di Scene
            Destroy(targetAnimal.gameObject);
        }

        ClosePanel();
    }

    private void OnCancelSell()
    {
        ClosePanel();
    }

    private void ClosePanel()
    {
        targetAnimal = null;
        currentPlayer = null;
        panelRoot.SetActive(false);
    }
}