using UnityEngine;
using FarmingEngine;

[RequireComponent(typeof(Selectable))]
[RequireComponent(typeof(UniqueID))]
public class Incubator : MonoBehaviour
{
    [System.Serializable]
    public class EggMapping
    {
        public ItemData egg_item;          // Telur Ayam, Telur Bebek, dll
        public CharacterData baby_animal;  // Hewan yang menetas
        public GameObject egg_visual_prefab; // Model 3D telur khusus
    }

    [Header("Settings")]
    public EggMapping[] mappings;          // Daftar telur dan hasilnya
    public int days_to_hatch = 3;          
    
    [Header("Visuals")]
    public Transform egg_spawn_point;      // Posisi di mana model 3D telur muncul (di atas mesin)
    public GameObject ready_fx;            // Partikel saat siap menetas

    private Selectable selectable;
    private string unique_id;
    private GameObject current_egg_visual;

    void Awake()
    {
        selectable = GetComponent<Selectable>();
        unique_id = GetComponent<UniqueID>().unique_id;
        if (ready_fx != null) ready_fx.SetActive(false);
    }

    void Start()
    {
        selectable.onUse += OnClickIncubator;
        UpdateVisual();
    }

    private UniqueID uid_comp;
    private string UID
    {
        get
        {
            if (uid_comp == null) uid_comp = GetComponent<UniqueID>();
            return uid_comp.unique_id;
        }
    }

    private float fx_timer = 0f;

    void Update()
    {
        fx_timer += Time.deltaTime;
        if (fx_timer > 1f)
        {
            fx_timer = 0f;
            RefreshReadyFX();
        }
    }
    private void RefreshReadyFX()
    {
        if (ready_fx == null) return;
        bool ready = HasEgg() && PlayerData.Get().day >= PlayerData.Get().GetCustomInt(UID + "_hatch_day");
        if (ready_fx.activeSelf != ready)
            ready_fx.SetActive(ready);
    }

    public void OnClickIncubator(PlayerCharacter player)
    {
        if (HasEgg())
        {
            int hatch_day = PlayerData.Get().GetCustomInt(UID + "_hatch_day");
            
            if (PlayerData.Get().day >= hatch_day)
            {
                // Cari data telur yang sedang disimpan
                string saved_egg_id = PlayerData.Get().GetCustomString(UID + "_egg_id");
                EggMapping mapping = GetMapping(saved_egg_id);

                if (mapping != null && mapping.baby_animal != null)
                {
                    Vector3 spawn_pos = transform.position + transform.right * 1.5f;
                    Character.Create(mapping.baby_animal, spawn_pos, Quaternion.identity);
                    Debug.Log($"{mapping.egg_item.title} menetas menjadi {mapping.baby_animal.title}!");
                }

                // Bersihkan Inkubator
                PlayerData.Get().SetCustomInt(UID + "_has_egg", 0);
                if (ready_fx != null) ready_fx.SetActive(false);
                UpdateVisual();
            }
            else
            {
                int days_left = hatch_day - PlayerData.Get().day;
                Debug.Log($"Belum menetas. Sisa {days_left} hari lagi.");
            }
        }
        else
        {
            Debug.Log("Inkubator kosong. Taruh telur dengan mengklik menggunakan item Telur.");
        }
    }

    public void PutEgg(ItemData egg_item)
    {
        if (HasEgg()) return;
        PlayerData.Get().SetCustomInt(UID + "_has_egg", 1); // GANTI
        PlayerData.Get().SetCustomInt(UID + "_hatch_day", PlayerData.Get().day + days_to_hatch); // GANTI
        PlayerData.Get().SetCustomString(UID + "_egg_id", egg_item.id); // GANTI
        
        UpdateVisual();
        Debug.Log($"{egg_item.title} dimasukkan ke Inkubator.");
    }

    public bool HasEgg()  // ganti dari private jadi public
    {
        return PlayerData.Get().GetCustomInt(UID + "_has_egg") == 1;
    }

    public bool IsValidEgg(ItemData item)
    {
        return GetMapping(item != null ? item.id : null) != null;
    }


    private void UpdateVisual()
    {
        // Hapus visual telur lama jika ada
        if (current_egg_visual != null) Destroy(current_egg_visual);

        if (HasEgg())
        {
            string saved_egg_id = PlayerData.Get().GetCustomString(UID + "_egg_id");
            EggMapping mapping = GetMapping(saved_egg_id);

            // Spawn model 3D telur yang sesuai
            if (mapping != null && mapping.egg_visual_prefab != null && egg_spawn_point != null)
            {
                current_egg_visual = Instantiate(mapping.egg_visual_prefab, egg_spawn_point.position, egg_spawn_point.rotation, transform);
            }
            
            if (ready_fx != null)
            {
                int hatch_day = PlayerData.Get().GetCustomInt(unique_id + "_hatch_day");
                ready_fx.SetActive(PlayerData.Get().day >= hatch_day);
            }
        }
    }

    private EggMapping GetMapping(string egg_id)
    {
        foreach (var map in mappings)
        {
            if (map.egg_item != null && map.egg_item.id == egg_id) return map;
        }
        return null;
    }
}