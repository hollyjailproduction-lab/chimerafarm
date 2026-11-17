using UnityEngine;

namespace FarmingEngine
{
    public class GroundGridOverlay : MonoBehaviour
    {
        public GameObject grid_plane; // Drag GridPlane (yang ukurannya 5x5) ke sini
        public float height_offset = 0.02f;
        
        [Header("Grid Settings")]
        public float grid_size = 1.0f; // Ukuran 1 kotak grid (biasanya 1 unit)
        public bool snap_to_grid = true; // Biar gridnya lompat per kotak, gak gerak mulus

        private PlayerCharacter character;

        void Awake()
        {
            character = GetComponent<PlayerCharacter>();
            
            // Matikan grid di awal
            if (grid_plane != null)
                grid_plane.SetActive(false);
        }

        void Update()
        {
            if (grid_plane == null || character == null)
                return;

            // Cek apakah player sedang memegang alat
            bool holding_tool = character.EquipData.GetEquippedItem(EquipSlot.Hand) != null;

            // Nyalakan/matikan grid
            grid_plane.SetActive(holding_tool);

            if (holding_tool)
            {
                Vector3 pos = character.transform.position;
                
                if (snap_to_grid)
                {
                    // Membulatkan posisi player ke tengah kotak grid terdekat
                    // Rumus: (Posisi / Ukuran Kotak) dibulatkan ke bawah, dikali ukuran kotak, ditambah setengah ukuran kotak
                    float x = Mathf.Floor(pos.x / grid_size) * grid_size + (grid_size / 2f);
                    float z = Mathf.Floor(pos.z / grid_size) * grid_size + (grid_size / 2f);
                    pos = new Vector3(x, pos.y + height_offset, z);
                }
                else
                {
                    pos.y += height_offset;
                }

                grid_plane.transform.position = pos;
            }
        }
    }
}