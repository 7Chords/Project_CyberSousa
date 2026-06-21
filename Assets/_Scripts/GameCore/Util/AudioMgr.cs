using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using SCFrame;
using UnityEngine;

namespace GameCore
{
    [Serializable]
    public class AudioInfo
    {
        public string audioName;
        public AudioSource audioSource;

        public AudioInfo(string name, AudioSource source)
        {
            audioName = name;
            audioSource = source;
        }
    }

    public class AudioMgr : Singleton<AudioMgr>
    {
        public AudioInfo bgmAudioInfo;
        public List<AudioInfo> sfxAudioInfoList;

        private Transform _bgmRoot;
        private Transform _sfxRoot;
        private TweenContainer _tweenContainer;
        private CoroutineContainer _coroutineContainer;
        private int _nextSfxCoroutineId;

        public override void OnInitialize()
        {
            sfxAudioInfoList = new List<AudioInfo>();
            _tweenContainer = new TweenContainer();
            _coroutineContainer = new CoroutineContainer();
            EnsureAudioRoots();
        }

        public override void OnDiscard()
        {
            _coroutineContainer?.KillAll();
            _coroutineContainer = null;
            _tweenContainer?.KillAllDoTween();
            _tweenContainer = null;

            StopAllSfx();
            StopBgmImmediate();

            sfxAudioInfoList?.Clear();
            bgmAudioInfo = null;
            _bgmRoot = null;
            _sfxRoot = null;
        }

        #region BGM

        public void PlayBgm(string fadeInMusicName, float fadeInDuration = 0.5f,
            float fadeOutDuration = 0.5f, bool loop = true)
        {
            if (!EnsureAudioRoots())
                return;

            if (string.IsNullOrEmpty(fadeInMusicName))
            {
                Debug.LogError("AudioMgr 播放 BGM 失败：资源名为空。");
                return;
            }

            if (bgmAudioInfo == null || bgmAudioInfo.audioSource == null)
            {
                CreateAndPlayBgm(fadeInMusicName, fadeInDuration, loop);
                return;
            }

            if (bgmAudioInfo.audioName == fadeInMusicName)
            {
                SCDebugHelper.LogWarning($"AudioMgr 当前已在播放 BGM：{fadeInMusicName}");
                return;
            }

            Sequence sequence = DOTween.Sequence();
            if (fadeOutDuration > 0 && bgmAudioInfo.audioSource.isPlaying)
                sequence.Append(bgmAudioInfo.audioSource.DOFade(0f, fadeOutDuration));

            sequence.OnComplete(() =>
            {
                DestroyBgmSource();
                CreateAndPlayBgm(fadeInMusicName, fadeInDuration, loop);
            });

            _tweenContainer.RegDoTween(sequence);
        }

        public void PauseBgm(float fadeOutDuration = 0.5f)
        {
            if (bgmAudioInfo == null || bgmAudioInfo.audioSource == null)
            {
                SCDebugHelper.LogWarning("AudioMgr 暂停 BGM 失败：当前没有正在播放的 BGM。");
                return;
            }

            AudioSource source = bgmAudioInfo.audioSource;
            if (fadeOutDuration > 0)
            {
                Sequence sequence = DOTween.Sequence();
                sequence.Append(source.DOFade(0f, fadeOutDuration));
                sequence.OnComplete(() => source.Pause());
                _tweenContainer.RegDoTween(sequence);
            }
            else
            {
                source.Pause();
            }
        }

        public void ResumeBgm(float fadeInDuration = 0.5f)
        {
            if (bgmAudioInfo == null || bgmAudioInfo.audioSource == null)
            {
                SCDebugHelper.LogWarning("AudioMgr 恢复 BGM 失败：当前没有可恢复的 BGM。");
                return;
            }

            AudioSource source = bgmAudioInfo.audioSource;
            source.UnPause();

            if (fadeInDuration > 0)
            {
                Tween tween = source.DOFade(GetBgmVolume(), fadeInDuration);
                _tweenContainer.RegDoTween(tween);
            }
            else
            {
                source.volume = GetBgmVolume();
            }
        }

