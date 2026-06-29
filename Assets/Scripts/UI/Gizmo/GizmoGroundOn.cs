using UnityEngine;

namespace Game {
    public class GizmoGroundOn : MonoBehaviour {
        private float limitY;

        private bool isRefreshOffset = false;

        public void RefreshOffset() {
            isRefreshOffset = true;
            float minY = float.MaxValue;
            var renderList = transform.GetComponentsInChildren<Renderer>();
            foreach (var tmpRender in renderList) {
                if (tmpRender.bounds.min.y < minY) {
                    minY = tmpRender.bounds.min.y;
                }
            }
            var offset = transform.transform.position.y - minY;
            limitY = offset;
        }

        public void SetRefreshFlag(bool value)
        {
            isRefreshOffset = value;
        }

        public float GetLimitY() {
            if (!isRefreshOffset) {
                RefreshOffset();
            }
            return limitY;
        }

    }
}
