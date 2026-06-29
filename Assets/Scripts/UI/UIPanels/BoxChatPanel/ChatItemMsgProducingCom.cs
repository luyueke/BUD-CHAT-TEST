using UnityEngine;

namespace Game
{
    /// <summary>
    /// 消息生成中动画：3个圆点 Image 循环切换显示 1/2/3 个亮点，模拟"正在输入"效果
    /// </summary>
    public class ChatItemMsgProducingCom : MonoBehaviour
    {
        public GameObject[] gameObjects; // 长度为 3，依次对应 1/2/3 个点的状态

        private const float Interval = 0.5f;

        private int _curIndex;
        private float _timer;

        private void OnEnable()
        {
            _curIndex = 0;
            _timer = 0f;
            RefreshDots();
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < Interval) return;
            _timer -= Interval;
            _curIndex = (_curIndex + 1) % gameObjects.Length;
            RefreshDots();
        }

        private void RefreshDots()
        {
            for (int i = 0; i < gameObjects.Length; i++)
                gameObjects[i].SetActive(i <= _curIndex);
        }
    }
}