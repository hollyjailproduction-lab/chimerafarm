using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{
    public class UIButtonPointer : MonoBehaviour
    {
        public GameObject arrow_prefab;
        public float bounce_speed = 3f;
        public float bounce_height = 10f;

        private static UIButtonPointer instance;
        private GameObject arrow_instance;
        private RectTransform target_button;

        void Awake()
        {
            // Guard singleton
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            // Cek dulu apakah arrow_prefab valid dan BUKAN diri sendiri
            if (arrow_prefab == null)
            {
                Debug.LogWarning("[UIButtonPointer] arrow_prefab belum di-assign!");
                return;
            }

            // PENTING: Pastikan arrow_prefab tidak punya UIButtonPointer (biar gak infinite recursion)
            if (arrow_prefab.GetComponent<UIButtonPointer>() != null)
            {
                Debug.LogError("[UIButtonPointer] arrow_prefab TIDAK BOLEH punya komponen UIButtonPointer! Hapus komponennya di prefab.");
                return;
            }

            // Buat instance panah
            arrow_instance = Instantiate(arrow_prefab, transform);
            arrow_instance.SetActive(false);
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        public void PointAt(Button button)
        {
            if (button == null || arrow_instance == null) return;
            target_button = button.GetComponent<RectTransform>();
            arrow_instance.SetActive(true);
        }

        public void Clear()
        {
            if (arrow_instance != null)
                arrow_instance.SetActive(false);
            target_button = null;
        }

        void Update()
        {
            if (target_button != null && arrow_instance != null)
            {
                Vector3 target_pos = target_button.position;
                float offsetY = Mathf.Sin(Time.time * bounce_speed) * bounce_height;
                arrow_instance.transform.position = target_pos + Vector3.up * (60f + offsetY);
            }
        }

        public static UIButtonPointer Get() { return instance; }
    }
}