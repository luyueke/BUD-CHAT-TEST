using System;
using System.Collections;
using System.Collections.Generic;
using GameData.BaseInfo;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.MusicalInstrument
{
    public class ToneAudioUtils : MonoBehaviour
    {
        private AudioSource _audioSource;
        private Coroutine _downloadCoroutine;
        private List<string> _downloadAudioUrl = new List<string>();
        private List<AudioClip> _downloadAudioClip = new List<AudioClip>();

        private void Awake()
        {
            _audioSource = this.gameObject.AddComponent<AudioSource>();
        }

        public void PreviewTone(ToneInfo toneInfo)
        {
            if(toneInfo == null)
                return;
            
            _downloadAudioUrl.Clear();
            _downloadAudioClip.Clear();
            switch ((ToneType)toneInfo.toneType)
            {
                case ToneType.Fifteen:
                    foreach (var kvp in toneInfo.toneDict)
                    {
                        if (kvp.Key == (int)SyllableType.High_1
                            || kvp.Key == (int)SyllableType.High_2
                            || kvp.Key == (int)SyllableType.High_3
                            || kvp.Key == (int)SyllableType.High_4
                            || kvp.Key == (int)SyllableType.High_5
                            || kvp.Key == (int)SyllableType.High_6
                            || kvp.Key == (int)SyllableType.High_7
                            || kvp.Key == (int)SyllableType.Double_High)
                        {
                            _downloadAudioUrl.Add(kvp.Value?.url);
                        }
                    }
                    break;
                
                case ToneType.TwentyTwo:
                    foreach (var kvp in toneInfo.toneDict)
                    {
                        if (kvp.Key == (int)SyllableType.Middle_1
                            || kvp.Key == (int)SyllableType.Middle_2
                            || kvp.Key == (int)SyllableType.Middle_3
                            || kvp.Key == (int)SyllableType.Middle_4
                            || kvp.Key == (int)SyllableType.Middle_5
                            || kvp.Key == (int)SyllableType.Middle_6
                            || kvp.Key == (int)SyllableType.Middle_7)
                        {
                            _downloadAudioUrl.Add(kvp.Value?.url);
                        }
                    }
                    break;
            }
            
            // 停止当前的下载协程（如果有）
            if (_downloadCoroutine != null)
            {
                StopCoroutine(_downloadCoroutine);
                LoggerUtils.Log("Current download stopped.");
            }
            
            // 开始新的下载协程
            _downloadCoroutine = StartCoroutine(DownloadAudioFiles(_downloadAudioUrl));
        }
        
        IEnumerator DownloadAudioFiles(List<string> audioUrls)
        {
            foreach (string url in audioUrls)
            {
                yield return StartCoroutine(DownloadAudio(url));
            }

            LoggerUtils.Log("All downloads completed.");
            StartCoroutine(PlayAudioClips());
        }

        IEnumerator DownloadAudio(string url)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
            {
                www.timeout = 5; // 设置5秒的超时时长
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    LoggerUtils.LogError($"Error downloading audio from {url}: {www.error}");
                }
                else
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    _downloadAudioClip.Add(clip);
                    LoggerUtils.Log($"Successfully downloaded audio from {url}");
                }
            }
        }
        
        IEnumerator PlayAudioClips()
        {
            foreach (AudioClip clip in _downloadAudioClip)
            {
                _audioSource.clip = clip;
                _audioSource.Play();

                // 播放2秒
                yield return new WaitForSeconds(2);

                // 停止播放
                _audioSource.Stop();
            }

            LoggerUtils.Log("All audio clips played.");
        }

        private void OnDisable()
        {
            if(_audioSource != null)
                _audioSource.Stop();
        }
    }
}