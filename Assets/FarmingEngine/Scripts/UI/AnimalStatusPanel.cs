using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FarmingEngine
{
    public class AnimalStatusPanel : UIPanel
    {
        public TMP_InputField name_input;
        public Slider hp_bar;
        public Slider hunger_bar;
        public TMP_Text hunger_text;
        public Button close_button;
        public Button feed_button;

        private static AnimalStatusPanel instance;

        private UniqueID current_uid;
        private AnimalLivestock current_livestock;
        private string default_name;

        protected override void Awake()
        {
            base.Awake();
            instance = this;

            hp_bar.interactable = false;
            hunger_bar.interactable = false;

            close_button.onClick.AddListener(() => Hide());
            feed_button.onClick.AddListener(OnFeedClick);
            name_input.onEndEdit.AddListener(OnNameChanged);
        }

        public void Show(Selectable select)
        {
            Destructible destruct = select.GetComponent<Destructible>();
            UniqueID uid = select.GetComponent<UniqueID>();
            AnimalLivestock livestock = select.GetComponent<AnimalLivestock>();

            if (destruct == null || uid == null)
                return;

            current_uid = uid;
            current_livestock = livestock;
            default_name = select.gameObject.name.Replace("(Clone)", "").Trim();

            base.Show();

            string saved_name = uid.HasCustomString("pet_name") ? uid.GetCustomString("pet_name") : default_name;
            name_input.SetTextWithoutNotify(saved_name);

            hp_bar.maxValue = destruct.GetMaxHP();
            hp_bar.value = destruct.hp;

            RefreshHunger();
        }

        private void RefreshHunger()
        {
            float progress = 0f;
            bool hungry = false;

            if (current_livestock != null)
            {
                float last_eat = PlayerData.Get().GetCustomFloat(current_livestock.GetSubUID("eat_time"));
                float now = PlayerData.Get().GetTotalTime();
                progress = Mathf.Clamp01((now - last_eat) / current_livestock.eat_interval_time);
                hungry = current_livestock.IsHungry();
            }

            hunger_bar.maxValue = 1f;
            hunger_bar.value = progress;

            string pet_name = name_input.text;
            hunger_text.text = hungry ? $"{pet_name} looks starving" : $"{pet_name} looks happy";
        }

        private void OnNameChanged(string new_name)
        {
            if (current_uid == null) return;

            new_name = new_name.Trim();
            if (string.IsNullOrEmpty(new_name))
                new_name = default_name;

            current_uid.SetCustomString("pet_name", new_name);
            name_input.SetTextWithoutNotify(new_name);
            RefreshHunger();
        }

        private void OnFeedClick()
        {
            PlayerCharacter player = PlayerCharacter.GetFirst();
            GroupData food_group = current_livestock?.eat_food_group;
            if (player == null || food_group == null || current_livestock == null) return;
            if (!player.Inventory.HasItemInGroup(food_group, 1)) return;

            player.Inventory.UseItemInGroup(food_group, 1);
            current_livestock.FeedByPlayer();
            RefreshHunger();
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
            current_uid = null;
            current_livestock = null;
        }

        public static AnimalStatusPanel Get() { return instance; }
    }
}