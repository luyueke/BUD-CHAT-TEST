using System;
using Game.Avatar;
using UnityEngine;

namespace UI.Preview3D.Mono
{
    public class ThemeSkinPreviewMono : MonoBehaviour
    {
        private CharacterWrap _characterWrap;

        public TextAsset characterJsonFile;
        
        private void Awake()
        {
            var curGo = this.gameObject;
            var playerAnimationCtrl = curGo.AddComponent<PlayerAnimationCtrl>();
            _characterWrap = new CharacterWrap(curGo);
            playerAnimationCtrl.Init(_characterWrap);
            
            // Read avatar data from json file.
            if (characterJsonFile != null)
            {
                CharacterData cData = CharacterData.DeserializeObject(characterJsonFile.text);
                SetCharacterData(cData);
            }
        }

        public void SetCharacterData(CharacterData data)
        {
            _characterWrap.SetCharacterData(data);
        }
    }
}