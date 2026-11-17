using UnityEngine;

namespace FarmingEngine
{
    public class TutorialArrow : MonoBehaviour
    {
        public GameObject arrow_prefab;
        public float distance_ahead = 3f;
        public float ground_offset = 0.05f;
        public LayerMask ground_layer = ~0;

        private static TutorialArrow instance;
        private GameObject arrow_instance;
        private PlayerCharacter character;
        private Transform target;

        void Awake()
        {
            instance = this;
        }

        void Update()
        {
            if (target == null || character == null)
            {
                HideArrow();
                return;
            }
            ShowArrowToward(target.position);
        }

        public void PointTo(PlayerCharacter player, Transform target_transform)
        {
            character = player;
            target = target_transform;
        }

        public void Clear()
        {
            target = null;
            character = null;
            HideArrow();
        }

        private void ShowArrowToward(Vector3 target_pos)
        {
            if (arrow_instance == null && arrow_prefab != null)
                arrow_instance = Instantiate(arrow_prefab);
            if (arrow_instance == null) return;

            arrow_instance.SetActive(true);

            Vector3 dir = target_pos - character.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            dir.Normalize();

            Vector3 pos = character.transform.position + dir * distance_ahead;
            if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f, ground_layer))
                pos = hit.point + Vector3.up * ground_offset;

            arrow_instance.transform.position = pos;
            arrow_instance.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        private void HideArrow()
        {
            if (arrow_instance != null)
                arrow_instance.SetActive(false);
        }

        public static TutorialArrow Get() { return instance; }
    }
}