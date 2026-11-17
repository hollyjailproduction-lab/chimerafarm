using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace FarmingEngine
{
    public class TutorialManager : MonoBehaviour
    {
        [Header("Step 1: Intro")]
        public DialogueData intro_dialogue;

        [Header("Step 2: Movement")]
        public DialogueData wuzzy_move_prompt; // "Ok, coba gerak dulu"
        public DialogueData wuzzy_after_move;  // "Nice, sekarang ayo keluar"
        public Sprite wasd_icon;               // gambar/icon WASD buat overlay
        public string wasd_label = "Tekan WASD untuk bergerak";

        [Header("Step 3: Exit House")]
        public Transform exit_door_target;

        [Header("Step 4: Pickup Items")]
        public DialogueData wuzzy_pickup_intro;
        public ItemData pacul_item;
        public ItemData siram_item;
        public DialogueData wuzzy_after_pickup;
        public Sprite pickup_icon;

        [Header("Step 5: Watering Can + Well")]
        public ItemData siram_full_item; // versi WateringCanFull
        //public DialogueData wuzzy_select_watering; // muncul setelah berhasil select
        public DialogueData wuzzy_go_to_well;      // "ayo ke sumur"
        public DialogueData wuzzy_click_well;      // "klik sumurnya"
        public DialogueData wuzzy_step6_start;
        private static TutorialManager instance;

        [Header("Step 6: Garden & Farming")]
        public DialogueData wuzzy_farming_intro;  // percakapan cara bercocok tanam, baris terakhir nyuruh cangkul 2 petak
        public DialogueData wuzzy_after_hoe;      // setelah 2 petak selesai, dikasih biji
        public ItemData seed_item;
        public int seed_quantity = 5;
        public DialogueData wuzzy_after_plant;
        public DialogueData wuzzy_after_water; 
        //public DialogueData wuzzy_step7_start;    // dialog pembuka step 7

        [Header("Step 7: Shop Tutorial")]
        public DialogueData wuzzy_after_grow;      // "Tanamanmu tumbuh! Ayo jual ke Wizzy!"
        public DialogueData wuzzy_shop_first;      // Dialog pertama saat buka toko
        public DialogueData wuzzy_shop_repeat;     // Dialog saat buka toko berikutnya
        public Transform wizzy_target;             // Posisi Wizzy
        public string mandrake_item_id = "mandrake"; // ID item mandrake di inventory
        public ItemData mandrake_seed_item;        // ItemData biji mandrake   
        public DialogueData wuzzy_step8_start;  

        public bool is_tutorial_completed = false;
        public DialogueData dialog_shop_locked;
        private bool is_shop_tutorial_active = false;
        public bool IsTutorialShopStepActive() { return is_shop_tutorial_active; }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject); // cegah duplikat kalau nggak sengaja ada 2 instance
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject); // BARU — bertahan lintas scene
        }

        void Start()
        {
            if (!PlayerData.Get().HasCustomInt("tutorial_done"))
            {
                PlayerData.Get().SetCustomInt("tutorial_done", 1);
                StartTutorial();
            }
        }

        void OnDestroy()
        {
            if (TheGame.Get() != null)
                TheGame.Get().onStartNewGame -= StartTutorial;
        }

        private void StartTutorial()
        {
            if (intro_dialogue != null)
            {
                PlayerCharacter player = PlayerCharacter.GetFirst();
                player?.StopMove();

                DialoguePanel.Get()?.Show(intro_dialogue, OnIntroDialogueEnd);
            }
        }

        private void OnIntroDialogueEnd()
        {
            StartMoveStep();
        }

        private void StartMoveStep()
        {
            DialoguePanel.Get()?.Show(wuzzy_move_prompt, OnMovePromptEnd);
        }

        private void OnMovePromptEnd()
        {
            PlayerCharacter player = PlayerCharacter.GetFirst();

            TutorialOverlay.Get()?.ShowUntil(
                () => player.IsMoving(),
                OnPlayerMoved,
                wasd_icon,
                wasd_label,
                1.5f
            );
        }

        private void OnPlayerMoved()
        {
            DialoguePanel.Get()?.Show(wuzzy_after_move, OnMoveStepEnd);
        }

        private void OnMoveStepEnd()
        {
            PlayerCharacter player = PlayerCharacter.GetFirst();
            Transform exit_target = TutorialSceneRefs.Get()?.exit_door_target;
            TutorialArrow.Get()?.PointTo(player, exit_target);

            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnArrivedAtFarm;
        }

        private void OnArrivedAtFarm(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnArrivedAtFarm; // unsubscribe, cuma sekali
            TutorialArrow.Get()?.Clear();

            DialoguePanel.Get()?.Show(wuzzy_pickup_intro, StartPickupStep); // langsung muncul begitu scene Farm kebuka
        }
        private void OnExitHouseStepEnd()
        {
            StartPickupStep();
        }

        private void StartPickupStep()
        {
            // Ambil referensi awal (untuk dinyalakan di awal)
            GameObject pacul_ind = TutorialSceneRefs.Get()?.pacul_indicator;
            GameObject siram_ind = TutorialSceneRefs.Get()?.siram_indicator;

            // Gunakan pengecekan if biasa, JANGAN pakai ?. untuk Unity Object
            if (pacul_ind != null) pacul_ind.SetActive(true);
            if (siram_ind != null) siram_ind.SetActive(true);

            PlayerCharacter player = PlayerCharacter.GetFirst();

            TutorialQuestTracker.Get()?.ShowUntil(
                () => player.InventoryData.HasItem(pacul_item.id) && player.InventoryData.HasItem(siram_item.id),
                OnItemsPickedUp,
                () =>
                {
                    int count = 0;

                    // AMBIL ULANG REFERENSI DI DALAM LAMBDA
                    // Ini penting agar jika scene berganti, kita mencari objek yang baru
                    GameObject current_pacul_ind = TutorialSceneRefs.Get()?.pacul_indicator;
                    GameObject current_siram_ind = TutorialSceneRefs.Get()?.siram_indicator;

                    if (player.InventoryData.HasItem(pacul_item.id)) 
                    { 
                        count++; 
                        // Pengecekan if biasa (bukan ?.) dan cek activeSelf agar tidak error
                        if (current_pacul_ind != null && current_pacul_ind.activeSelf) 
                            current_pacul_ind.SetActive(false); 
                    }
                    
                    if (player.InventoryData.HasItem(siram_item.id)) 
                    { 
                        count++; 
                        if (current_siram_ind != null && current_siram_ind.activeSelf) 
                            current_siram_ind.SetActive(false); 
                    }
                    
                    return $"Ambil peralatan untuk bertani ({count}/2)";
                }
            );
        }
        private void OnItemsPickedUp()
        {
            DialoguePanel.Get()?.Show(wuzzy_after_pickup, OnPickupDialogueEnd); // teks digabung sama "select watering" di sini
        }

        private void OnPickupDialogueEnd()
        {
            StartEquipWateringStep(); // BARU, ganti dari langsung manggil OnSelectDialogueEnd
        }

        private void StartEquipWateringStep()
        {
            PlayerCharacter player = PlayerCharacter.GetFirst();
            InventoryPanel panel = InventoryPanel.Get(player.player_id);
            int slot_index = FindSlotIndexOf(player.InventoryData, siram_item.id);

            if (panel != null && slot_index >= 0)
                UISlotPointer.Get()?.PointAt(panel, slot_index);

            TutorialQuestTracker.Get()?.ShowUntil(
                () => player.EquipData.GetEquippedItem(EquipSlot.Hand)?.item_id == siram_item.id,
                OnWateringEquipped,
                () => "Pilih penyiram di hotbar"
            );
        }

        private void OnWateringEquipped()
        {
            UISlotPointer.Get()?.Clear();
            DialoguePanel.Get()?.Show(wuzzy_go_to_well, OnGoToWellDialogueEnd); // langsung ke sumur, no extra dialog
        }

        private void OnSelectDialogueEnd()
        {
            DialoguePanel.Get()?.Show(wuzzy_go_to_well, OnGoToWellDialogueEnd);
        }

        private void OnGoToWellDialogueEnd()
        {
            PlayerCharacter player = PlayerCharacter.GetFirst();
            Selectable well = TutorialSceneRefs.Get()?.well_selectable;
            if (well == null) return;

            TutorialArrow.Get()?.PointTo(player, well.transform);

            TutorialQuestTracker.Get()?.ShowUntil(
                () => well.IsInUseRange(player),
                OnArrivedAtWell,
                () => "Pergi ke sumur"
            );
        }

        private void OnArrivedAtWell()
        {
            TutorialArrow.Get()?.Clear();
            DialoguePanel.Get()?.Show(wuzzy_click_well, OnClickWellDialogueEnd);
        }

        private void OnClickWellDialogueEnd()
        {
            PlayerCharacter player = PlayerCharacter.GetFirst();

            TutorialQuestTracker.Get()?.ShowUntil(
                () => player.EquipData.GetEquippedItem(EquipSlot.Hand)?.item_id == siram_full_item.id,
                OnWaterFilled,
                () => "Klik sumur untuk ambil air"
            );
        }

        private void OnWaterFilled()
        {
            Debug.Log("OnWaterFilled() dipanggil -> lanjut ke StartGardenGateStep");
            DialoguePanel.Get()?.Show(wuzzy_step6_start, StartGardenGateStep); // BARU, lanjut ke gate
        }

        private void OnStep6Start()
        {
            Debug.Log("Step 6 dimulai");
            // TODO
        }

        private int FindSlotIndexOf(InventoryData inv, string item_id)
        {
            foreach (var pair in inv.items)
            {
                if (pair.Value != null && pair.Value.item_id == item_id)
                    return pair.Key;
            }
            return -1;
        }

        private void StartGardenGateStep()
        {
            Debug.Log("StartGardenGateStep() dipanggil");

            PlayerCharacter player = PlayerCharacter.GetFirst();
            Debug.Log("player = " + player);
            Debug.Log("TutorialSceneRefs.Get() = " + TutorialSceneRefs.Get());

            Selectable gate = TutorialSceneRefs.Get()?.garden_gate_selectable;
            Debug.Log("gate = " + gate);

            if (gate == null)
            {
                Debug.Log("GATE NULL, berhenti di sini");
                return;
            }

            TutorialArrow.Get()?.PointTo(player, gate.transform);

            TutorialQuestTracker.Get()?.ShowUntil(
                () => {
                    bool in_range = gate.IsInUseRange(player);
                    return in_range;
                },
                OnArrivedAtGate,
                () => "Pergi ke pagar kebun"
            );
        }

        private void OnArrivedAtGate()
        {
            Debug.Log("OnArrivedAtGate() dipanggil -> cek Zone taman");

            TutorialArrow.Get()?.Clear();

            PlayerCharacter player = PlayerCharacter.GetFirst();
            Zone garden = TutorialSceneRefs.Get()?.garden_zone;
            Debug.Log("garden zone = " + garden);

            if (garden == null)
            {
                Debug.Log("GARDEN ZONE NULL, berhenti di sini");
                return;
            }

            TutorialQuestTracker.Get()?.ShowUntil(
                () => {
                    bool inside = garden.IsInsideXZ(player.transform.position);
                    Debug.Log($"Cek IsInsideXZ: player pos={player.transform.position}, inside={inside}");
                    return inside;
                },
                OnEnteredGarden,
                () => "Klik pagar untuk membuka"
            );
        }


        private void OnEnteredGarden()
        {
            Debug.Log("OnEnteredGarden() dipanggil -> trigger dialog farming intro");
            DialoguePanel.Get()?.Show(wuzzy_farming_intro, OnFarmingIntroEnd);
        }

        private void OnFarmingIntroEnd()
        {
            StartHoeStep();
        }

        private void StartHoeStep()
        {
            Zone garden = TutorialSceneRefs.Get()?.garden_zone;
            PlayerCharacter player = PlayerCharacter.GetFirst();
            ConstructionData hoe_soil = player.GetComponent<PlayerCharacterHoe>()?.hoe_soil; // ambil langsung dari komponen Hoe, biar gak dobel-assign asset
            if (garden == null || hoe_soil == null) return;

            TutorialQuestTracker.Get()?.ShowUntil(
                () => Construction.CountInRegion(hoe_soil, garden) >= 2,
                OnHoeComplete,
                () => $"Cangkul tanah ({Mathf.Min(Construction.CountInRegion(hoe_soil, garden), 2)}/2)"
            );
        }

        private void OnHoeComplete()
        {
            DialoguePanel.Get()?.Show(wuzzy_after_hoe, OnAfterHoeDialogueEnd);
        }

        private void OnAfterHoeDialogueEnd()
        {
            PlayerCharacter player = PlayerCharacter.GetFirst();
            player.Inventory.GainItem(seed_item, seed_quantity); // kasih biji langsung ke inventory

            StartSelectSeedStep();
        }

        private void StartSelectSeedStep()
        {
            PlayerCharacter player = PlayerCharacter.GetFirst();
            InventoryPanel panel = InventoryPanel.Get(player.player_id);
            int slot_index = FindSlotIndexOf(player.InventoryData, seed_item.id);

            if (panel != null && slot_index >= 0)
                UISlotPointer.Get()?.PointAt(panel, slot_index);

            TutorialQuestTracker.Get()?.ShowUntil(
                () => player.Crafting.IsBuildMode(), // Biji masuk mode sow
                OnSeedEquipped,  // <-- GANTI dari OnSeedSelected ke OnSeedEquipped
                () => "Klik biji ini untuk menanam"
            );
        }

        private void OnSeedEquipped()
        {
            UISlotPointer.Get()?.Clear();
            StartPlantStep();
        }

        private void StartPlantStep()
        {
            Zone garden = TutorialSceneRefs.Get()?.garden_zone;
            if (garden == null) return;

            TutorialQuestTracker.Get()?.ShowUntil(
                () => SObject.CountInRegion(seed_item.plant_data, garden) >= 2,  // <- UBAH KE 2
                OnAllPlanted,
                () => {
                    int count = Mathf.Min(SObject.CountInRegion(seed_item.plant_data, garden), 2);
                    return $"Tanam 2 biji di tanah yang telah dicangkul ({count}/2)";
                }
            );
        }

        private void OnAllPlanted()
        {
            // Setelah 2 biji tertanam, tampilkan dialog yang nyuruh menyiram
            DialoguePanel.Get()?.Show(wuzzy_after_plant, StartWaterPlantsStep);
        }

        private void StartWaterPlantsStep()
        {
            Zone garden = TutorialSceneRefs.Get()?.garden_zone;
            PlayerCharacter player = PlayerCharacter.GetFirst();
            if (garden == null || player == null) return;

            // Pastikan player sedang memegang watering can yang berisi air
            bool holding_watering_can = player.EquipData.GetEquippedItem(EquipSlot.Hand)?.item_id == siram_full_item.id;
            
            if (!holding_watering_can)
            {
                InventoryPanel panel = InventoryPanel.Get(player.player_id);
                int slot_index = FindSlotIndexOf(player.InventoryData, siram_full_item.id);
                if (panel != null && slot_index >= 0)
                    UISlotPointer.Get()?.PointAt(panel, slot_index);

                TutorialQuestTracker.Get()?.ShowUntil(
                    () => player.EquipData.GetEquippedItem(EquipSlot.Hand)?.item_id == siram_full_item.id,
                    () => {
                        UISlotPointer.Get()?.Clear();
                        StartWaterPlantsStep(); // Panggil ulang setelah equip
                    },
                    () => "Pilih kembali penyiram berisi air di hotbar"
                );
                return;
            }

            // Player sudah memegang watering can, sekarang suruh siram 2 tanaman
            TutorialQuestTracker.Get()?.ShowUntil(
                () => {
                    // Hitung manual tanah yang sudah disiram di area taman
                    int watered_count = 0;
                    foreach (Soil soil in Soil.GetAll())
                    {
                        if (garden.IsInsideXZ(soil.transform.position) && soil.IsWatered())
                        {
                            watered_count++;
                        }
                    }
                    return watered_count >= 2;
                },
                OnAllPlantsWatered,
                () => {
                    // Hitung untuk ditampilkan di UI
                    int watered_count = 0;
                    foreach (Soil soil in Soil.GetAll())
                    {
                        if (garden.IsInsideXZ(soil.transform.position) && soil.IsWatered())
                        {
                            watered_count++;
                        }
                    }
                    int count = Mathf.Min(watered_count, 2);
                    return $"Siram tanamanmu dengan penyiram ({count}/2)";
                }
            );
        }

        private void OnAllPlantsWatered()
{
    // 1. Tampilkan dialog dulu
    DialoguePanel.Get()?.Show(wuzzy_after_water, OnAfterWaterDialogueEnd);
}

