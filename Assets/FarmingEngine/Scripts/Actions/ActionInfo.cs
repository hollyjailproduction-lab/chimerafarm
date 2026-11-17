using UnityEngine;

namespace FarmingEngine
{
    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Info", order = 51)]
    public class ActionInfo : SAction
    {
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            AnimalStatusPanel.Get()?.Show(select);
        }

        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            Destructible destruct = select.GetComponent<Destructible>();
            return destruct != null && !destruct.IsDead();
        }
    }
}