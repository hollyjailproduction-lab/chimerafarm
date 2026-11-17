using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using TMPro;

namespace FarmingEngine
{
    public class TutorialOverlay : UIPanel
    {
        public Image icon;
        public TMP_Text label;

        private static TutorialOverlay instance;

        private Func<bool> wait_condition;
        private UnityAction on_condition_met;
        private float hold_timer = 0f;
        private float required_hold = 0f;
        private bool waiting = false;

        protected override void Awake()
        {
            base.Awake();
            instance = this;
        }

        protected override void Update()
        {
            base.Update();

            if (!waiting)
                return;

            bool condition_true = wait_condition != null && wait_condition.Invoke();

            if (condition_true)
            {
                hold_timer += Time.deltaTime;
                if (hold_timer >= required_hold)
                {
                    waiting = false;
                    Hide();
                    on_condition_met?.Invoke();
                    on_condition_met = null;
                    wait_condition = null;
                }
            }
            else
            {
                hold_timer = 0f;
            }
        }

        public void ShowUntil(Func<bool> condition, UnityAction on_met, Sprite display_icon = null, string display_text = null, float hold_duration = 0f)
        {
            wait_condition = condition;
            on_condition_met = on_met;
            required_hold = hold_duration;
            hold_timer = 0f;
            waiting = true;

            if (icon != null && display_icon != null) icon.sprite = display_icon;
            if (label != null && display_text != null) label.text = display_text;

            Show();
        }

        public static TutorialOverlay Get() { return instance; }
    }
}