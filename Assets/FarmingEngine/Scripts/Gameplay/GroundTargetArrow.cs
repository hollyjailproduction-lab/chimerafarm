using UnityEngine;

namespace FarmingEngine
{
    public class GroundTargetArrow : MonoBehaviour
    {
        public GameObject arrow_prefab; // objek 3D flat (quad/plane) dengan texture panah, default menghadap +Z
        public float distance_ahead = 3f; // jarak arrow dari kaki player
        public float ground_offset = 0.05f; // dikit di atas tanah biar gak z-fighting
        public float search_range = 999f;
        public LayerMask ground_layer = ~0;

        private GameObject arrow_instance;
        private PlayerCharacter character;

        void Awake()
        {
            character = GetComponent<PlayerCharacter>(); // taruh script ini di prefab PlayerCharacter
        }

        void Update()
        {
            if (character == null)
                return;

            GroupData target_group = GetHeldMergeTargetGroup(character);
            Selectable target = target_group != null 
                ? Selectable.GetNearestGroup(target_group, character.transform.position, search_range) 
                : null;

            if (target == null || target.IsInUseRange(character))
            {
                HideArrow();
                return;
            }

            ShowArrowToward(target.transform.position);
        }

        private GroupData GetHeldMergeTargetGroup(PlayerCharacter character)
        {
            InventoryItemData equipped = character.EquipData.GetEquippedItem(EquipSlot.Hand);
            ItemData idata = ItemData.Get(equipped?.item_id);
            if (idata == null)
                return null;

            foreach (SAction action in idata.actions)
            {
                if (action is MAction maction && maction.merge_target != null && maction.show_guide_arrow)
                    return maction.merge_target;
            }
            return null;
        }

        private void ShowArrowToward(Vector3 target_pos)
        {
            if (arrow_instance == null && arrow_prefab != null)
                arrow_instance = Instantiate(arrow_prefab);

            if (arrow_instance == null)
                return;

            arrow_instance.SetActive(true);

            Vector3 dir = target_pos - character.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f)
                return;
            dir.Normalize();

            Vector3 pos = character.transform.position + dir * distance_ahead;

            // Snap ke permukaan tanah
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
    }
}