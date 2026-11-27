using UnityEngine;
using FarmingEngine;

[CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/FeedAnimal", order = 50)]
public class ActionFeedAnimal : MAction
{
    public ItemData food_item; // Drag item Mandrake ke sini
    public int friendship_gain = 10; // Tambah hati hewan

    public override void DoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
    {
        AnimalLivestock animal = select.GetComponent<AnimalLivestock>();
        
        if (animal != null && slot.GetItem() == food_item)
        {
            // 1. Hilangkan 1 makanan dari tangan/inventory
            character.Inventory.UseItem(food_item, 1); 

            // 2. BERI TAHU HEWANNYA BAHWA DIA SEDANG MAKAN
            animal.FeedByPlayer(); // <--- INI KUNCI RAHASIANYA

            Debug.Log($"Memberi makan {food_item.title} ke {animal.name}!");
            
            // 3. (Opsional) Animasi player mengambil/memberi makan
            character.TriggerAnim("take", animal.transform.position); 
        }
    }

    public override bool CanDoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
    {
        AnimalLivestock animal = select.GetComponent<AnimalLivestock>();
        
        // Hanya bisa dilakukan jika target adalah hewan DAN player memegang makanan ini
        return animal != null && slot.GetItem() == food_item;
    }
}