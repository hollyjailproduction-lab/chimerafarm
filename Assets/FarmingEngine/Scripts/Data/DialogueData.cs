using UnityEngine;

namespace FarmingEngine
{
    [System.Serializable]
    public struct DialogueLine
    {
        public string speaker_name;
        [TextArea(2, 4)]
        public string text;
        public Sprite portrait; // opsional
    }

    [CreateAssetMenu(fileName = "Dialogue", menuName = "FarmingEngine/Dialogue", order = 60)]
    public class DialogueData : ScriptableObject
    {
        public DialogueLine[] lines;
    }
}