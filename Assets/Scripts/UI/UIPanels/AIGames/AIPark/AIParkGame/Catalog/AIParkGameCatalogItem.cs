using Com.TheFallenGames.OSA.Util.IO;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIParkGameCatalogItem : MonoBehaviour
    {
        public Toggle Tog;
        public Image Head;
        public RemoteImageBehaviour RemoteImageBehaviour;

        [HideInInspector] public AIParkGameCatalogPanel Root;

        private string PlayerID;
        private void Awake()
        {
            Tog.onValueChanged.AddListener(OnTog);
        }

        public void SetData(AIParkGameCatalogPanel root, string playerID) {
            PlayerID = playerID;
            Root = root;

            RemoteImageBehaviour.gameObject.SetActive(false);
            Head.gameObject.SetActive(false);

            var cover = AIPark_NpcUtil.GetCata(playerID, Head);
            if (!string.IsNullOrEmpty(cover))
            {
                RemoteImageBehaviour.Load(cover);
                RemoteImageBehaviour.gameObject.SetActive(true);
            }

        }


        private void OnTog(bool bo)
        {
            if (bo)
            {
                Root.ShowInfo(PlayerID);
            }
        }

    }
}