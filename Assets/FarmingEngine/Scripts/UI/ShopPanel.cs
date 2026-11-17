using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{
    public class ShopPanel : UISlotPanel
    {
        [Header("Header")]
        public Text shop_title;
        public Text gold_value;

        [Header("Slot List")]
        public ShopSlot slot_prefab;
        public Transform slot_container; // Content, child dari ScrollView
        public Button buy_tab_button;
        public Button sell_tab_button;

        [Header("Description")]
        public Image desc_icon; // BARU — gambar item yang dipilih
        public Text title;
        public Text desc;
        public Text buy_cost;
        public Button button;
        public Text button_text;
        public GameObject desc_group;

        public AudioClip buy_sell_audio;

        [Header("Tutorial References")]
        public Button close_button;         // Tombol X (tutup toko)
        public Button big_buy_button;       // Tombol BUY besar di bawah
        public ShopSlot slot_to_highlight;  // Slot yang sedang di-highlight (opsional)

        public Button GetSellTabButton() { return sell_tab_button; }
        public Button GetBuyTabButton()  { return buy_tab_button; }
        public Button GetPurchaseButton() { return button; }       // 'button' adalah tombol BUY/SELL besar
        public Button GetCloseButton()    { return close_button; } // tambahkan field ini jika belum ada

        [Header("Tutorial")]
        private PlayerCharacter current_player;
        private List<ItemData> buy_items;
        private GroupData sell_group;
        private ShopSlot selected = null;
        private bool showing_sell = false;

        private List<ShopSlot> spawned_slots = new List<ShopSlot>();

        private static ShopPanel instance;

        public bool IsShowingSell() { return showing_sell; }

        [Header("Animal Setup")]
        public List<AnimalPenMapping> animal_pen_mappings = new List<AnimalPenMapping>();

        [System.Serializable]
    public class AnimalPenMapping
    {
        public ItemData animal_item;     // Item hewan yang dijual di toko (misal: Ayam)
        public GroupData pen_zone_group; // GroupData untuk kandangnya (misal: Group_KandangAyam)
    }
        protected override void Awake()
        {
            base.Awake();
            instance = this;

            onClickSlot += OnClickSlot;
            onPressAccept += OnAccept;
            onPressCancel += OnCancel;

            if (buy_tab_button != null) buy_tab_button.onClick.AddListener(() => SwitchMode(false));
            if (sell_tab_button != null) sell_tab_button.onClick.AddListener(() => SwitchMode(true));
        }

        private void SwitchMode(bool sell)
        {
            if (showing_sell == sell) return;
            showing_sell = sell;
            selected = null;
            BuildSlotList(); // BARU — bangun ulang cuma pas ganti tab
        }

        protected override void RefreshPanel()
{
    base.RefreshPanel();

    gold_value.text = current_player != null ? current_player.SaveData.gold.ToString() : "0";

    // Cuma update highlight selected, TANPA destroy/instantiate
    foreach (ShopSlot s in spawned_slots)
    {
        if (s != null)
            s.SetSelected(selected == s);
    }

    // Description
    ItemData select_item = selected?.GetItem();
    desc_group.SetActive(select_item != null);
    if (select_item != null)
    {
        if (desc_icon != null) { desc_icon.sprite = select_item.icon; desc_icon.enabled = true; }
        title.text = select_item.title;
        desc.text = select_item.desc;
        bool sell = selected.IsSell();
        int cost = (sell ? select_item.sell_cost : select_item.buy_cost);
        buy_cost.text = cost.ToString();
        button_text.text = sell ? "SELL" : "BUY";
        button.interactable = (sell && cost > 0 && CanSell(select_item)) || (!sell && cost <= current_player.SaveData.gold);
    }
}

// Method baru, TANPA Coroutine
private void DebugLayoutInfo()
{
    Debug.Log("===== SHOP PANEL DEBUG =====");
    Debug.Log($"Total slot di spawned_slots: {spawned_slots.Count}");

    for (int i = 0; i < spawned_slots.Count; i++)
    {
        ShopSlot slot = spawned_slots[i];
        if (slot == null)
        {
            Debug.LogWarning($"Slot [{i}] NULL!");
            continue;
        }
        RectTransform srt = slot.GetComponent<RectTransform>();
        Debug.Log($"Slot [{i}] {slot.name} - Pos: {srt.anchoredPosition}, Size: {srt.rect.width}x{srt.rect.height}, Active: {slot.gameObject.activeSelf}");
    }

    // Hitung juga slot yang BUKAN dari spawned_slots (slot lama yang masih ada)
    int leftover_slots = 0;
    foreach (Transform child in slot_container)
    {
        if (child.GetComponent<ShopSlot>() != null && !spawned_slots.Contains(child.GetComponent<ShopSlot>()))
            leftover_slots++;
    }
    Debug.Log($"Slot LAMA yang masih ada di Content (tidak di-destroy): {leftover_slots}");
    Debug.Log("============================");
}

private void PositionSlotsManually()
{
    if (slot_container == null || spawned_slots.Count == 0) return;

    float cellWidth = 110f;
    float cellHeight = 130f;
    float spacingX = 5f;
    float spacingY = 5f;
    int columns = 3;
    float paddingTop = 10f;
    float paddingLeft = 10f;

    for (int i = 0; i < spawned_slots.Count; i++)
    {
        RectTransform rt = spawned_slots[i].GetComponent<RectTransform>();
        
        // Anchor ke pojok kiri atas Content
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);

        int col = i % columns;
        int row = i / columns;

        rt.anchoredPosition = new Vector2(
            paddingLeft + col * (cellWidth + spacingX),
            -paddingTop - row * (cellHeight + spacingY)
        );
        
        rt.sizeDelta = new Vector2(cellWidth, cellHeight);
    }

    // Hitung tinggi Content berdasarkan jumlah baris
    RectTransform contentRT = slot_container.GetComponent<RectTransform>();
    int totalRows = Mathf.CeilToInt((float)spawned_slots.Count / columns);
    float totalHeight = paddingTop + totalRows * (cellHeight + spacingY) + 10f;

    // Set tinggi Content (jangan ubah lebar karena sudah diatur oleh anchor stretch)
    contentRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 400f);
}
        private bool CanSell(ItemData item)
        {
            return sell_group == null || item.HasGroup(sell_group);
        }

        public void ShowShop(PlayerCharacter player, string title, List<ItemData> items, GroupData sell_items)
        {
            current_player = player;
            buy_items = items;
            sell_group = sell_items;
            shop_title.text = title;
            selected = null;
            showing_sell = false;
            Show();
            BuildSlotList(); // BARU — bangun slot cuma di sini
        }

        private void BuildSlotList()
        {
            for (int i = slot_container.childCount - 1; i >= 0; i--)
            {
                Transform child = slot_container.GetChild(i);
                if (child.GetComponent<ShopSlot>() != null)
                    Destroy(child.gameObject);
            }
            spawned_slots.Clear();

            if (current_player == null || slot_prefab == null || slot_container == null)
                return;

            int index = 0;

            if (!showing_sell)
            {
                foreach (ItemData item in buy_items)
                {
                    ShopSlot slot = Instantiate(slot_prefab, slot_container);
                    slot.SetBuySlot(item, item.buy_cost);
                    RegisterSlot(slot, index);
                    spawned_slots.Add(slot);
                    index++;
                }
            }
            else
            {
                foreach (KeyValuePair<int, InventoryItemData> pair in current_player.InventoryData.items)
                {
                    InventoryItemData item = pair.Value;
                    ItemData idata = ItemData.Get(item?.item_id);
                    if (idata == null) continue;

                    bool can_sell = CanSell(idata);
                    ShopSlot slot = Instantiate(slot_prefab, slot_container);
                    slot.SetSellSlot(idata, idata.sell_cost, item.quantity, can_sell);
                    RegisterSlot(slot, index);
                    spawned_slots.Add(slot);
                    index++;
                }
            }

            slots = spawned_slots.ToArray();
            PositionSlotsManually();
        }

        public override void Show(bool instant = false)
{
    base.Show(instant);
    
    // Setelah panel aktif, rebuild layout
    if (slot_container != null)
    {
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(
            slot_container.GetComponent<RectTransform>()
        );
    }
}

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
            current_player = null;
            TheCamera.Get()?.StopFocus();
        }

        private void OnClickSlot(UISlot islot)
        {
            ShopSlot slot = (ShopSlot)islot;
            ItemData item = slot.GetItem();

            if (slot != null && item != null && selected != slot)
                selected = slot;
            else
                selected = null;

            RefreshPanel();
        }

        private void OnAccept(UISlot islot)
        {
            if (selected == islot)
                OnClickBuy();
            else
                OnClickSlot(islot);
        }

        private void OnCancel(UISlot islot)
        {
            if (selected != null)
                selected = null;
            else
                Hide();
        }

        public void OnClickBuy()
        {
            if (selected == null) return;

            // Simpan player ke variabel lokal, karena Hide() akan men-null-kan current_player
            PlayerCharacter player = current_player;
            if (player == null) return;

            bool sell = selected.IsSell();
            ItemData item = selected.GetItem();
            if (item == null) return;

            if (sell)
            {
                if (player.InventoryData.HasItem(item.id, 1) && item.sell_cost > 0 && CanSell(item))
                {
                    player.SaveData.gold += item.sell_cost;
                    player.InventoryData.RemoveItem(item.id, 1);
                    TheAudio.Get().PlaySFX("shop", buy_sell_audio);
                }
                RebuildAndReselect(item);
                return;
            }

            // Mode beli
            if (player.SaveData.gold < item.buy_cost)
            {
                Debug.Log("Uang tidak cukup!");
                return;
            }

            if (item.character_data != null)
            {
                // Beli hewan: potong uang, tutup toko, masuk mode ghost placement
                player.SaveData.gold -= item.buy_cost;
                TheAudio.Get().PlaySFX("shop", buy_sell_audio);
                Hide(); // ini juga memanggil StopFocus() di TheCamera

                // Cari zone kandang dari required_zone_group prefab hewan
                GroupData targetZoneGroup = null;
                if (item.character_data.character_prefab != null)
                {
                    Buildable buildablePrefab = item.character_data.character_prefab.GetComponent<Buildable>();
                    if (buildablePrefab != null)
                        targetZoneGroup = buildablePrefab.required_zone_group;
                }

                // Masuk build mode dulu, baru arahkan kamera (urutan penting)
                player.Crafting.CraftCharacterBuildMode(item.character_data);
                player.Crafting.build_remote = true;

                if (targetZoneGroup != null)
                {
                    CameraViewPoint vp = CameraViewPoint.GetByGroup(targetZoneGroup);
                    if (vp != null)
                    {
                        TheCamera.Get()?.ViewTemporarily(vp.transform.position, player, vp.extra_camera_offset);
                    }
                    else
                    {
                        // Fallback kalau titik belum dibuat: pakai posisi zone
                        Zone targetZone = GetZoneByGroup(targetZoneGroup);
                        if (targetZone != null)
                            TheCamera.Get()?.ViewTemporarily(targetZone.transform.position, player, Vector3.zero);
                        Debug.LogWarning("CameraViewPoint untuk group " + targetZoneGroup.name + " belum dibuat.");
                    }
                }

                Debug.Log("Masuk mode penempatan hewan (Ghost) di area kandang.");
            }
            else
            {
                // Beli item biasa
                player.SaveData.gold -= item.buy_cost;
                player.Inventory.GainItem(item, 1);
                TheAudio.Get().PlaySFX("shop", buy_sell_audio);
                RebuildAndReselect(item);
            }
        }

        private Zone GetZoneByGroup(GroupData group)
        {
            if (group == null) return null;
            
            // Loop semua zone yang ada di scene
            foreach (Zone zone in Zone.GetAll())
            {
                if (zone.zone_group == group)
                {
                    return zone;
                }
            }
            return null;
        }

        private void RebuildAndReselect(ItemData item)
        {
            BuildSlotList();

            selected = null;
            foreach (ShopSlot s in spawned_slots)
            {
                if (s.GetItem() == item)
                {
                    selected = s;
                    break;
                }
            }

            RefreshPanel(); // update gold + deskripsi + highlight LANGSUNG, nggak nunggu timer
        }

        public ItemData GetSelectedItem()
        {
            return selected != null ? selected.GetItem() : null;
        }

        public ShopSlot GetSlotByItemId(string item_id)
        {
            foreach (ShopSlot slot in spawned_slots)
            {
                ItemData item = slot.GetItem();
                if (item != null && item.id == item_id)
                    return slot;
            }
            return null;
        }

        public PlayerCharacter GetPlayer() { return current_player; }
        public static ShopPanel Get() { return instance; }
        public static bool IsAnyVisible() { return instance != null && instance.IsVisible(); }
    
    }
}