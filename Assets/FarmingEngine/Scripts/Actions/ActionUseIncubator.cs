using UnityEngine;
using FarmingEngine;

[CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/UseIncubator", order = 50)]
public class ActionUseIncubator : MAction
{
    public override void DoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
    {
        Incubator incubator = select != null ? select.GetComponent<Incubator>() : null;
        ItemData item = slot != null ? slot.GetItem() : null;
        if (incubator == null || item == null) return;

        incubator.PutEgg(item);

        if (slot.GetInventoryItem() != null)
            character.Inventory.UseItem(item, 1);        // masih di inventory/hotbar
        else
            character.EquipData.RemoveItem(item.id, 1);  // lagi dipegang
    }

    public override bool CanDoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
    {
        if (select == null || slot == null) return false;

        Incubator incubator = select.GetComponent<Incubator>();
        ItemData item = slot.GetItem();
        if (incubator == null || item == null) return false;

        return incubator.IsValidEgg(item) && !incubator.HasEgg();
    }
}