        public void StopBgm(float fadeOutDuration = 0.5f)
        {
            if (bgmAudioInfo == null || bgmAudioInfo.audioSource == null)
            {
                SCDebugHelper.LogWarning("AudioMgr 停止 BGM 失败：当前没有正在播放的 BGM。");
                return;
            }

            AudioSource source = bgmAudioInfo.audioSource;
            Sequence sequence = DOTween.Sequence();
            if (fadeOutDuration > 0)
                sequence.Append(source.DOFade(0f, fadeOutDuration));

            sequence.OnComplete(DestroyBgmSource);
            _tweenContainer.RegDoTween(sequence);
        }

        #endregion

        #region SFX

        public void PlaySfx(string sfxName, bool loop = false)
        {
            if (!EnsureAudioRoots())
                return;

            if (string.IsNullOrEmpty(sfxName))
            {
                Debug.LogError("AudioMgr 播放音效失败：资源名为空。");
                return;
            }

            AudioClip clip = ResourcesHelper.LoadAsset<AudioClip>(sfxName);
            if (clip == null)
            {
                SCDebugHelper.LogError($"AudioMgr 音效资源加载失败：{sfxName}");
                return;
            }

            GameObject sfxObject = new GameObject($"{sfxName}_SFX");
            sfxObject.transform.SetParent(_sfxRoot, false);

            AudioSource source = sfxObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = loop;
            source.volume = GetSfxVolume();
            source.Play();

            AudioInfo info = new AudioInfo(sfxName, source);
            sfxAudioInfoList.Add(info);

            if (!loop)
            {
                int coroutineId = ++_nextSfxCoroutineId;
                _coroutineContainer.Run(DetectingSfxPlayState(info), $"AudioMgr_Sfx_{coroutineId}");
            }
        }

        public void PauseSfx(string pauseSfxName)
        {
            AudioInfo audioInfo = FindSfxInfo(pauseSfxName);
            if (audioInfo == null)
            {
                SCDebugHelper.LogWarning($"AudioMgr 暂停音效失败：未找到 sfx={pauseSfxName}");
                return;
            }

            audioInfo.audioSource.Pause();
        }

        public void ResumeSfx(string sfxName)
        {
            AudioInfo audioInfo = FindSfxInfo(sfxName);
            if (audioInfo == null)
            {
                SCDebugHelper.LogWarning($"AudioMgr 恢复音效失败：未找到 sfx={sfxName}");
                return;
            }

            audioInfo.audioSource.UnPause();
        }

        public void StopSfx(string stopSfxName)
        {
            AudioInfo audioInfo = FindSfxInfo(stopSfxName);
            if (audioInfo == null)
            {
                SCDebugHelper.LogWarning($"AudioMgr 停止音效失败：未找到 sfx={stopSfxName}");
                return;
            }

            DestroySfxInfo(audioInfo);
        }

        public void StopAllSfx()
        {
            if (sfxAudioInfoList == null)
                return;

            for (int index = sfxAudioInfoList.Count - 1; index >= 0; index--)
                DestroySfxInfo(sfxAudioInfoList[index]);

            sfxAudioInfoList.Clear();
        }

        #endregion

        #region 音量

        public void RefreshVolume()
        {
            if (bgmAudioInfo != null && bgmAudioInfo.audioSource != null)
                bgmAudioInfo.audioSource.volume = GetBgmVolume();

            if (sfxAudioInfoList == null)
                return;

            for (int index = 0; index < sfxAudioInfoList.Count; index++)
            {
                AudioSource source = sfxAudioInfoList[index]?.audioSource;
                if (source != null)
                    source.volume = GetSfxVolume();
            }
        }

        private static float GetBgmVolume()
        {
            return SCSettingMgr.instance.bgmVolumeFactor;
        }

