using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{
    [RequireComponent(typeof(Selectable))]
    public class NPCDialogue : MonoBehaviour
    {
        public DialogueData dialogue;
        public UnityEvent onDialogueEnd; // kosongin buat NPC tutorial biasa; isi buat trigger buka shop, dll

        private Selectable selectable;

        void Awake()
        {
            selectable = GetComponent<Selectable>();
            selectable.onUse += OnUse;
        }

        private void OnUse(PlayerCharacter character)
        {
            character.StopMove();
            character.FaceTorward(transform.position);
            DialoguePanel.Get()?.Show(dialogue, () => onDialogueEnd?.Invoke());
        }
    }
}