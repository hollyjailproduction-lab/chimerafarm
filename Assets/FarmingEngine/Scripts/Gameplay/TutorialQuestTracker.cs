using UnityEngine;
using TMPro;
using UnityEngine.Events;
using System;

namespace FarmingEngine
{
    public class TutorialQuestTracker : UIPanel
    {
        public TMP_Text quest_text;

        private static TutorialQuestTracker instance;

        private Func<bool> wait_condition;
        private UnityAction on_met;
        private Func<string> text_provider;

        protected override void Awake()
        {
            base.Awake();
            instance = this;
        }

        protected override void Update()
        {
            base.Update();

            if (wait_condition == null)
                return;

            if (quest_text != null && text_provider != null)
                quest_text.text = text_provider.Invoke();

            if (wait_condition.Invoke())
            {
                // 1. Simpan callback ke variabel lokal DULU
                UnityAction callback = on_met;
                
                // 2. Bersihkan state SEBELUM memanggil callback
                // Ini mencegah bug di mana callback baru langsung terhapus
                wait_condition = null;
                on_met = null;
                text_provider = null;

                // 3. Sembunyikan panel
                Hide();

                // 4. Baru panggil callback-nya
                callback?.Invoke();
            }
        }

        public void ShowUntil(Func<bool> condition, UnityAction on_condition_met, Func<string> text_getter)
        {
            wait_condition = condition;
            on_met = on_condition_met;
            text_provider = text_getter;
            Show();
        }

        public static TutorialQuestTracker Get() { return instance; }
    }
}