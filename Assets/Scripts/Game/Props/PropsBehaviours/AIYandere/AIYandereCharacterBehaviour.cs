using System;
using System.Collections.Generic;
using System.Linq;
using AIGame.Base;
using Game.Avatar;
using Game.Base;
using Game.Scene.EnterModelController;
using GameData.BaseInfo;
using GameData.Manager;
using Message;
using Network.Message;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Props.PropsBehaviours
{
    public class AIYandereCharacterBehaviour: AIPropBaseBehaviour
    {
         private Dictionary<YandereAreaType, Vector3> autoNavMesh = new Dictionary<YandereAreaType, Vector3>()
        {
            {YandereAreaType.LivingRoom,new(1.34f, 13.05f, -2.324f)},
            {YandereAreaType.Bathroom,new(1.277f, 13.05f, 8.512f)},
            {YandereAreaType.Bedroom,new(-5.53f, 13.05f, 7.951f)},
        };
         
         //key下标 ，value 位置和旋转
         private Dictionary<int, Vector3[]> sofaNavMesh = new Dictionary<int, Vector3[]>()
         {
             {0, new []{new Vector3(-2.545f,13.03f,-4.324f),new Vector3(0,180,0)}},
             {1, new []{new Vector3(-4.263f,13.03f,-6.992f),new Vector3(0,90,0)}}
         };

        public Dictionary<MoodOption, string> emoteIdDic = new Dictionary<MoodOption, string>()
        {
            {MoodOption.Normal,"0"},
            {MoodOption.Happy,"40100407"},
            {MoodOption.Sad,"40100408"},
            {MoodOption.Angry,"40100409"},
            {MoodOption.Exasperated,"40100410"},
            {MoodOption.Surprised,"40100411"},
            {MoodOption.Killer,"40100412"},
            {MoodOption.Nock,"40100413"},
        };
        
        public Dictionary<MoodOption, string> ugcWomanEmoteIdDic = new Dictionary<MoodOption, string>()
        {
            {MoodOption.Normal,"0"},
            {MoodOption.Happy,"40100416"},
            {MoodOption.Sad,"40100418"},
            {MoodOption.Angry,"401004019"},
            {MoodOption.Exasperated,"40100410"},
            {MoodOption.Surprised,"40100417"},
            {MoodOption.Killer,"40100412"},
            {MoodOption.Nock,"40100413"},
        };
        
        public Dictionary<MoodOption, string> ugcManEmoteIdDic = new Dictionary<MoodOption, string>()
        {
            {MoodOption.Normal,"0"},
            {MoodOption.Happy,"40100426"},
            {MoodOption.Sad,"40100425"},
            {MoodOption.Angry,"40100422"},
            {MoodOption.Exasperated,"40100423"},
            {MoodOption.Surprised,"40100424"},
            {MoodOption.Killer,"40100421"},
            {MoodOption.Nock,"40100413"},
        };
        public enum YadereNpcState
        {
            Normal = 1000,
            Attack = 1001,
        }
        
        public enum NpcTargetState
        {
            None,
            GoSofa,
            SitSofa
        }
        
        public enum FollowState
        {
            None,
            Normal,
            Kill
        }
        public Transform CamFollowCenter;
        public CharacterWrap NpcWrap;
        public YadereNpcState animState;
        public FollowState curFollowState = FollowState.None;
        
        public float stopDistance = 3;

        private NpcTargetState NpcState = NpcTargetState.None;
        
        private NavMeshAgent agent;
        private NavMeshHit navHit;
        private PlayerAnimationCtrl animController;
        private OtherStateController npcStateController;
        private Action<bool> runAfterAction;
        private bool isRunAfter = false;
        private float hitOffset = 0.1f;
        private Vector3 initPosition;
        private Vector3 initRotation;
        private float initSpeed = 2.2f;
        private GameObject daggerNode;
        private AvatarController roleController;
        private Animator animator;
        private string daggerPath = "Assets/Loadable/Model3D/Editor_Props/AIGames/Yandere/AI_NpcDagger.prefab";
        private string handPath = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 R Clavicle/Bip001 R UpperArm/Bip001 R Forearm/Bip001 R Hand";
        private static readonly int ForwordSpeed = Animator.StringToHash("ForwordSpeed");
        private string killerAnimTexturePath = "Assets/Loadable/Avatar/DefaultSkin/FacePaint/yandere/yandereKillerFaceTexture.png";
        private Texture killerAnimTexture;
        private Texture killerAnimCurTexture;//缓存当前玩家面部图
        private Texture killerAnimMaskTexture;//缓存当前玩家mask图
        private bool isPgcEnter;
        public bool isNpcSpeak = false;
        private int curAreaIndex = 0;
        private BudTimer followTimer;

        public override void OnInitByCreate()
        {
            base.OnInitByCreate();

            agent = this.gameObject.AddComponent<NavMeshAgent>();
            agent.speed = initSpeed;
            agent.angularSpeed = 5000;
            agent.radius = 0.25f;
            agent.height = 1.1f;
            agent.enabled = true;
            var npcInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<AINpcInfo>();
            isPgcEnter = npcInfo.id.Equals("0");
            var data = CharacterData.DeserializeObject(npcInfo.npcAvatarJson);
            
            NpcWrap = AvatarController.Inst.AddAIAvatar(data, this.gameObject);
            animController = NpcWrap.Avatar.GetComponent<PlayerAnimationCtrl>();
            npcStateController = NpcWrap.Avatar.GetComponent<OtherStateController>();
            CamFollowCenter = transform.Find("CamFollowCenter");
            roleController = NpcWrap.Avatar.GetComponent<AvatarController>();
            animator = GetComponent<Animator>();
            animController.PlayCurEyeAni();
            initPosition = this.transform.position;
            initRotation = this.transform.eulerAngles;
            this.gameObject.AddComponent<AvatarTrigger>();
            LoadDagger();
            Loader.LoadAsyncOrSync<Texture>(killerAnimTexturePath, this.gameObject, asset =>
            {
                if (asset != null)
                {
                    killerAnimTexture = asset;
                }
            });
        }

        private void LoadDagger()
        {
            Loader.LoadAsyncOrSync<GameObject>(daggerPath,this.gameObject, asset =>
            {
                if (asset != null)
                {
                    var parent = this.transform.Find(handPath);
                    daggerNode = GameObject.Instantiate(asset, parent);
                    daggerNode.transform.localPosition = new Vector3(-0.04f, 0.003f, -0.043f);
                    daggerNode.transform.localEulerAngles = new Vector3(-0.109f, 0, 0);
                    daggerNode.gameObject.SetActive(false);
                }
            });
        }


        /// <summary>
        /// 设置目标位置
        /// </summary>
        /// <param name="destination">目标点</param>
        /// <param name="stopDistance">保持距离</param>
        public void SetDestination(Vector3 destination,float stopDistance = 0.1f)
        {
            if (agent.isOnNavMesh)
            {
                agent.stoppingDistance = stopDistance;
                agent.SetDestination(destination);
            }

            float remainingDistance = agent.remainingDistance;
            if (!agent.pathPending && remainingDistance <= stopDistance && isRunAfter)
            {
                isRunAfter = false;
                StopFollowPlayer();
                runAfterAction?.Invoke(true);
            }

            if (remainingDistance <= stopDistance)
            {
                destination.y = agent.transform.position.y;
                agent.transform.LookAt(destination);
            }
        }

        
        /// <summary>
        /// 设置目标位置
        /// </summary>
        /// <param name="destination">目标点</param>
        /// <param name="stopDistance">保持距离</param>
        public void NpcGotoDestinationAndAnim(YandereAreaType cmd,int index)
        {
            if (cmd == YandereAreaType.SitSofa)
            {
                NpcState = NpcTargetState.GoSofa;
                curAreaIndex = index;
                var trans = sofaNavMesh[index];
                if (agent.isOnNavMesh)
                {
                    agent.stoppingDistance = 0.1f;
                    agent.SetDestination(trans[0]);
                }
            }
        }

        private void UpdateNpcState()
        {
            if (NpcState == NpcTargetState.GoSofa&&agent.enabled)
            {
                float remainingDistance = agent.remainingDistance;
                if (!agent.pathPending && remainingDistance <= 0.1f)
                {
                    NpcState = NpcTargetState.SitSofa;
                    agent.enabled = false;
                    var npcInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<AINpcInfo>();
                    animController.PlaySingleEmoteByPlayerId(npcInfo.id,"40200402");
                    var trans = sofaNavMesh[curAreaIndex];
                    this.transform.localPosition = trans[0];
                    this.transform.localEulerAngles = trans[1];
                }
            }
        }


        public void StartRunAfter(float speed, float distance, Action<bool> callback)
        {
            isRunAfter = true;
            curFollowState = FollowState.Kill;
            agent.enabled = true;
            agent.acceleration = 18.5f;
            agent.speed = speed;
            stopDistance = distance;
            runAfterAction = callback;
        }

        public void ChangeAttackState()
        {
            SetAnimState(YadereNpcState.Attack);
            animator.Play("yanderekiller_face",1);
        }

        public void ReSetNpc()
        {
            isRunAfter = false;
            StopFollowPlayer();
            if (agent)
            {
                agent.speed = initSpeed;
            }
            stopDistance = 3;
            SetAnimState(YadereNpcState.Normal);
        }
        public void GoToYanderArea(YandereAreaType cmd)
        {
            if (autoNavMesh.ContainsKey(cmd))
            {
                agent.enabled = true;
                animController.ResetEmoteAnimation();
                SetDestination(autoNavMesh[cmd], 0);
            }
        }

        public void SetAnimState(YadereNpcState state)
        {
            animState = state;
            NpcState = NpcTargetState.None;
            if (state == YadereNpcState.Normal)
            {
                agent.enabled = true;
                animController.ResetEmoteAnimation();
            }

            if (state == YadereNpcState.Normal)
            {
                animController.SetPlayerAniState(PlayerAniState.Idle);
            }
            else  if (state == YadereNpcState.Attack)
            {
                animController.SetPlayerAniState(PlayerAniState.Run);
            }
            SetDaggerAnimState(state);
        }

        public void SetDaggerAnimState(YadereNpcState state)
        {
            if(daggerNode == null)
                return;
            switch (state)
            {
                case YadereNpcState.Attack:
                    daggerNode.SetActive(true);
                    break;
                case YadereNpcState.Normal:
                    daggerNode.SetActive(false);
                    break;
            }
        }

        public void FollowPlayer()
        {
            var player =  AvatarController.Inst.SelfController;
            if (player != null&&agent.enabled)
            {
                SetDestination(player.transform.position, stopDistance);
            }
        }

        public void ForceStopFollow()
        {
            StopFollowPlayer();
            agent.velocity = Vector3.zero;
        }

        public void LookAtPlayer()
        {
            if (agent == null || NpcState == NpcTargetState.GoSofa || NpcState == NpcTargetState.SitSofa)
            {
                return;
            }
            var player = AvatarController.Inst.SelfController;
            if (player != null)
            {
                var playerPos = AvatarController.Inst.SelfController.transform.position;
                playerPos.y = agent.transform.position.y;
                agent.transform.LookAt(playerPos);
            }
        }

        private bool isPlayingHit = false;
        

        public override void OnColliderEnter()
        {
            if (!isRunAfter)
            {
                base.OnColliderEnter();
                PlayHitAnim();
            }
            var player = AvatarController.Inst.SelfController;
            var position = transform.position;
            var behitDir = player.transform.position - position;
            behitDir.y = 0;
            behitDir.Normalize();
            var behitOffset = position - behitDir * hitOffset;
            if (NavMesh.SamplePosition(behitOffset, out navHit, 0.1f, NavMesh.AllAreas))
            {
                agent.Warp(behitOffset);
            }
        }

        BudTimer hitTimer;
        private void PlayHitAnim()
        {
            if(isPlayingHit || NpcState == NpcTargetState.SitSofa) return;
            hitTimer = TimerManager.Inst.RunOnce("hitAnim",5,()=>{
                isPlayingHit = false;
            });
            isPlayingHit = true;
            PlayAnim(MoodOption.Nock);//撞人动画
        }


        public void SetNpcToSpawnPoint()
        {
            StopFollowPlayer();
            var desPosition =  new Vector3(1.34f, 13.05231f, -2.324f);
            this.transform.position = desPosition;
            this.transform.eulerAngles = new Vector3(0,0,0);
            animController.ResetEmoteAnimation();
            NpcState = NpcTargetState.None;
            agent.SetDestination(desPosition);
            agent.enabled = true;
        }

        public void PlayLaughAnim()
        {
            PlayAnim(MoodOption.Happy);
        }

        // private void OnCollisionEnter(Collision collision)
        // {
        //     // Debug.LogError(collision.gameObject.name);
        //     var behaviour = collision.gameObject.GetComponentInParent<AIYandereDoorBehaviour>();
        //     if (behaviour)
        //     {
        //         // behaviour.HandOpenDoor();
        //     }
        // }

        public void WaitStartFollowPlayer()
        {
            if (followTimer != null)
            {
                TimerManager.Inst.Stop(followTimer);
            }

            followTimer = TimerManager.Inst.RunOnce("followPlayer", 2, () =>
            {
                if (curFollowState != FollowState.Kill)
                {
                    animController.ResetEmoteAnimation();
                    curFollowState = FollowState.Normal;
                }
            });
        }


        public void StopFollowPlayer()
        {
            if (followTimer != null)
            {
                TimerManager.Inst.Stop(followTimer);
            }
            curFollowState = FollowState.None;
        }


        private void Update()
        {
            if (curFollowState != FollowState.None)
            {
                FollowPlayer();
            }
            UpdateNpcState();
            UpdateAnimator();
        }
        
        private void UpdateAnimator()
        {
            Vector3 velocity = agent.velocity;
            Vector3 localVelocity = transform.InverseTransformDirection(velocity);
            if (animController != null)
            {
                animController.SetFloat(ForwordSpeed, localVelocity.z);
            }
        }

        public void PlayKillerAnim()
        {
            try
            {
                PlayAnim(MoodOption.Killer);
            }
            catch
            {
                
            }
        }

        // private void OnGUI()
        // {
        //     if (GUI.Button(new Rect(500, 100, 100, 100), "去卧室"))
        //     {
        //         // isFollow = false;
        //         // var pos = new Vector3(-5.29f, 14, 7.37f);
        //         // SetDestination(pos, 0);
        //         // SetAnimState(YadereNpcState.Normal);
        //         PlayAnim(MoodOption.Happy);
        //
        //     }
        //
        //     if (GUI.Button(new Rect(650, 100, 100, 100), "去卫生间"))
        //     {
        //         PlayAnim(MoodOption.Sad);
        //
        //         // isFollow = false;
        //         // var pos = new Vector3(1f, 14, 8.22f);
        //         // SetDestination(pos, 0);
        //         // SetAnimState(YadereNpcState.Normal);
        //     }
        //
        //     if (GUI.Button(new Rect(800, 100, 100, 100), "1"))
        //     {
        //         PlayAnim(MoodOption.Angry);
        //         // isFollow = true;
        //         // SetAnimState(YadereNpcState.Attack);
        //     }
        //
        //     if (GUI.Button(new Rect(950, 100, 100, 100), "2"))
        //     {
        //         PlayAnim(MoodOption.Exasperated);
        //         // isFollow = true;
        //         // SetAnimState(YadereNpcState.Normal);
        //     }
        //
        //     if (GUI.Button(new Rect(1100, 100, 100, 100), "Attack"))
        //     {
        //         PlayAnim(MoodOption.Surprised);
        //         // isFollow = false;
        //         // var pos = new Vector3(-5.29f, 14, 7.37f);
        //         // if (agent.isOnNavMesh)
        //         // {
        //         //     agent.SetDestination(pos);
        //         // }
        //         // SetAnimState(YadereNpcState.Attack);
        //     }
        //
        //     if (GUI.Button(new Rect(1300, 100, 100, 100), "Attack"))
        //     {
        //         PlayAnim(MoodOption.Killer);
        //         // isFollow = false;
        //         // var pos = new Vector3(-5.29f, 14, 7.37f);
        //         // if (agent.isOnNavMesh)
        //         // {
        //         //     agent.SetDestination(pos);
        //         // }
        //         // SetAnimState(YadereNpcState.Attack);
        //     }
        // }

        public void PlayAnim(MoodOption op)
        {
            var npcInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<AINpcInfo>();
            string id = isPgcEnter ? emoteIdDic[op] : (npcInfo.npcGender == 1 ? ugcManEmoteIdDic[op] : ugcWomanEmoteIdDic[op]);
            animController.PlaySingleEmoteByPlayerId(npcInfo.id,id);
        }

        // private void StopAnim()
        // {
        //     PlayAniType aniType = PlayAniType.SingleLoopEnd;
        //     var emoAniConfig = animController.PlayConfigAni(emoAniDataList.ConvertToAniConfig(), aniType, () =>
        //     {
        //         onComplete?.Invoke(this);
        //     },isPlaySound:!isBanAudio);
        // }

        public void PlayAnimAndStopSpeak(MoodOption op)
        {
            StopSpeakAnim();
            PlayAnim(op);
        }

        
        public void PlayIdleAnim() {
            agent.enabled = true;
            animController.ResetEmoteAnimation();
        }

        public void PlaySpeakAnim()
        {
            if (animator!=null)
            {
                isNpcSpeak = true;
                animator.SetLayerWeight(2, 0.9f);
                AIGameSoundUtils.Inst.PlaySound(YandereConfig.NPC_Voice_Loop,this.gameObject);
            }
        }
        //
        public void StopSpeakAnim()
        {
            if (isNpcSpeak && animator != null)
            {
                isNpcSpeak = false;
                animator.SetLayerWeight(2, 0);
                agent.enabled = true;
                animController.ResetEmoteAnimation();
                AIGameSoundUtils.Inst.StopSound(YandereConfig.NPC_Voice_Loop,this.gameObject);
            }
        }

        public override void OnReset()
        {
            StopFollowPlayer();
            agent.speed = initSpeed;
            agent.Warp(initPosition);
            SetAnimState(YadereNpcState.Normal);
            animController.SetFloat(ForwordSpeed, 0);
            this.transform.position = initPosition;
            this.transform.eulerAngles = initRotation;
            TimerManager.Inst.Stop(hitTimer);
        }


        public override void OnTouchClick()
        {
            base.OnTouchClick();
            MessageHelper.Broadcast(MessageName.OnAINPCTouchClick, this);
        }
    }
}