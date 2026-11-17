using UnityEngine;

namespace FarmingEngine
{
    public class BobbingIndicator : MonoBehaviour
    {
        public float bob_height = 0.2f;
        public float bob_speed = 2f;

        private Vector3 base_pos;
        private Transform cam;

        void Start()
        {
            base_pos = transform.position;
            cam = Camera.main?.transform;
        }

        void Update()
        {
            if (cam == null) return;

            // 1. Efek naik turun
            float y = Mathf.Sin(Time.time * bob_speed) * bob_height;
            transform.position = base_pos + Vector3.up * y;

            // 2. Y-Axis Billboard: hanya menghadap kamera secara horizontal
            // Triknya: samakan tinggi (Y) target dengan tinggi panel
            Vector3 lookPos = cam.position;
            lookPos.y = transform.position.y; // <-- INI KUNCI UTAMANYA

            // LookAt akan memutar panel ke arah lookPos,
            // dan Vector3.up memastikan panel tetap tegak (tidak miring)
            transform.LookAt(lookPos, Vector3.up);
        }
    }
}