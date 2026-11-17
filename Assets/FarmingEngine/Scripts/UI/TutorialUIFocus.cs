using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{
    public class TutorialUIFocus : MonoBehaviour
    {
        [Header("Overlay (CanvasGroup)")]
        public CanvasGroup overlay_canvas_group;   // GameObject dengan CanvasGroup untuk overlay gelap
        public float fade_speed = 6f;

        [Header("Arrow")]
        public GameObject arrow_prefab;
        public Vector3 arrow_offset = new Vector3(0f, 80f, 0f);
        public float bounce_speed = 3f;
        public float bounce_height = 8f;

        private static TutorialUIFocus instance;
        private GameObject arrow_instance;
        private RectTransform current_target;
        private bool is_active = false;

        void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;

            // Sembunyikan overlay di awal
            if (overlay_canvas_group != null)
            {
                overlay_canvas_group.alpha = 0f;
                overlay_canvas_group.blocksRaycasts = false;
                overlay_canvas_group.interactable = false;
                overlay_canvas_group.gameObject.SetActive(false);
            }

            // Buat instance panah
            if (arrow_prefab != null)
            {
                if (arrow_prefab.GetComponent<TutorialUIFocus>() != null)
                {
                    Debug.LogError("[TutorialUIFocus] arrow_prefab tidak boleh punya komponen TutorialUIFocus!");
                }
                else
                {
                    arrow_instance = Instantiate(arrow_prefab, transform);
                    arrow_instance.SetActive(false);
                }
            }
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        public void FocusOn(RectTransform target)
        {
            Debug.Log($"[Focus] FocusOn DIPANGGIL! target={target}, overlayCG={overlay_canvas_group}");

            Clear();

            if (target == null)
            {
                Debug.LogError("[Focus] Target NULL — cek referensi tombol di ShopPanel!");
                return;
            }

            current_target = target;
            is_active = true;

            // Aktifkan overlay
            if (overlay_canvas_group != null)
            {
                Debug.Log($"[Focus] Aktivasi overlay. Alpha sebelum={overlay_canvas_group.alpha}, activeSelf={overlay_canvas_group.gameObject.activeSelf}");
                overlay_canvas_group.gameObject.SetActive(true);
                overlay_canvas_group.blocksRaycasts = true;
                overlay_canvas_group.interactable = true;
            }
            else
            {
                Debug.LogError("[Focus] overlay_canvas_group NULL! Assign di Inspector!");
            }

            // Naikkan tombol target ke atas overlay
            Canvas target_canvas = target.GetComponent<Canvas>();
            if (target_canvas == null)
                target_canvas = target.gameObject.AddComponent<Canvas>();
            target_canvas.overrideSorting = true;
            target_canvas.sortingOrder = 100;

            if (target.GetComponent<GraphicRaycaster>() == null)
                target.gameObject.AddComponent<GraphicRaycaster>();

            // Tampilkan panah
            if (arrow_instance != null)
            {
                arrow_instance.SetActive(true);
                arrow_instance.transform.SetAsLastSibling();
            }
        }

        public void Clear()
        {
            is_active = false;

            if (current_target != null)
            {
                Canvas c = current_target.GetComponent<Canvas>();
                if (c != null) c.overrideSorting = false;
            }
            current_target = null;

            if (overlay_canvas_group != null)
            {
                overlay_canvas_group.blocksRaycasts = false;
                overlay_canvas_group.interactable = false;
            }

            if (arrow_instance != null)
                arrow_instance.SetActive(false);
        }

        void Update()
        {
            // Fade in/out overlay
            if (overlay_canvas_group != null)
            {
                float target_alpha = is_active ? 1f : 0f;
                overlay_canvas_group.alpha = Mathf.MoveTowards(
                    overlay_canvas_group.alpha,
                    target_alpha,
                    fade_speed * Time.deltaTime
                );
    

                // Nonaktifkan GameObject setelah fade out selesai
                if (!is_active && overlay_canvas_group.alpha < 0.01f)
                {
                    if (overlay_canvas_group.gameObject.activeSelf)
                        overlay_canvas_group.gameObject.SetActive(false);
                }
            }

            // Panah naik-turun
            if (current_target != null && arrow_instance != null && arrow_instance.activeSelf)
            {
                float bob = Mathf.Sin(Time.time * bounce_speed) * bounce_height;
                arrow_instance.transform.position = current_target.position + arrow_offset + Vector3.up * bob;
            }
        }

        public static TutorialUIFocus Get() { return instance; }
    }
}