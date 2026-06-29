// @Author: YangJie
// @Description:
// @Date:  2023/09/19
// @Modify:

using Game.Config;
using Sirenix.OdinInspector;

namespace Game.OfflineRender
{
    public class UGCMatData
    {
        public string nodeName;
        public int matId;
        public string uMatId;
        public string color;
        public string tile;
        public string mainTex;
        public string norTex;

        public TextureCombineData GetTextureCombineData() {
            return new TextureCombineData() {
                mainTex = mainTex,
                norTex = norTex,
            };
        }

        public bool IsCombineMat() {
            return !string.IsNullOrEmpty(mainTex);
        }

    }


    [System.Serializable]
    public class TextureCombineData
    {
        [ReadOnly]
        public string mainTex;

        [ReadOnly]
        public string norTex;
    }



}
