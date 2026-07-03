using DG.Tweening;
using GameCore;
using GameCore.Flow;
using SCFrame;
using SCFrame.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameCore.UI
{
    public class UIPanelSetting : _ASCUIPanelBase<UIMonoSetting>
    {
        private TweenContainer _m_tweenContainer;

        public UIPanelSetting(UIMonoSetting _mono, SCUIShowType _showType) : base(_mono, _showType)
        {
        }

        public override void AfterInitialize()
        {
            _m_tweenContainer = new TweenContainer();
        }

        public override void BeforeDiscard()
        {
            _m_tweenContainer?.KillAllDoTween();
            _m_tweenContainer = null;
        }

        public override void OnHidePanel()
        {
            if (mono.sldMusic != null)
                mono.sldMusic.onValueChanged.RemoveAllListeners();
            if (mono.sldSound != null)
                mono.sldSound.onValueChanged.RemoveAllListeners();
            if (mono.btnClose != null)
                mono.btnClose.RemoveMouseLeftClickDown(onBtnCloseClickDown);
            if (mono.btnReturnMain != null)
            {
                mono.btnReturnMain.RemoveMouseLeftClickDown(onBtnReturnMainClickDown);
                mono.btnReturnMain.RemoveMouseEnter(onBtnReturnMainMouseEnter);
                mono.btnReturnMain.RemoveMouseExit(onBtnReturnMainMouseExit);
            }
        }

        public override void OnShowPanel()
        {
            if (mono.sldMusic != null)
            {
                mono.sldMusic.onValueChanged.AddListener(onMusicVolumeChanged);
                mono.sldMusic.value = SCSettingMgr.instance.bgmVolumeFactor;
            }

            if (mono.sldSound != null)
            {
                mono.sldSound.onValueChanged.AddListener(onSoundVolumeChanged);
                mono.sldSound.value = SCSettingMgr.instance.sfxVolumeFactor;
            }

            if (mono.btnClose != null)
                mono.btnClose.AddMouseLeftClickDown(onBtnCloseClickDown);

            if (mono.btnReturnMain != null)
            {
                mono.btnReturnMain.AddMouseLeftClickDown(onBtnReturnMainClickDown);
                mono.btnReturnMain.AddMouseEnter(onBtnReturnMainMouseEnter);
                mono.btnReturnMain.AddMouseExit(onBtnReturnMainMouseExit);
            }
        }

        public void SetReturnBtnShowState(bool _needShow)
        {
            if (mono.btnReturnMain != null)
                SCCommon.SetGameObjectEnable(mono.btnReturnMain.gameObject, _needShow);
        }

        private void onMusicVolumeChanged(float value)
        {
            SCSettingMgr.instance.bgmVolumeFactor = value;
            AudioMgr.instance.RefreshVolume();
        }

        private void onSoundVolumeChanged(float value)
        {
            SCSettingMgr.instance.sfxVolumeFactor = value;
            AudioMgr.instance.RefreshVolume();
        }

        private void onBtnReturnMainMouseEnter(PointerEventData _data, object[] _objs)
        {
            if (mono.btnReturnMain == null)
                return;

            _m_tweenContainer?.RegDoTween(mono.btnReturnMain.transform.DOScale(mono.btnEnterScale, mono.btnScaleChgTime));
        }

        private void onBtnReturnMainMouseExit(PointerEventData _data, object[] _objs)
        {
            if (mono.btnReturnMain == null)
                return;

            _m_tweenContainer?.RegDoTween(mono.btnReturnMain.transform.DOScale(Vector3.one, mono.btnScaleChgTime));
        }

        private void onBtnReturnMainClickDown(PointerEventData _data, object[] _objs)
        {
            AudioMgr.instance.PlaySfx(AudioKeys.ButtonClick);
            UIPanelGameplayMain.SaveCurrentDayProgressIfActive();
            UINodeMgr.instance.CloseTopNode();
            GameFlowController.instance.EnterMainMenu();
        }

        private void onBtnCloseClickDown(PointerEventData _data, object[] _objs)
        {
            AudioMgr.instance.PlaySfx(AudioKeys.ButtonClick);
            UINodeMgr.instance.CloseTopNode();
        }
    }
}
