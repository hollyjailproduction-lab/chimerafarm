using UnityEngine;
using FarmingEngine;

[CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/SellAnimal", order = 50)]
public class ActionSellAnimal : SAction
{
    [Header("Harga Jual")]
    public int sell_price = 50; // Set harga di sini (beda untuk prefab bayi dan dewasa)

    public override void DoAction(PlayerCharacter character, Selectable select)
    {
        AnimalLivestock animal = select.GetComponent<AnimalLivestock>();
        if (animal != null)
        {
            // Panggil UI Konfirmasi, jangan langsung jual
            if (ConfirmSellPanel.Instance != null)
            {
                ConfirmSellPanel.Instance.ShowConfirmation(animal, sell_price, character);
            }
            else
            {
                Debug.LogError("ConfirmSellPanel tidak ditemukan di scene!");
            }
        }
    }

    public override bool CanDoAction(PlayerCharacter character, Selectable select)
    {
        return select.GetComponent<AnimalLivestock>() != null;
    }
}