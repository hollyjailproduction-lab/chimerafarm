using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{
    /// <summary>
    /// Animal that can eat and produce products
    /// </summary>

    public enum LivestockState
    {
        Wander=0,
        MoveTo=10,
        FindFood=20,
        Eat=25,
        SeekMate=30,
        Mating=35,
        Dead=50,
    }

    public enum LivestockProduceType
    {
        DropFloor=0,
        CollectAction=10
    }

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(Destructible))]
    [RequireComponent(typeof(Character))]
    public class AnimalLivestock : MonoBehaviour
    {
        [Header("Move/Wander")]
        public WanderBehavior wander = WanderBehavior.WanderNear;
        public float wander_range = 10f;
        public float wander_interval = 10f;
        public float detect_range = 5f;

        [Header("Time")]
        public TimeType time_type = TimeType.GameHours; //Time type (days or hours) for the produce/growth time

        [Header("Eat")]
        public bool eat = true;
        public GroupData eat_food_group; //Group of food the animal can eaet
        public float eat_range = 1f; //Distance it can eat from
        public float eat_interval_time = 12f; //In game-hours (or game-days), how long before eating again

        [Header("Produce")]
        public ItemData item_produce;
        public LivestockProduceType item_collect_type; //Is the item dropped on floor or need to be collected manually
        public int item_eat_count = 1; //How many time must eat to produce
        public float item_produce_time = 24f; //In game-hours (or game-days)
        public int item_max = 1; //Maximum number of produced items on floor at a time
        public float item_max_range = 10f; //How far are items counted as in maximum

        [Header("Growth")]
        public CharacterData grow_to;
        public int grow_eat_count = 4; //How many time must eat to grow
        public float grow_time = 48f; //In game-hours (or game-days)

        [Header("Breeding")]
        public bool can_breed = true;
        public CharacterData baby_data; // Masukkan CharacterData anak hewan di sini
        public int breeding_cooldown_days = 2; // Cooldown 2 hari
        public float mate_search_range = 15f;  // jarak maksimal cari pasangan
        public float mate_range = 1.2f;        // jarak dianggap "bersentuhan" (sesuaikan ukuran hewan)
        public float mate_duration = 5f;       // lama berdekatan sebelum anak lahir
        public float mate_seek_timeout = 20f;  // menyerah kalau pasangan gak ketemu/ketangkap
        public GameObject love_fx;

        // Variabel runtime (jangan diubah di inspector)
        [HideInInspector] public bool is_in_love = false;
        private int cooldown_end_day = 0;
        private AnimalLivestock partner;
        private float mate_search_timer = 0f;
        private float mate_move_timer = 0f;
        private GameObject love_fx_instance;

        public UnityAction onAttack;
        public UnityAction onDamaged;
        public UnityAction onDeath;

        private LivestockState state;
        private Character character;
        private Selectable selectable;
        private Destructible destruct;
        private Animator animator;

        private Vector3 start_pos;

        private AnimalFood target_food;
        private Vector3 wander_target;

        private bool is_running = false;
        private bool is_active = false;

        private float state_timer = 0f;
        private float find_timer = 0f;
        private float update_timer = 0f;

        private float last_eat_time = 0f;
        private float last_grow_time = 0f;

        void Awake()
        {
            character = GetComponent<Character>();
            destruct = GetComponent<Destructible>();
            selectable = GetComponent<Selectable>();
            animator = GetComponentInChildren<Animator>();
            start_pos = transform.position;
            state_timer = 99f; //Find wander right away
            update_timer = Random.Range(-1f, 1f);

            if(wander != WanderBehavior.None)
                transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }

        void Start()
        {
            character.onAttack += OnAttack;
            destruct.onDamaged += OnDamaged;
            destruct.onDeath += OnDeath;

            last_eat_time = PlayerData.Get().GetCustomFloat(GetSubUID("eat_time"));
            last_grow_time = PlayerData.Get().GetCustomFloat(GetSubUID("grow_time"));

            if (last_eat_time < 0.01f)
                ResetEatTime();
            if (last_grow_time < 0.01f)
                ResetGrowTime();
        }

        void FixedUpdate()
        {
            if (TheGame.Get().IsPaused())
                return;

            //Optimization, dont run if too far
            float dist = (TheCamera.Get().GetTargetPos() - transform.position).magnitude;
            float active_range = Mathf.Max(detect_range * 2f, selectable.active_range * 0.8f);
            is_active = (state != LivestockState.Wander && state != LivestockState.Dead) || character.IsMoving() || dist < active_range || is_in_love;
        }

        private void Update()
        {
            //Animations
            bool paused = TheGame.Get().IsPaused();
            if (animator != null)
                animator.enabled = !paused;

            if (TheGame.Get().IsPaused())
                return;

            if (state == LivestockState.Dead || !is_active)
                return;

            state_timer += Time.deltaTime;
            find_timer += Time.deltaTime;

            //States
            if (state == LivestockState.Wander)
            {
                Debug.Log($"Cek Stuck: {character.IsStuck()} | Jarak ke target: {(wander_target - transform.position).magnitude}");
                
                if (state_timer > wander_interval && wander != WanderBehavior.None)
                {
                    state_timer = Random.Range(-1f, 1f);
                    FindWanderTarget();
                    character.MoveTo(wander_target);
                }

                //Character stuck
                if (character.IsStuck())
                    character.Stop();
            }

            if (state == LivestockState.MoveTo)
            {
                if (character.HasReachedMoveTarget())
                    StopAction();
            }

            if (state == LivestockState.FindFood)
            {
                if (target_food == null)
                {
                    ChangeState(LivestockState.Wander);
                    return;
                }

                if(!character.IsTryMoving())
                    character.MoveTo(target_food.transform.position);

                float dist = (target_food.transform.position - transform.position).magnitude;
                if (dist < eat_range)
                {
                    StartEat();
                    return;
                }

                if (state_timer > 2f && character.IsStuck())
                    StopFindFood();

                if (state_timer > 10f)
                    StopFindFood();
            }

            if (state == LivestockState.Eat)
            {
                if (target_food == null)
                {
                    ChangeState(LivestockState.Wander);
                    return;
                }

                if (state_timer > 2f)
                {
                    FinishEat(target_food);
                }
            }

            // --- Breeding ---
            if (state == LivestockState.Wander && is_in_love && partner == null)
            {
                mate_search_timer += Time.deltaTime;
                if (mate_search_timer > 1f)
                {
                    mate_search_timer = 0f;
                    TrySeekMate();
                }
            }

            if (state == LivestockState.SeekMate)
            {
                if (!IsPartnerValid()) { AbortMate(); }
                else
                {
                    mate_move_timer += Time.deltaTime;
                    if (mate_move_timer > 0.4f)
                    {
                        mate_move_timer = 0f;
                        character.MoveTo(partner.transform.position); // update terus karena pasangan juga bergerak
                    }

                    if (FlatDistanceTo(partner.transform.position) < mate_range)
                        BeginMating();
                    else if (state_timer > mate_seek_timeout)
                        AbortMate();
                }
            }

            if (state == LivestockState.Mating)
            {
                if (!IsPartnerValid()) { AbortMate(); }
                else
                {
                    FacePartner();
                    if (state_timer > mate_duration)
                        FinishMating();
                }
            }

            update_timer += Time.deltaTime;
            if (update_timer > 0.5f)
            {
                update_timer = Random.Range(-0.1f, 0.1f);
                SlowUpdate(); //Optimization
            }

            //Animations
            if (animator != null && animator.enabled)
            {
                animator.SetBool("Move", IsMoving() && IsActive());
                animator.SetBool("Run", IsRunning() && IsActive());
            }

            CheckForMate(); 
        }

        private void SlowUpdate()
        {
            if (state == LivestockState.Wander)
            {
                if (eat && IsHungry() && find_timer > 0f)
                {
                    target_food = AnimalFood.GetNearest(eat_food_group, transform.position, detect_range);
                    if (target_food != null)
                    {
                        ChangeState(LivestockState.FindFood);
                        character.Stop();
                    }
                }

                if (grow_to != null && GrowTimeFinished() && GetEatCount() >= grow_eat_count)
                {
                    Grow();
                }

                else if (item_produce != null && ProduceTimeFinished() && GetEatCount() >= item_eat_count)
                {
                    ProduceItem();
                }
            }
        }

        private void StartEat()
        {
            character.Stop();
            character.FaceTorward(target_food.transform.position);
            ChangeState(LivestockState.Eat);
            if(animator != null)
                animator.SetTrigger("Eat");
        }

        private void FinishEat(AnimalFood food)
        {
            if (food != null)
            {
                state_timer = 0f;
                food.EatFood();
                ResetEatTime();
                PlayerData.Get().SetCustomInt(GetSubUID("eat"), GetEatCount() + 1);
                ChangeState(LivestockState.Wander);
            }
        }

        private void StopFindFood()
        {
            find_timer = -5f; //Dont find try find food again for 5 secs
            ChangeState(LivestockState.Wander);
            FindWanderTarget();
            character.MoveTo(wander_target);
        }

        private void ProduceItem()
        {
            if (item_produce != null)
            {
                ResetGrowTime();
                PlayerData.Get().SetCustomInt(GetSubUID("eat"), 0);

                if (item_collect_type == LivestockProduceType.DropFloor)
                {
                    int count_animal = Character.CountInRange((CraftData)character.data, transform.position, item_max_range);
                    int count_item = Item.CountInRange((CraftData)item_produce, transform.position, item_max_range);

                    if (count_animal * item_max > count_item)
                    {
                        Item.Create(item_produce, transform.position);
                    }
                }


                if (item_collect_type == LivestockProduceType.CollectAction)
                {
                    int nb = GetProductCount();
                    if (nb < item_max)
                    {
                        PlayerData.Get().SetCustomInt(GetSubUID("product"), nb + 1);
                    }
                }
            }
        }

        private void Grow(){

            if (grow_to != null)
            {
                PlayerData.Get().RemoveCharacter(GetUID());
                PlayerData.Get().RemoveObject(GetUID());
                PlayerData.Get().SetCustomInt(GetSubUID("eat"), 0);
                ResetGrowTime();
                Destroy(gameObject);

                Character.Create(grow_to, transform.position, transform.rotation);
            }
        }

        private void FindWanderTarget()
        {
            float range = Random.Range(0f, wander_range);
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 spos = wander == WanderBehavior.WanderFar ? transform.position : start_pos;
            Vector3 pos = spos + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * range;
            wander_target = pos;
        }

        public void CollectProduct(PlayerCharacter player)
        {
            int nb = GetProductCount();
            if (nb > 0)
            {
                PlayerData.Get().SetCustomInt(GetSubUID("product"), nb - 1);
                player.Inventory.GainItem(item_produce, 1);
            }
        }

        public void MoveToTarget(Vector3 pos, bool run)
        {
            is_running = run;
            ChangeState(LivestockState.MoveTo);
            character.MoveTo(pos);
        }

        public void StopAction()
        {
            character.Stop();
            is_running = false;
            target_food = null;
            ChangeState(LivestockState.Wander);
        }

        public void ChangeState(LivestockState state)
        {
            this.state = state;
            state_timer = 0f;
        }

        public void Reset()
        {
            StopAction();
            character.Reset();
            animator.Rebind();
        }

        private void ResetEatTime()
        {
            if (time_type == TimeType.GameDays)
                PlayerData.Get().SetCustomFloat(GetSubUID("eat_time"), PlayerData.Get().day);
            if (time_type == TimeType.GameHours)
                PlayerData.Get().SetCustomFloat(GetSubUID("eat_time"), PlayerData.Get().GetTotalTime());
        }

        private void ResetGrowTime()
        {
            if (time_type == TimeType.GameDays)
                PlayerData.Get().SetCustomFloat(GetSubUID("grow_time"), PlayerData.Get().day);
            if (time_type == TimeType.GameHours)
                PlayerData.Get().SetCustomFloat(GetSubUID("grow_time"), Mathf.RoundToInt(PlayerData.Get().GetTotalTime()));
        }

        private bool EatTimeFinished()
        {
            float last_eat_time = PlayerData.Get().GetCustomFloat(GetSubUID("eat_time"));
            if (time_type == TimeType.GameDays && HasUID())
                return PlayerData.Get().day >= Mathf.RoundToInt(last_eat_time + eat_interval_time);
            if (time_type == TimeType.GameHours && HasUID())
                return PlayerData.Get().GetTotalTime() > last_eat_time + eat_interval_time;
            return false;
        }

        private bool GrowTimeFinished()
        {
            float last_grow_time = PlayerData.Get().GetCustomFloat(GetSubUID("grow_time"));
            if (time_type == TimeType.GameDays && HasUID())
                return PlayerData.Get().day >= Mathf.RoundToInt(last_grow_time + grow_time);
            if (time_type == TimeType.GameHours && HasUID())
                return PlayerData.Get().GetTotalTime() > last_grow_time + grow_time;
            return false;
        }

        private bool ProduceTimeFinished()
        {
            float last_grow_time = PlayerData.Get().GetCustomFloat(GetSubUID("grow_time"));
            if (time_type == TimeType.GameDays && HasUID())
                return PlayerData.Get().day >= Mathf.RoundToInt(last_grow_time + item_produce_time);
            if (time_type == TimeType.GameHours && HasUID())
                return PlayerData.Get().GetTotalTime() > last_grow_time + item_produce_time;
            return false;
        }

        public bool CanUseLovePotion()
        {
            return can_breed && !is_in_love && state != LivestockState.Dead
                && PlayerData.Get().day >= cooldown_end_day;
        }


        public void ApplyLovePotion()
        {
            is_in_love = true;
            mate_search_timer = 99f; // langsung cari pasangan
            ShowLoveFX();
        }

        private void CheckForMate()
        {
            if (!is_in_love) return;

            // Cari hewan lain di sekitarnya
            Collider[] hits = Physics.OverlapSphere(transform.position, mate_range);
            foreach (var hit in hits)
            {
                AnimalLivestock other = hit.GetComponent<AnimalLivestock>();
                
                // Jika menemukan hewan lain yang juga sedang jatuh cinta
                if (other != null && other != this && other.is_in_love && other.can_breed)
                {
                    // Lahirkan anak hewan!
                    if (baby_data != null)
                    {
                        Vector3 spawn_pos = transform.position + new Vector3(1f, 0f, 0f); // Spawn di sebelah kanan
                        Character.Create(baby_data, spawn_pos, Quaternion.identity);
                        Debug.Log("Breeding berhasil! Anak hewan telah lahir.");
                    }

                    // Reset status cinta
                    this.is_in_love = false;
                    other.is_in_love = false;

                    // Set cooldown 2 hari untuk keduanya
                    this.cooldown_end_day = PlayerData.Get().day + breeding_cooldown_days;
                    other.cooldown_end_day = PlayerData.Get().day + other.breeding_cooldown_days;
                    
                    break; // Hentikan pencarian karena sudah kawin
                }
            }
        }

        private void TrySeekMate()
        {
            AnimalLivestock mate = FindMate();
            if (mate == null) return;

            partner = mate;
            mate.partner = this;

            foreach (AnimalLivestock a in new[] { this, mate })
            {
                a.target_food = null;
                a.character.Stop();
                a.ChangeState(LivestockState.SeekMate);
                a.mate_move_timer = 99f; // langsung gerak
            }
        }

        private AnimalLivestock FindMate()
        {
            AnimalLivestock nearest = null;
            float min_dist = 9999f;

            Collider[] hits = Physics.OverlapSphere(transform.position, mate_search_range);
            foreach (Collider hit in hits)
            {
                AnimalLivestock other = hit.GetComponentInParent<AnimalLivestock>();
                if (other == null || other == this) continue;
                if (!other.is_in_love || !other.can_breed || other.partner != null) continue;
                if (other.state == LivestockState.Dead) continue;
                if (other.character.data != character.data) continue; // harus spesies sama

                float d = (other.transform.position - transform.position).magnitude;
                if (d < min_dist) { min_dist = d; nearest = other; }
            }
            return nearest;
        }

        private bool IsPartnerValid()
        {
            return partner != null && partner.is_in_love && partner.partner == this
                && partner.state != LivestockState.Dead;
        }

        private float FlatDistanceTo(Vector3 pos)
        {
            Vector3 d = pos - transform.position;
            d.y = 0f;
            return d.magnitude;
        }

        private void AbortMate()
        {
            // Tetap jatuh cinta, cuma batal ngejar pasangan ini
            if (partner != null && partner.partner == this)
                partner.partner = null;
            partner = null;
            character.Stop();
            ChangeState(LivestockState.Wander);
        }

        private void BeginMating()
        {
            StartMatingSelf();
            if (partner != null && partner.state != LivestockState.Mating)
                partner.StartMatingSelf();
        }

        private void StartMatingSelf()
        {
            character.Stop();
            ChangeState(LivestockState.Mating);
        }

        private void FacePartner()
        {
            Vector3 dir = partner.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 5f * Time.deltaTime);
        }

        private void FinishMating()
        {
            AnimalLivestock p = partner;

            if (baby_data != null && p != null)
            {
                Vector3 spawn_pos = (transform.position + p.transform.position) * 0.5f + transform.right * 0.8f;
                Character.Create(baby_data, spawn_pos, transform.rotation);
            }

            EndLove();
            if (p != null) p.EndLove();
        }

        private void EndLove()
        {
            is_in_love = false;
            partner = null;
            cooldown_end_day = PlayerData.Get().day + breeding_cooldown_days;
            HideLoveFX();
            ChangeState(LivestockState.Wander);
        }

        private void ShowLoveFX()
        {
            if (love_fx != null && love_fx_instance == null)
                love_fx_instance = Instantiate(love_fx, transform.position + Vector3.up * 1.5f, Quaternion.identity, transform);
        }

        private void HideLoveFX()
        {
            if (love_fx_instance != null)
                Destroy(love_fx_instance);
        }

        private void OnDamaged()
        {
            if (IsDead())
                return;

            if (onDamaged != null)
                onDamaged.Invoke();

            if (animator != null)
                animator.SetTrigger("Damaged");
        }

        private void OnDeath()
        {
            state = LivestockState.Dead;

            if (onDeath != null)
                onDeath.Invoke();

            if (animator != null)
                animator.SetTrigger("Death");
        }

        void OnAttack()
        {
            if (animator != null)
                animator.SetTrigger("Attack");
        }

        public bool IsHungry()
        {
            return eat && EatTimeFinished();
        }

        public bool IsDead()
        {
            return character.IsDead();
        }

        public bool IsActive()
        {
            return is_active;
        }

        public bool IsMoving()
        {
            return character.IsMoving();
        }

        public bool IsRunning()
        {
            return character.IsMoving() && is_running;
        }

        public bool HasProduct()
        {
            return PlayerData.Get().GetCustomInt(GetSubUID("product")) > 0;
        }

        public int GetProductCount()
        {
            return PlayerData.Get().GetCustomInt(GetSubUID("product"));
        }

        public int GetEatCount()
        {
            return PlayerData.Get().GetCustomInt(GetSubUID("eat"));
        }

        public bool HasUID()
        {
            return character.HasUID();
        }

        public string GetUID()
        {
            return character.GetUID();
        }

        public string GetSubUID(string tag)
        {
            return character.GetSubUID(tag);
        }

        public void FeedByPlayer()
        {
            state_timer = 0f;
            ResetEatTime();
            PlayerData.Get().SetCustomInt(GetSubUID("eat"), GetEatCount() + 1);
            ChangeState(LivestockState.Wander);
            if (animator != null)
                animator.SetTrigger("Eat");
        }
    }

}