        private static float GetSfxVolume()
        {
            return SCSettingMgr.instance.sfxVolumeFactor;
        }

        #endregion

        public void ClearAll()
        {
            StopBgm(0.2f);
            StopAllSfx();
        }

        private bool EnsureAudioRoots()
        {
            if (_bgmRoot != null && _sfxRoot != null)
                return true;

            if (SCGameMono.instance == null)
            {
                Debug.LogError("AudioMgr 初始化失败：SCGameMono 未就绪。");
                return false;
            }

            if (SCGameMono.instance.bgmRoot == null)
            {
                Debug.LogError("AudioMgr 初始化失败：SCGameMono.bgmRoot 未配置。");
                return false;
            }

            if (SCGameMono.instance.sfxRoot == null)
            {
                Debug.LogError("AudioMgr 初始化失败：SCGameMono.sfxRoot 未配置。");
                return false;
            }

            _bgmRoot = SCGameMono.instance.bgmRoot.transform;
            _sfxRoot = SCGameMono.instance.sfxRoot.transform;
            return true;
        }

        private void CreateAndPlayBgm(string musicName, float fadeInDuration, bool loop)
        {
            AudioClip clip = ResourcesHelper.LoadAsset<AudioClip>(musicName);
            if (clip == null)
            {
                SCDebugHelper.LogError($"AudioMgr BGM 资源加载失败：{musicName}");
                return;
            }

            GameObject bgmObject = new GameObject($"{musicName}_BGM");
            bgmObject.transform.SetParent(_bgmRoot, false);

            AudioSource source = bgmObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = loop;
            source.volume = fadeInDuration > 0 ? 0f : GetBgmVolume();
            source.Play();

            bgmAudioInfo = new AudioInfo(musicName, source);

            if (fadeInDuration > 0)
            {
                Tween tween = source.DOFade(GetBgmVolume(), fadeInDuration);
                _tweenContainer.RegDoTween(tween);
            }
        }

        private void DestroyBgmSource()
        {
            if (bgmAudioInfo == null || bgmAudioInfo.audioSource == null)
            {
                bgmAudioInfo = null;
                return;
            }

            bgmAudioInfo.audioSource.Stop();
            SCCommon.DestoryGameObject(bgmAudioInfo.audioSource.gameObject);
            bgmAudioInfo = null;
        }

        private void StopBgmImmediate()
        {
            if (bgmAudioInfo == null || bgmAudioInfo.audioSource == null)
            {
                bgmAudioInfo = null;
                return;
            }

            bgmAudioInfo.audioSource.Stop();
            SCCommon.DestoryGameObject(bgmAudioInfo.audioSource.gameObject);
            bgmAudioInfo = null;
        }

        private AudioInfo FindSfxInfo(string sfxName)
        {
            if (sfxAudioInfoList == null || string.IsNullOrEmpty(sfxName))
                return null;

            return sfxAudioInfoList.Find(info => info != null && info.audioName == sfxName);
        }

        private void DestroySfxInfo(AudioInfo audioInfo)
        {
            if (audioInfo == null)
                return;

            if (audioInfo.audioSource != null)
            {
                audioInfo.audioSource.Stop();
                SCCommon.DestoryGameObject(audioInfo.audioSource.gameObject);
            }

            sfxAudioInfoList.Remove(audioInfo);
        }

        private IEnumerator DetectingSfxPlayState(AudioInfo info)
        {
            if (info == null || info.audioSource == null)
                yield break;

            AudioSource audioSource = info.audioSource;
            yield return new WaitWhile(() => audioSource != null && audioSource.isPlaying);

            if (sfxAudioInfoList != null && sfxAudioInfoList.Contains(info))
                sfxAudioInfoList.Remove(info);

            if (audioSource != null && audioSource.gameObject != null)
                SCCommon.DestoryGameObject(audioSource.gameObject);
        }
    }
}
