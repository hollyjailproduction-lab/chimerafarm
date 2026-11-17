using UnityEngine;

namespace FarmingEngine
{
    public class TutorialSceneRefs : MonoBehaviour
    {
        [Header("Isi sesuai kebutuhan step di scene ini")]
        public Transform exit_door_target;       // cuma relevan di scene RumahChimera
        public Transform[] pickup_item_positions; // cuma relevan di scene Farm
        public Transform well_target;             // dst, tambah sesuai kebutuhan
        public Selectable well_selectable;
        public GameObject pacul_indicator;
        public GameObject siram_indicator;
        private static TutorialSceneRefs instance;
        public Selectable garden_gate_selectable; // pagar/pintu taman
        public Zone garden_zone; 

        public Transform wizzy_target;         // <-- TAMBAHKAN INI
        public ShopNPC wizzy_npc; 

        

        void Awake() { instance = this; }
        void OnDestroy() { if (instance == this) instance = null; }

        public static TutorialSceneRefs Get() { return instance; }
    }
}