using UnityEngine;

namespace FarmingEngine
{
    public class UISlotPointer : MonoBehaviour
    {
        public RectTransform pointer_icon; // panah kecil, child Canvas
        public float bob_height = 15f;
        public float bob_speed = 3f;

        private static UISlotPointer instance;
        private ItemSlotPanel target_panel;
        private int target_slot = -1;

        void Awake()
        {
            instance = this;
            if (pointer_icon != null)
                pointer_icon.gameObject.SetActive(false);
        }

        void Update()
        {
            if (target_slot < 0 || target_panel == null || pointer_icon == null)
                return;

            Vector3 world_pos = target_panel.GetSlotWorldPosition(target_slot);
            float bob = Mathf.Sin(Time.time * bob_speed) * bob_height;
            pointer_icon.position = world_pos + new Vector3(0, 40f + bob, 0);
        }

        public void PointAt(ItemSlotPanel panel, int slot_index)
        {
            target_panel = panel;
            target_slot = slot_index;
            if (pointer_icon != null)
                pointer_icon.gameObject.SetActive(true);
        }

        public void Clear()
        {
            target_panel = null;
            target_slot = -1;
            if (pointer_icon != null)
                pointer_icon.gameObject.SetActive(false);
        }

        public static UISlotPointer Get() { return instance; }
    }
}