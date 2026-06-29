using UnityEngine;
using Sirenix.OdinInspector;

namespace Game.Weapon
{
    [CreateAssetMenu(fileName = "NewWeaponConfig", menuName = "Weapons/WeaponConfig")]
    public class WeaponConfigSO : ScriptableObject
    {
        [Header("基础信息")]
        public string weaponName;
        public WeaponType weaponType;
        
        [Header("基础动画")]
        [Space(20)]
        public string idleAnim;         // 待机动画
        public string attackAnim;       // 攻击动画
        public float attackInterval;    // 攻击间隔

        [Header("武器属性")]
        [Space(20)]
        [SerializeField] private bool isRangeWeapon;
        
        [Header("远程武器参数")]
        [ShowIf("isRangeWeapon")]
        public float projectileSpeed;
        public float shootRange;

        public bool IsRangeWeapon()
        {
            return isRangeWeapon;
        }

        // 可以添加一些辅助方法
        public void InitializeAsHammer()
        {
            weaponType = WeaponType.Hammer;
            isRangeWeapon = false;
        }

        public void InitializeAsBow()
        {
            weaponType = WeaponType.Bow;
            isRangeWeapon = true;
        }

        public void InitializeAsGun()
        {
            weaponType = WeaponType.Gun;
            isRangeWeapon = true;
        }
    }
}