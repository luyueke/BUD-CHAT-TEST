using System.Collections;
using UnityEngine;

namespace Game.KinematicCharacter
{
    public class CharacterFootFlag :MonoBehaviour
    {
        PlayerStateController Self;

        PlayerStateController Target;

        static string _effectPath = "Assets/Loadable/Avatar/CharacterBody/CallNpcEffect/s9npcSelect/s9AINpcArrow.prefab";

        public void SetTarget(PlayerStateController self,PlayerStateController target) {
            Self = self;
            Target = target;
        }

        public static CharacterFootFlag LoadEffect(Transform parent)
        {
            var prefab = Loader.Load<GameObject>(_effectPath).RetainAsset();
            if (prefab == null)
            {
                LoggerUtils.LogError($"加载资源异常 path = {_effectPath}");
                return null;
            }
            var Effect = GameObject.Instantiate(prefab, parent).transform;
            Effect.gameObject.SetActive(true);
            return Effect.gameObject.AddComponent<CharacterFootFlag>();
        }

        private void Update()
        {
            if (Self != null && Target != null)
            {
                Vector3 direction = Target.transform.position - Self.transform.position;
                direction.y = 0;
                Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = lookRotation;
            }
        }
    }
}