private void OnAfterWaterDialogueEnd()
{
    // 2. Setelah dialog ditutup, tanaman baru tumbuh instan
    GrowPlantsInstantly();
    
    // 3. Lanjut ke step panen
    StartHarvestStep();
}

private void StartHarvestStep()
{
    PlayerCharacter player = PlayerCharacter.GetFirst();
    if (player == null) return;

    // Tunggu sampai player punya item mandrake di inventory (hasil panen)
    TutorialQuestTracker.Get()?.ShowUntil(
        () => player.InventoryData.HasItem(mandrake_item_id, 1),
        OnMandrakeHarvested,
        () => "Panen Mandrake yang sudah tumbuh"
    );
}

private void OnMandrakeHarvested()
{
    // 4. Setelah dipanen, baru munculkan dialog untuk ke toko
    DialoguePanel.Get()?.Show(wuzzy_after_grow, StartGoToShopStep);
}

        private void GrowPlantsInstantly()
        {
            Zone garden = TutorialSceneRefs.Get()?.garden_zone;
            if (garden == null) return;

            // Kumpulkan dulu ke list sementara, karena GrowPlant() akan menghancurkan objek asli
            List<Plant> plants_to_grow = new List<Plant>();
            foreach (Plant plant in Plant.GetAll())
            {
                if (garden.IsInsideXZ(plant.transform.position) && plant.IsBuilt())
                    plants_to_grow.Add(plant);
            }

            foreach (Plant plant in plants_to_grow)
            {
                if (plant.data != null && plant.data.growth_stage_prefabs.Length > 1)
                {
                    int max_stage = plant.data.growth_stage_prefabs.Length - 1;
                    if (plant.growth_stage < max_stage)
                    {
                        plant.GrowPlant(max_stage);
                        Debug.Log($"Tanaman {plant.name} dipaksa tumbuh ke stage {max_stage}!");
                    }
                }
                else
                {
                    Debug.LogWarning($"Tanaman {plant.name} tidak punya growth_stage_prefabs lebih dari 1! Cek PlantData.");
                }
            }
        }

        private void StartGoToShopStep()
        {
            PlayerCharacter player = PlayerCharacter.GetFirst();
            if (player == null) return;

            // Ambil target Wizzy dari scene refs (karena scene bisa beda)
            Transform wizzy_target = TutorialSceneRefs.Get()?.wizzy_target;
            if (wizzy_target == null)
            {
                Debug.LogWarning("[Tutorial] wizzy_target belum di-assign di TutorialSceneRefs!");
                // Tetap aktifkan flag agar tutorial toko tetap berjalan saat player interaksi dengan Wizzy
                is_shop_tutorial_active = true;
                TutorialQuestTracker.Get()?.ShowUntil(
                    () => !is_shop_tutorial_active,
                    () => { },
                    () => "Buka toko Wizzy (klik Wizzy)"
                );
                return;
            }

            TutorialArrow.Get()?.PointTo(player, wizzy_target);
            is_shop_tutorial_active = true;

            TutorialQuestTracker.Get()?.ShowUntil(
                () => !is_shop_tutorial_active,
                () => { TutorialArrow.Get()?.Clear(); },
                () => "Pergi ke toko Wizzy"
            );
        }

        public void StartShopTutorial(PlayerCharacter player, UnityEngine.Events.UnityAction open_shop_callback)
        {
            is_shop_tutorial_active = true;
            DialoguePanel.Get()?.Show(wuzzy_shop_first, () => {
                open_shop_callback?.Invoke(); // Buka panel Shop
                StartSellTabStep();
            });
        }

        private void StartSellTabStep()
        {
            Button sell_btn = ShopPanel.Get()?.GetSellTabButton();
            TutorialUIFocus.Get()?.FocusOn(sell_btn?.GetComponent<RectTransform>());

            TutorialQuestTracker.Get()?.ShowUntil(
                () => ShopPanel.Get()?.IsShowingSell() == true,
                () => {
                    TutorialUIFocus.Get()?.Clear();
                    OnSellTabSelected();
                },
                () => "Klik tombol SELL di toko"
            );
        }

        private void OnSellTabSelected()
        {
            // Cari slot mandrake — dinamis sesuai inventory player
            ShopSlot slot = ShopPanel.Get()?.GetSlotByItemId(mandrake_item_id);
            TutorialUIFocus.Get()?.FocusOn(slot?.GetComponent<RectTransform>());

            TutorialQuestTracker.Get()?.ShowUntil(
                () => {
                    var selected = ShopPanel.Get()?.GetSelectedItem();
                    return selected != null && selected.id == mandrake_item_id;
                },
                () => {
                    TutorialUIFocus.Get()?.Clear();
                    OnMandrakeSelected();
                },
                () => "Klik Mandrake untuk dijual"
            );
        }


        private void OnMandrakeSelected()
        {
            Button purchase_btn = ShopPanel.Get()?.GetPurchaseButton();
            TutorialUIFocus.Get()?.FocusOn(purchase_btn?.GetComponent<RectTransform>());

            TutorialQuestTracker.Get()?.ShowUntil(
                () => {
                    PlayerCharacter player = PlayerCharacter.GetFirst();
                    return !player.InventoryData.HasItem(mandrake_item_id, 1);
                },
                () => {
                    TutorialUIFocus.Get()?.Clear();
                    OnMandrakeSoldOut();
                },
                () => "Klik tombol SELL untuk jual"
            );
        }

        private void OnMandrakeSoldOut()
        {
            Button buy_tab = ShopPanel.Get()?.GetBuyTabButton();
            TutorialUIFocus.Get()?.FocusOn(buy_tab?.GetComponent<RectTransform>());

            TutorialQuestTracker.Get()?.ShowUntil(
                () => ShopPanel.Get()?.IsShowingSell() == false,
                () => {
                    TutorialUIFocus.Get()?.Clear();
                    OnBuyTabSelected();
                },
                () => "Klik tombol BUY"
            );
        }

        private void OnBuyTabSelected()
        {
            StartCoroutine(FocusSeedSlotDelayed());
        }

        private System.Collections.IEnumerator FocusSeedSlotDelayed()
        {
            yield return new WaitForEndOfFrame();

            ShopSlot slot = ShopPanel.Get()?.GetSlotByItemId(mandrake_seed_item.id);
            TutorialUIFocus.Get()?.FocusOn(slot?.GetComponent<RectTransform>());

            TutorialQuestTracker.Get()?.ShowUntil(
                () => {
                    var selected = ShopPanel.Get()?.GetSelectedItem();
                    return selected != null && selected.id == mandrake_seed_item.id;
                },
                () => {
                    TutorialUIFocus.Get()?.Clear();
                    OnShopSeedSelected();
                },
                () => "Klik biji Mandrake"
            );
        }

        
        // Ini tetap untuk step TOKO
        private void OnShopSeedSelected()
        {
            Button purchase_btn = ShopPanel.Get()?.GetPurchaseButton();
            TutorialUIFocus.Get()?.FocusOn(purchase_btn?.GetComponent<RectTransform>());

            TutorialQuestTracker.Get()?.ShowUntil(
                () => {
                    PlayerCharacter player = PlayerCharacter.GetFirst();
                    return player.SaveData.gold < mandrake_seed_item.buy_cost;
                },
                () => {
                    TutorialUIFocus.Get()?.Clear();
                    OnAllGoldSpent();
                },
                () => "Beli sampai uangmu habis"
            );
        }

        private void OnAllGoldSpent()
        {
            Button close_btn = ShopPanel.Get()?.GetCloseButton();
            TutorialUIFocus.Get()?.FocusOn(close_btn?.GetComponent<RectTransform>());

            TutorialQuestTracker.Get()?.ShowUntil(
                () => ShopPanel.Get()?.IsVisible() == false,
                () => {
                    TutorialUIFocus.Get()?.Clear();
                    OnShopClosed();
                },
                () => "Tutup toko (klik X)"
            );
        }

        private void OnShopClosed()
        {
            is_shop_tutorial_active = false; // Selesai
            DialoguePanel.Get()?.Show(wuzzy_step8_start, OnStep8Start);
        }

        private void OnStep8Start()
        {
            is_tutorial_completed = true;
            Debug.Log("Step 8 dimulai - Kasih makan hewan");
            // TODO
        }

        

        public static TutorialManager Get() { return instance; }
    }
}