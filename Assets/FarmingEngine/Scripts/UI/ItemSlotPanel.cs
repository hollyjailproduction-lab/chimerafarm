using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{
    /// <summary>
    /// Generic parent UI panel that manages multiple item slots (Inventory/Equip/Storage, etc)
    /// </summary>

    public class ItemSlotPanel : UISlotPanel
    {
        public bool limit_one_item = false; //If true, only 1 item per slot

        public UnityAction<ItemSlot> onSelectSlot;
        public UnityAction<ItemSlot, ItemSlot> onMergeSlot;

        protected PlayerCharacter current_player = null;
        protected InventoryType inventory_type;
        protected string inventory_uid;
        protected int inventory_size = 99;

        protected int selected_slot = -1;
        protected int selected_right_slot = -1;

        //Slot yang lagi "dipegang" sebagai hand tool, dikunci biar item lain gak nimpa
        protected int active_hand_slot = -1;
        protected int active_plant_slot = -1;
        protected ItemData active_hand_item = null;

        private static List<ItemSlotPanel> slot_panels = new List<ItemSlotPanel>();

        protected override void Awake()
        {
            base.Awake();
            slot_panels.Add(this);

            for (int i = 0; i < slots.Length; i++)
                ((ItemSlot) slots[i]).Hide();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            slot_panels.Remove(this);
        }

        protected override void Start()
        {
            base.Start();

            PlayerControlsMouse.Get().onRightClick += (Vector3, Selectable) => { CancelSelection(); };

            onClickSlot += OnClick;
            onRightClickSlot += OnClickDrop;   // klik kanan -> langsung drop
            onDoubleClickSlot += OnClickRight; // double click -> fallback popup (Split, dll)
            onLongClickSlot += OnClickDrop;    // tahan -> langsung drop (mobile)
            onDragStart += OnDragStart;
            onDragEnd += OnDragEnd;
            onDragTo += OnDragTo;
            onPressAccept += OnClick;
            onPressUse += OnClickRight;
            onPressCancel += OnCancel;

            InitPanel();
        }

        protected override void Update()
        {
            base.Update();

            InitPanel(); //Try to init panel if its not already
        }

        public virtual void InitPanel()
        {
            if (!IsPlayerSet())
            {
                PlayerUI player_ui = GetComponentInParent<PlayerUI>();
                PlayerCharacter player = player_ui ? player_ui.GetPlayer() : PlayerCharacter.GetFirst();
                if (player != null && current_player == null)
                    current_player = player; //Set default player
            }
        }

        protected override void RefreshPanel()
        {
            base.RefreshPanel();

            InventoryData inventory = GetInventory();

            if (inventory != null)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    ItemSlot slot = (ItemSlot) slots[i];

                    //Slot ini lagi jadi hand tool aktif -> selalu baca ulang dari EquipData (auto-sync, misal abis di-fill)
                    if (i == active_hand_slot)
                    {
                        InventoryItemData equipped_now = current_player?.EquipData.GetEquippedItem(EquipSlot.Hand);
                        ItemData equipped_data_now = ItemData.Get(equipped_now?.item_id);

                        if (equipped_data_now != null)
                        {
                            active_hand_item = equipped_data_now;
                            slot.SetSlot(active_hand_item, 1, true);
                            continue;
                        }
                        else
                        {
                            active_hand_slot = -1;
                            active_hand_item = null;
                        }
                    }

                    InventoryItemData invdata = inventory.GetInventoryItem(i);
                    ItemData idata = ItemData.Get(invdata?.item_id);

                    if (invdata != null && idata != null)
                    {
                        bool is_highlighted = selected_slot == slot.index || selected_right_slot == slot.index || slot.index == active_plant_slot;
                        slot.SetSlot(idata, invdata.quantity, is_highlighted);
                        slot.SetDurability(idata.GetDurabilityPercent(invdata.durability), ShouldShowDurability(idata, invdata.durability));
                        slot.SetFilter(GetFilterLevel(idata, invdata.durability));
                    }
                    else if (i < inventory_size)
                    {
                        slot.SetSlot(null, 0, false);
                    }
                    else
                    {
                        slot.Hide();
                    }
                }

                ItemSlot sslot = GetSelectedSlot();
                if (sslot != null && sslot.GetItem() == null)
                    CancelSelection();
            }
        }

        protected bool ShouldShowDurability(ItemData idata, float durability)
        {
            int durabi = idata.GetDurabilityPercent(durability);
            return idata.HasDurability() && durabi < 100 && (idata.durability_type != DurabilityType.Spoilage || durabi <= 50);
        }

        protected int GetFilterLevel(ItemData idata, float durability)
        {
            int durabi = idata.GetDurabilityPercent(durability);
            if (idata.HasDurability() && durabi <= 40 && idata.durability_type == DurabilityType.Spoilage)
            {
                return durabi <= 20 ? 2 : 1;
            }
            return 0;
        }

        private void OnClick(UISlot uislot)
        {
            if (uislot != null)
            {
                //Cancel right click and action selector
                int previous_right_select = selected_right_slot;
                ActionSelectorUI.Get(GetPlayerID()).Hide();
                selected_right_slot = -1;

                int slot = uislot.index;

                if (active_plant_slot >= 0 && slot != active_plant_slot)
                {
                    PlayerUI.Get(GetPlayerID())?.CancelSelection(); // ini otomatis trigger CancelBuilding()
                    active_plant_slot = -1;
                }

                ItemSlot selslot = GetSelectedSlotInAllPanels();

                //Klik ulang di slot yang lagi jadi hand tool aktif -> unequip
                if (slot == active_hand_slot)
                {
                    InventoryData inv0 = GetInventory();
                    PlayerCharacter player0 = GetPlayer();
                    if (player0 != null && inv0 != null)
                    {
                        player0.Inventory.UnequipItem(EquipSlot.Hand);
                        inv0.UnreserveSlot(active_hand_slot);
                    }
                    active_hand_slot = -1;
                    active_hand_item = null;
                    return;
                }

                //Cancel action selector
                if (slot == previous_right_select)
                {
                    CancelSelection();
                    return;
                }

                ItemSlot islot = uislot as ItemSlot;

                //Merge two slots
                if (islot != null && selslot != null)
                {
                    MergeSlots(selslot, islot);
                    if (onMergeSlot != null)
                        onMergeSlot.Invoke(selslot, islot);
                }
                //Select / Equip / Auto action
                else if (islot.GetCraftable() != null)
                {
                    CancelSelectionAll();
                    GetPlayer()?.Crafting.CancelBuilding();
                    selected_slot = slot;

                    ItemData idata = islot?.GetItem();
                    if (idata != null && idata.construction_data != null)
                    {
                        GetPlayer()?.Crafting.BuildItemBuildMode(islot.GetInventory(), slot);
                        return; // jangan lanjut ke action selector
                    }
                    PlayerCharacter player = GetPlayer();
                    InventoryData inv = islot.GetInventory();

                    ActionEquip equip_action = FindEquipAction(idata);
                    ActionPlant plant_action = FindPlantAction(idata);

                    if (equip_action != null && idata != null && idata.equip_slot == EquipSlot.Hand
                        && equip_action.CanDoAction(player, islot) && player != null && inv != null)
                    {
                        InventoryItemData invdata_new = inv.GetInventoryItem(slot);
                        if (invdata_new != null)
                        {
                            //Kembalikan alat lama (kalau ada) ke slot asalnya
                            InventoryItemData currently_equipped = player.EquipData.GetEquippedItem(EquipSlot.Hand);
                            if (currently_equipped != null)
                            {
                                player.EquipData.UnequipItem(EquipSlot.Hand);
                                inv.UnreserveSlot(active_hand_slot);

                                InventoryItemData target_check = inv.GetInventoryItem(active_hand_slot);
                                bool slot_free = active_hand_slot >= 0 && (target_check == null || target_check.quantity <= 0);

                                if (slot_free)
                                    inv.AddItemAt(currently_equipped.item_id, active_hand_slot, 1, currently_equipped.durability, currently_equipped.uid);
                                else
                                    inv.AddItem(currently_equipped.item_id, 1, currently_equipped.durability, currently_equipped.uid);
                            }

                            //Equip item yang baru diklik
                            inv.RemoveItemAt(slot, 1);
                            player.EquipData.EquipItem(EquipSlot.Hand, idata.id, invdata_new.durability, invdata_new.uid);
                            inv.ReserveSlot(slot);

                            active_hand_slot = slot;
                            active_hand_item = idata;
                            selected_slot = -1;

                            PlayerData.Get().SetCustomInt("hand_slot_idx_" + player.player_id, slot);
                        }
                    }
                    else if (equip_action != null && equip_action.CanDoAction(player, islot))
                    {
                        equip_action.DoAction(player, islot); // equipment non-Hand (topi, baju, dll)
                        selected_slot = -1;
                    }
                    else if (plant_action != null && plant_action.CanDoAction(player, islot))
                    {
                        if (slot == active_plant_slot)
                        {
                            // Klik ulang di slot yang sama -> cancel mode sow
                            PlayerUI.Get(GetPlayerID())?.CancelSelection();
                            active_plant_slot = -1;
                        }
                        else
                        {
                            plant_action.DoAction(player, islot);
                            active_plant_slot = slot;
                        }
                        selected_slot = -1; // tetap -1, biar gak numbuk logic merge
                    }
                    else
                    {
                        // Item lain: makanan, mandrake, material, dll -> munculin popup aksi (Eat/Drop/dst)
                        if (idata != null && islot.GetInventoryItem() != null && idata.actions.Length > 0)
                        {
                            ActionSelectorUI.Get(GetPlayerID()).Show(islot);
                        }
                        selected_slot = -1;
                    }

                    if (onSelectSlot != null)
                        onSelectSlot.Invoke(islot);
                }
            }
        }

        private ActionPlant FindPlantAction(ItemData idata)
        {
            if (idata == null)
                return null;
            foreach (SAction action in idata.actions)
            {
                if (action is ActionPlant)
                    return (ActionPlant)action;
            }
            return null;
        }

        private ActionEquip FindEquipAction(ItemData idata)
        {
            if (idata == null)
                return null;
            foreach (SAction action in idata.actions)
            {
                if (action is ActionEquip)
                    return (ActionEquip) action;
            }
            return null;
        }

        //Dipanggil pas klik-kanan / tahan -> langsung drop, tanpa popup
        private void OnClickDrop(UISlot uislot)
        {
            selected_slot = -1;
            selected_right_slot = -1;
            ActionSelectorUI.Get(GetPlayerID()).Hide();

            ItemSlot islot = uislot as ItemSlot;
            PlayerCharacter player = GetPlayer();
            if (islot?.GetItem() == null || islot.GetInventoryItem() == null || player == null)
                return;

            //Jangan bisa drop slot yang lagi jadi hand tool aktif (harus di-unequip dulu lewat klik biasa)
            if (islot.index == active_hand_slot)
                return;

            player.Inventory.DropItem(islot.GetInventory(), islot.index);
        }

        //Fallback popup lama (Equip/Drop/Split) -- sekarang dipicu double-click
        private void OnClickRight(UISlot uislot)
        {
            //Cancel select
            selected_slot = -1; 
            selected_right_slot = -1;
            ActionSelectorUI.Get(GetPlayerID()).Hide();

            //Run auto actions
            ItemSlot islot = uislot as ItemSlot;
            ItemData idata = islot?.GetItem();
            AAction aaction = idata?.FindAutoAction(GetPlayer(), islot);
            aaction?.DoAction(GetPlayer(), islot);

            //Show action selector
            if (idata != null && islot?.GetInventoryItem() != null && idata.actions.Length > 0)
            {
                selected_right_slot = islot.index;
                ActionSelectorUI.Get(GetPlayerID()).Show(islot);
            }
        }

        private void OnDragStart(UISlot slot)
        {
            CancelSelection();
        }

        private void OnDragEnd(UISlot aslot)
        {
            PlayerControlsMouse mouse = PlayerControlsMouse.Get();
            if (mouse.IsMouseOverUI())
                return;

            //Drag to selectable
            ItemSlot slot = aslot as ItemSlot;
            if (slot != null && slot.GetItem() != null)
            {
                PlayerCharacter player = GetPlayer();
                Selectable select = mouse.GetNearestRaycastList(mouse.GetPointingPos());
                MAction maction = slot.GetItem().FindMergeAction(select);
                if (player != null && maction != null
                    && select.IsInUseRange(player)
                    && maction.CanDoAction(player, slot, select))
                {
                    maction.DoAction(player, slot, select);
                }
            }
        }

        private void OnDragTo(UISlot slot, UISlot target)
        {
            if (slot != null && target != null)
            {
                ItemSlot islot = slot as ItemSlot;
                ItemSlot itarget = target as ItemSlot;
                MergeSlots(islot, itarget);
                if (onMergeSlot != null)
                    onMergeSlot.Invoke(islot, itarget);
            }
        }

        private void OnCancel(UISlot slot)
        {
            ItemSlotPanel.CancelSelectionAll();
            UISlotPanel.UnfocusAll();
            active_plant_slot = -1;
        }

        public void SetInventory(InventoryType type, string uid, int size)
        {
            inventory_type = type;
            inventory_uid = uid;
            inventory_size = size;

            InventoryData idata = InventoryData.Get(type, uid);
            if(idata != null)
                idata.size = size;
        }

        public void SetPlayer(PlayerCharacter player)
        {
            current_player = player;
        }

        public int GetPlayerID()
        {
            return current_player ? current_player.player_id : 0;
        }

        //Dipakai buat restore active hand slot setelah load game (dipanggil dari InventoryPanel.InitPanel)
        public void RestoreActiveHandSlot(int slot, ItemData item)
        {
            active_hand_slot = slot;
            active_hand_item = item;
            InventoryData inv = GetInventory();
            if (inv != null && slot >= 0)
                inv.ReserveSlot(slot);
        }

        public void MergeSlots(ItemSlot selected_slot, ItemSlot clicked_slot)
        {
            if (selected_slot != null && clicked_slot != null && current_player != null)
            {
                ItemSlot slot1 = selected_slot;
                ItemSlot slot2 = clicked_slot;
                ItemData item1 = slot1.GetItem();
                ItemData item2 = slot2.GetItem();

                if (slot1 == slot2)
                {
                    CancelSelection();
                    return;
                }

                //Check merge actions
                if (item1 != null && item2 != null)
                {
                    MAction action1 = item1.FindMergeAction(item2);
                    MAction action2 = item2.FindMergeAction(item1);

                    if (action1 != null && action1.CanDoAction(current_player, slot1, slot2))
                    {
                        DoMergeAction(action1, slot1, slot2);
                        return;
                    }

                    else if (action2 != null && action2.CanDoAction(current_player, slot2, slot1))
                    {
                        DoMergeAction(action2, slot2, slot1);
                        return;
                    }
                }

                //Move item
                MoveItem(slot1, slot2);
            }
        }

        private void DoMergeAction(MAction action, ItemSlot slot_action, ItemSlot slot_other)
        {
            if (slot_action == null || slot_other == null || current_player == null)
                return;

            action.DoAction(current_player, slot_action, slot_other);

            CancelPlayerSelection();
        }

        public void MoveItem(ItemSlot slot1, ItemSlot slot2)
        {
            ItemData item1 = slot1.GetItem();
            if (item1 == null || current_player == null)
                return;

            current_player.Inventory.MoveItem(slot1, slot2, limit_one_item);
            CancelPlayerSelection();
        }

        public void UseItem(ItemSlot slot, int quantity=1)
        {
            InventoryData inventory1 = slot.GetInventory();
            if (current_player != null && inventory1 != null)
                inventory1.RemoveItemAt(slot.index, quantity);
        }

        public void CancelSelection()
        {
            selected_slot = -1;
            selected_right_slot = -1;
        }

        public void CancelPlayerSelection()
        {
            CancelSelection();
            if (current_player != null)
            {
                PlayerUI player_ui = PlayerUI.Get(current_player.player_id);
                if (player_ui != null)
                    player_ui.CancelSelection();
            }
        }

        public bool HasSlotSelected()
        {
            return selected_slot >= 0;
        }

        public int GetSelectedSlotIndex()
        {
            return selected_slot;
        }

        public ItemSlot GetSlotByIndex(int slot_index)
        {
            foreach (ItemSlot slot in slots)
            {
                if (slot.index == slot_index)
                    return slot;
            }
            return null;
        }

        public ItemSlot GetSelectedSlot()
        {
            return GetSlotByIndex(selected_slot);
        }

        public ItemSlot GetActiveHandSlot()
        {
            return active_hand_slot >= 0 ? GetSlotByIndex(active_hand_slot) : null;
        }
        public Vector3 GetSlotWorldPosition(int slot)
        {
            ItemSlot islot = GetSlotByIndex(slot);
            if (islot != null)
            {
                RectTransform slotRect = islot.GetRect();
                return slotRect.position;
            }
            return Vector3.zero;
        }

        public string GetInventoryUID()
        {
            return inventory_uid;
        }

        public InventoryData GetInventory()
        {
            return InventoryData.Get(inventory_type, inventory_uid);
        }

        public bool IsInventorySet()
        {
            return inventory_type != InventoryType.None;
        }

        public bool IsPlayerSet()
        {
            return current_player != null;
        }

        public PlayerCharacter GetPlayer()
        {
            return current_player;
        }

        public static void CancelSelectionAll()
        {
            foreach (ItemSlotPanel panel in slot_panels)
                panel.CancelSelection();
        }

        public static ItemSlot GetSelectedSlotInAllPanels()
        {
            foreach (ItemSlotPanel panel in slot_panels)
            {
                ItemSlot slot = panel.GetSelectedSlot();
                if (slot != null)
                    return slot;
            }
            return null;
        }

        public static ItemSlot GetDragSlotInAllPanels()
        {
            foreach (ItemSlotPanel panel in slot_panels)
            {
                ItemSlot slot = panel.GetDragSlot();
                if (slot != null)
                    return slot;
            }
            return null;
        }

        public static ItemSlotPanel Get(InventoryType type)
        {
            foreach (ItemSlotPanel panel in slot_panels)
            {
                if (panel != null && panel.inventory_type == type)
                    return panel;
            }
            return null;
        }

        public static ItemSlotPanel Get(string inventory_uid)
        {
            foreach (ItemSlotPanel panel in slot_panels)
            {
                if (panel != null && panel.inventory_uid == inventory_uid)
                    return panel;
            }
            return null;
        }

        public static new List<ItemSlotPanel> GetAll()
        {
            return slot_panels;
        }
    }

}