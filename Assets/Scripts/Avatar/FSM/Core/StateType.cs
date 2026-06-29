using System.Collections.Generic;
using UnityEngine;

namespace FSM
{
    [CreateAssetMenu(fileName = "StateName", menuName = "CreateNewPlayerState", order = 1)]
    public class StateType : ScriptableObject
    {
        [Header("人物状态")]
        public PlayerState playerState;

        [Header("人物默认状态")]
        [Space(40)]
        public PlayerDefault playerDefaultType;
        [Header("人物功能属性")]
        public PlayerFeature playerFeature;
        [Header("环境")]
        public PlayerEnvironment environment;
        [Header("触发属性")]
        public PlayerTrigger triggerAttributes;
        [Header("不被中断或缓存")]
        public bool isNotInterrupt;

        /// <summary>
        /// 获取状态分类
        /// </summary>
        /// <returns></returns>
        public List<object> GetStateType()
        {
            List<object> typeList = new List<object>();

            if (playerDefaultType != PlayerDefault.None)
            {
                typeList.Add(playerDefaultType);
            }

            if (playerFeature != PlayerFeature.None)
            {
                typeList.Add(playerFeature);
            }

            if (environment != PlayerEnvironment.None)
            {
                typeList.Add(environment);
            }

            if (triggerAttributes != PlayerTrigger.None)
            {
                typeList.Add(triggerAttributes);
            }

            return typeList;
        }
    }
}