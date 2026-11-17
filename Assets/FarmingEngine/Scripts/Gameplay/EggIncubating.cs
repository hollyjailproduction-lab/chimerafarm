using UnityEngine;
using FarmingEngine;

[RequireComponent(typeof(Selectable))]
[RequireComponent(typeof(UniqueID))]
[RequireComponent(typeof(Buildable))]
public class EggIncubating : MonoBehaviour
{
    [Header("Settings")]
    public CharacterData baby_animal;
    public int days_to_hatch = 3;

    [Header("Visuals")]
    public GameObject ready_fx;

    private Selectable selectable;
    private Buildable buildable;
    private UniqueID uid_comp;
    private float check_timer = 0f;
    private bool is_ready = false;

    private string UID { get { return uid_comp.unique_id; } }
    private string HatchKey { get { return UID + "_hatch_day"; } }

    void Awake()
    {
        selectable = GetComponent<Selectable>();
        buildable = GetComponent<Buildable>();
        uid_comp = GetComponent<UniqueID>();
        if (ready_fx != null) ready_fx.SetActive(false);
    }

    void Start()
    {
        selectable.onUse += OnClickEgg;
    }

    void Update()
    {
        // Masih ghost preview di build mode: jangan sentuh data apa pun
        if (buildable.IsBuilding())
            return;

        check_timer += Time.deltaTime;
        if (check_timer < 0.5f) return;
        check_timer = 0f;

        EnsureHatchDay(); // diset sekali setelah benar-benar ditaruh, aman buat load game

        bool ready = IsReady();
        if (ready != is_ready)
        {
            is_ready = ready;
            if (ready_fx != null) ready_fx.SetActive(ready);
        }
    }

    private void EnsureHatchDay()
    {
        PlayerData data = PlayerData.Get();
        if (!data.HasCustomInt(HatchKey))
            data.SetCustomInt(HatchKey, data.day + days_to_hatch);
    }

    private bool IsReady()
    {
        PlayerData data = PlayerData.Get();
        return data.HasCustomInt(HatchKey) && data.day >= data.GetCustomInt(HatchKey);
    }

    private void OnClickEgg(PlayerCharacter player)
    {
        if (buildable.IsBuilding()) return;

        PlayerData data = PlayerData.Get();

        if (!IsReady())
        {
            int days_left = data.GetCustomInt(HatchKey) - data.day;
            Debug.Log($"Belum menetas. Sisa {days_left} hari lagi."); // TODO: notifikasi UI
            return;
        }

        if (baby_animal != null)
            Character.Create(baby_animal, transform.position, transform.rotation);

        // Hapus dari data save supaya telur gak muncul lagi saat load
        data.RemoveCustomInt(HatchKey);
        data.RemoveConstruction(UID);
        Destroy(gameObject);
    }
}