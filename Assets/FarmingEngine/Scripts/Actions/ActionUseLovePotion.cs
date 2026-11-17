using UnityEngine;
using FarmingEngine;

[CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/UseLovePotion", order = 50)]
public class ActionUseLovePotion : MAction
{
    public ItemData love_potion; // drag ItemData Love Potion ke sini

    public override void DoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
    {
        AnimalLivestock animal = select != null ? select.GetComponentInParent<AnimalLivestock>() : null;
        if (animal == null || slot == null) return;

        ItemData item = slot.GetItem();
        if (item == null) return;

        animal.ApplyLovePotion();

        if (slot.GetInventoryItem() != null)
            character.Inventory.UseItem(item, 1);       // masih di inventory/hotbar
        else
            character.EquipData.RemoveItem(item.id, 1); // lagi dipegang (EquipData)
    }

    public override bool CanDoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
    {
        if (select == null || slot == null) return false;
        if (love_potion != null && slot.GetItem() != love_potion) return false;

        AnimalLivestock animal = select.GetComponentInParent<AnimalLivestock>();
        return animal != null && animal.CanUseLovePotion();
    }
}