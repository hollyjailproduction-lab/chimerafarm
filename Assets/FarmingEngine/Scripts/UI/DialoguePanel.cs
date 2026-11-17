using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace FarmingEngine
{
    public class DialoguePanel : UIPanel
    {
        public TMP_Text name_text;
        public TMP_Text body_text;
        public Image portrait_image;
        public Button next_button;
        public Button panel_click_area;
        public GameObject end_button_container;

        private static DialoguePanel instance;

        private DialogueLine[] lines;
        private int current_index;
        private UnityAction on_end;
        private PlayerCharacter locked_player;

        protected override void Awake()
        {
            base.Awake();
            instance = this;
            next_button.onClick.AddListener(OnPanelClicked);
            panel_click_area.onClick.AddListener(OnPanelClicked);
            if (end_button_container != null)
                end_button_container.SetActive(false);
        }

        public void OnPanelClicked()
        {
            if (end_button_container != null && end_button_container.activeSelf)
                return;
            Next();
        }

        public void Show(DialogueData data, UnityAction on_dialogue_end = null)
        {
            if (data == null || data.lines == null || data.lines.Length == 0)
                return;

            lines = data.lines;
            current_index = 0;
            on_end = on_dialogue_end;

            if (end_button_container != null)
                end_button_container.SetActive(false);
            if (next_button != null)
                next_button.gameObject.SetActive(true);

            locked_player = PlayerCharacter.GetFirst();
            locked_player?.StopMove();
            locked_player?.DisableControls();

            base.Show();
            RefreshLine();
        }

        private void RefreshLine()
        {
            DialogueLine line = lines[current_index];
            if (name_text != null) name_text.text = line.speaker_name;
            if (body_text != null) body_text.text = line.text;
            if (portrait_image != null)
            {
                portrait_image.enabled = line.portrait != null;
                portrait_image.sprite = line.portrait;
            }
        }

        public void Next()
        {
            current_index++;
            if (current_index >= lines.Length)
            {
                if (next_button != null)
                    next_button.gameObject.SetActive(false);

                if (end_button_container != null)
                    end_button_container.SetActive(true);
                else
                    Hide();
                return;
            }
            RefreshLine();
        }

        public void OnClickEndAction()
        {
            Hide();
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
            locked_player?.EnableControls();
            locked_player = null;
            on_end?.Invoke();
        }

        public static DialoguePanel Get() { return instance; }
    }
}