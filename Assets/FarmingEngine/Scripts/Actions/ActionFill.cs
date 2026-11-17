using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// Fill a jug with water (or other)
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Fill", order = 50)]
    public class ActionFill : MAction
    {
        public ItemData filled_item;

        //Merge action
        public override void DoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {
            if (select.HasGroup(merge_target))
            {
                InventoryItemData real_data = slot.GetInventoryItem(); // data asli di InventoryData

                if (real_data == null && filled_item != null)
                {
                    // Item ini lagi di-equip di tangan (data aslinya kosong di inventory) -> swap langsung di EquipData
                    InventoryItemData equipped = character.EquipData.GetEquippedItem(EquipSlot.Hand);
                    if (equipped != null)
                    {
                        character.EquipData.UnequipItem(EquipSlot.Hand);
                        character.EquipData.EquipItem(EquipSlot.Hand, filled_item.id, equipped.durability, equipped.uid);
                    }
                }
                else
                {
                    // Perilaku lama: item beneran ada di slot inventory biasa
                    InventoryData inventory = slot.GetInventory();
                    inventory.RemoveItemAt(slot.index, 1);
                    character.Inventory.GainItem(inventory, filled_item, 1);
                }
            }
        }

    }

}