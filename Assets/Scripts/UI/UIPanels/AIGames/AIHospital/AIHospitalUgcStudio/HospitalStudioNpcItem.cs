using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using UnityEngine;

public class HospitalStudioNpcItem : MonoBehaviour
{
        public RemoteImageBehaviour Rm_NpcIcon;
        public GameObject Go_NpcIcon;
        public GameObject Go_LearnMore;
        public void SetProfile(string iconUrl) {
            Rm_NpcIcon.Load(iconUrl);
            Go_NpcIcon.SetActive(true);
            Go_LearnMore.SetActive(false);
        }

        public void SetLearnMore(){
            Go_NpcIcon.SetActive(false);
            Go_LearnMore.SetActive(true);
        }
}
