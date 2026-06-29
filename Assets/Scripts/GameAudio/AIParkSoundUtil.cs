using System;
using System.Collections;
using System.Collections.Generic;
using Game.Audio;
using UnityEngine;

namespace AIGame.Base
{
    public enum AIParkSoundType
    {
        Cricket, //蛐蛐点声
        Fountain, //喷泉声
        Park_2D, //游乐园2D环境声
        Fireworks, //烟花
    }
    public class AIParkSoundUtil : GlobalInstance<AIParkSoundUtil>
    {
        public Dictionary<AIParkSoundType, Vector3> _soundPosMap;
        public Dictionary<AIParkSoundType, GameObject> _soundObjMap;
        public Dictionary<AIParkSoundType, string> _soundNameMap;
        private const string TAG = "AIParkSoundUtil";
        private GameObject _bgmNode;
        public GameObject BgmNode
        {
            get
            {
                if (_bgmNode == null)
                {
                    _bgmNode = new GameObject("AIGameBgmNode");
                    GameObject.DontDestroyOnLoad(_bgmNode);
                }
                return _bgmNode;
            }
        }

        private GameObject _soundEffectNode;
        public GameObject SoundEffectNode
        {
            get
            {
                if (_soundEffectNode == null)
                {
                    _soundEffectNode = new GameObject("AIGameSoundtNode");
                    GameObject.DontDestroyOnLoad(_soundEffectNode);
                }
                return _soundEffectNode;
            }
        }

        public AIParkSoundUtil()
        {
            InitParkPos();
            InitSoundObj();
            InitSoundName();
            PlayEvironmentSound();
        }
        public void InitParkPos()
        {
            _soundPosMap ??= new();
            _soundPosMap.Clear();
            _soundPosMap.Add(AIParkSoundType.Cricket, new Vector3(41.45f, 1.66f, 5.34f));
            _soundPosMap.Add(AIParkSoundType.Fountain, new Vector3(5.05f, 0, 3.15f));
            _soundPosMap.Add(AIParkSoundType.Park_2D, new Vector3(0, 0, 0));
            _soundPosMap.Add(AIParkSoundType.Fireworks, new Vector3(0, 30, 0));
        }

        public void InitSoundObj()
        {
            _soundObjMap ??= new();
            _soundObjMap.Clear();
            var cricketObj = new GameObject("Cricket");
            cricketObj.transform.position = _soundPosMap[AIParkSoundType.Cricket];
            _soundObjMap.Add(AIParkSoundType.Cricket, cricketObj);
            var fountainObj = new GameObject("Fountain");
            fountainObj.transform.position = _soundPosMap[AIParkSoundType.Fountain];
            _soundObjMap.Add(AIParkSoundType.Fountain, fountainObj);
            var park2DObj = new GameObject("Park_2D");
            park2DObj.transform.position = _soundPosMap[AIParkSoundType.Park_2D];
            _soundObjMap.Add(AIParkSoundType.Park_2D, park2DObj);
            var fireObj = new GameObject("Fireworks");
            fireObj.transform.position = _soundPosMap[AIParkSoundType.Fireworks];
            _soundObjMap.Add(AIParkSoundType.Fireworks, fireObj);
        }
        public void InitSoundName()
        {
            _soundNameMap ??= new();
            _soundNameMap.Clear();
            _soundNameMap.Add(AIParkSoundType.Cricket, "Para_Cricket_Loop");//蟋蟀点声源
            _soundNameMap.Add(AIParkSoundType.Fountain, "Para_Fountain_Loop");//喷泉点声源
            _soundNameMap.Add(AIParkSoundType.Park_2D, "Environment_Paradise");//游乐园2D环境声
            // _soundNameMap.Add(AIParkSoundType.Fireworks, "Para_Fireworks");//游乐园烟花
        }

        public void PlayEvironmentSound()
        {
            foreach (var item in _soundNameMap)
            {
                AIGameSoundUtils.Inst.PlaySound(item.Value, _soundObjMap[item.Key]);
            }
        }

        public void StopEvironmentSound()
        {
            foreach (var item in _soundNameMap)
            {
                AIGameSoundUtils.Inst.StopSound(item.Value, _soundObjMap[item.Key]);
            }
        }

        public void ReleaseAllSound()
        {
            foreach (var item in _soundNameMap)
            {
                AIGameSoundUtils.Inst.StopSound(item.Value, _soundObjMap[item.Key]);
            }
            foreach (var item in _soundObjMap)
            {
                if (item.Value != null)
                {
                    GameObject.Destroy(item.Value);
                }
            }
            _soundObjMap.Clear();
            _soundNameMap.Clear();
            _soundPosMap.Clear();
        }

    }
}
