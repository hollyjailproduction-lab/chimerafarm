using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine {

    [RequireComponent(typeof(Selectable))]
    public class ShopNPC : MonoBehaviour
    {
        public string title;
        public DialogueData intro_dialogue;

        [Header("Dialogue Settings")]
        [Tooltip("Centang = Dialog muncul setiap kali interaksi. Hilangkan centang = Dialog cuma muncul sekali (untuk tutorial).")]
        public bool repeat_dialogue = true; 

        [Header("Buy")]
        public ItemData[] items;

        [Header("Sell")]
        public GroupData sell_group;

        private Selectable selectable;
        private bool has_talked = false; // Mengganti nama dari 'greeted'

        [Header("Shop Dialogue Camera")]
        public Vector3 camera_local_offset = new Vector3(-1.2f, 1.6f, -2f); // posisi kamera relatif ke NPC
        public float camera_look_height = 1.5f; // tinggi titik yang dilihat kamera (muka NPC)
        
        [Header("Tutorial")]
        public bool is_wizzy = false; // Centang di Inspector khusus untuk NPC Wizzy

        private void Awake()
        {
            selectable = GetComponent<Selectable>();
        }

        public void OpenShop()
        {
            PlayerCharacter character = PlayerCharacter.GetNearest(transform.position);
            if (character != null)
                OpenShop(character);
        }

                public void OpenShop(PlayerCharacter player)
        {
            TutorialManager tm = TutorialManager.Get();

            // ===== 1. CEK APAKAH TOKO DIKUNCI =====
            if (tm != null && !tm.is_tutorial_completed && !(is_wizzy && tm.IsTutorialShopStepActive()))
            {
                // Fokuskan kamera ke NPC (mungkin perlu dinaikkan Y-nya)
                TheCamera.Get()?.FocusOn(transform, camera_local_offset, camera_look_height);
                
                if (tm.dialog_shop_locked != null)
                {
                    // Tampilkan dialog, LALU kembalikan kamera ke player setelah dialog ditutup
                    DialoguePanel.Get()?.Show(tm.dialog_shop_locked, () => {
                        TheCamera.Get()?.FocusOn(player.transform, Vector3.zero, 0f); 
                    });
                }
                else
                {
                    // Jika dialog kosong, langsung kembalikan kamera
                    TheCamera.Get()?.FocusOn(player.transform, Vector3.zero, 0f);
                }
                
                return; // Berhenti di sini, toko tidak dibuka
            }

            // ===== 2. JIKA AMAN, BARU BUKA TOKO =====
            TheCamera.Get()?.FocusOn(transform, camera_local_offset, camera_look_height);

            if (tm != null && is_wizzy && tm.IsTutorialShopStepActive())
            {
                tm.StartShopTutorial(player, () => ActuallyOpenShop(player));
                return;
            }

            // Logika normal
            bool should_show_dialogue = intro_dialogue != null && (repeat_dialogue || !has_talked);
            if (should_show_dialogue)
            {
                has_talked = true;
                DialoguePanel.Get()?.Show(intro_dialogue, () => ActuallyOpenShop(player));
            }
            else
            {
                ActuallyOpenShop(player);
            }
        }

        private void ActuallyOpenShop(PlayerCharacter player)
        {
            List<ItemData> buy_items = new List<ItemData>(items);
            ShopPanel.Get().ShowShop(player, title, buy_items, sell_group);
        }
    }
}