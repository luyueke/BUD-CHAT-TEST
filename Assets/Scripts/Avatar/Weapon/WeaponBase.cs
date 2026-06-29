using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Game.Weapon
{
    public enum WeaponType
    {
        Hammer,     // 锤子
        Bow,        // 弓箭
        Gun         // 枪
    }

    public abstract class WeaponBase : MonoBehaviour
    {
        [Header("动画设置")]
        public string idleAnim;      // 待机动画名
        public string[] attackAnims; // 攻击动画列表

        public abstract void Attact();
    }
}