using System.Collections;
using GameCore.Flow;
using SCFrame;
using SCFrame.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameCore.UI
{
    public class UIPanelStart : _ASCUIPanelBase<UIMonoStart>
    {
        private const float WhiteFlashHoldDuration = 0.05f;
        private const float WhiteFlashFadeDuration = 0.12f;

        private static Sprite _s_whiteFlashSprite;

        private Coroutine _waitForAnyInputCoroutine;
        private Coroutine _revealTransitionCoroutine;
        private Image _whiteFlashOverlay;
        private bool _isRevealTransitionPlaying;

        public UIPanelStart(UIMonoStart _mono, SCUIShowType _showType) : base(_mono, _showType)
        {
        }

        public override void AfterInitialize()
        {
            ensureButtonInteractEffects();
        }

        public override void BeforeDiscard()
        {
            stopInputWaiting();
            stopRevealTransition();
        }

        public override void OnHidePanel()
        {
            removeFirstInputListener();
            stopInputWaiting();
            stopRevealTransition();
            setWhiteFlashOverlayVisible(false, 0f);

            if (mono.btnStart != null)
                mono.btnStart.RemoveMouseLeftClickDown(onBtnStartClicked);
            if (mono.btnContinue != null)
                mono.btnContinue.RemoveMouseLeftClickDown(onBtnContinueClicked);
            if (mono.btnSetting != null)
                mono.btnSetting.RemoveMouseLeftClickDown(onBtnSettingClicked);
            if (mono.btnExit != null)
                mono.btnExit.RemoveMouseLeftClickDown(onBtnExitClicked);
        }

        public override void OnShowPanel()
        {
            stopRevealTransition();
            resetInitialVisualState();
            addFirstInputListener();
            startInputWaiting();

            if (mono.btnStart != null)
                mono.btnStart.AddMouseLeftClickDown(onBtnStartClicked);
            if (mono.btnContinue != null)
                mono.btnContinue.AddMouseLeftClickDown(onBtnContinueClicked);
            if (mono.btnSetting != null)
                mono.btnSetting.AddMouseLeftClickDown(onBtnSettingClicked);
            if (mono.btnExit != null)
                mono.btnExit.AddMouseLeftClickDown(onBtnExitClicked);

            RefreshContinueButton();
        }

        private void onPanelClicked(PointerEventData _data, object[] _objs)
        {
            tryBeginRevealTransition();
        }

        private void onBtnStartClicked(PointerEventData _data, object[] _objs)
        {
            GamePlayerDataMgr.instance.BeginNewGame();
            GameFlowController.instance.EnterGameplay();
        }

        private void onBtnContinueClicked(PointerEventData _data, object[] _objs)
        {
            if (!GamePlayerDataMgr.instance.TryLoadSaveData())
            {
                SCDebugHelper.Log("没有可继续的存档。");
                RefreshContinueButton();
                return;
            }

            GameFlowController.instance.EnterGameplay();
        }

        private void onBtnExitClicked(PointerEventData arg1, object[] arg2)
        {
            SCDebugHelper.Log("退出游戏");
        }

        private void onBtnSettingClicked(PointerEventData arg1, object[] arg2)
        {
            UINodeMgr.instance.AddNode(new UINodeSetting(SCUIShowType.ADDITION, false));
        }

        private void RefreshContinueButton()
        {
            if (mono.btnContinue == null)
                return;

            SCCommon.SetGameObjectEnable(mono.btnContinue.gameObject, GamePlayerDataMgr.instance.hasSaveData);
        }

        private void SetButtonBoxVisible(bool _visible)
        {
            if (mono.btnBox == null)
            {
                SCDebugHelper.LogError("[UIPanelStart] btnBox 未绑定，无法切换开始页按钮显示状态。");
                return;
            }

            SCCommon.SetGameObjectEnable(mono.btnBox, _visible);
        }

        private void ensureButtonInteractEffects()
        {
            ensureButtonInteractEffect(mono.btnStart, nameof(mono.btnStart));
            ensureButtonInteractEffect(mono.btnContinue, nameof(mono.btnContinue));
            ensureButtonInteractEffect(mono.btnSetting, nameof(mono.btnSetting));
            ensureButtonInteractEffect(mono.btnExit, nameof(mono.btnExit));
        }

        private void ensureButtonInteractEffect(Button _button, string _buttonName)
        {
            if (_button == null)
            {
                SCDebugHelper.LogError($"[UIPanelStart] {_buttonName} 未绑定，无法添加按钮缩放交互效果。");
                return;
            }

            if (_button.GetComponent<UIScaleInteractEffect>() != null)
                return;

            _button.gameObject.AddComponent<UIScaleInteractEffect>();
        }

        private void setBackgroundVisible(GameObject _target, bool _visible, string _targetName)
        {
            if (_target == null)
            {
                SCDebugHelper.LogError($"[UIPanelStart] {_targetName} 未绑定，无法切换显示状态。");
                return;
            }

            SCCommon.SetGameObjectEnable(_target, _visible);
        }

        private void resetInitialVisualState()
        {
            _isRevealTransitionPlaying = false;
            setBackgroundVisible(mono.bg1, true, nameof(mono.bg1));
            setBackgroundVisible(mono.bg2, false, nameof(mono.bg2));
            SetButtonBoxVisible(false);
            setWhiteFlashOverlayVisible(false, 0f);
        }

        private void addFirstInputListener()
        {
            if (mono.bg1 == null)
            {
                SCDebugHelper.LogError("[UIPanelStart] bg1 未绑定，无法响应首次点击。");
                return;
            }

            mono.bg1.transform.AddMouseLeftClickDown(onPanelClicked);
        }

        private void removeFirstInputListener()
        {
            if (mono.bg1 == null)
                return;

            mono.bg1.transform.RemoveMouseLeftClickDown(onPanelClicked);
        }

        private void startInputWaiting()
        {
            stopInputWaiting();
            _waitForAnyInputCoroutine = mono.StartCoroutine(waitForAnyInputRoutine());
        }

        private void stopInputWaiting()
        {
            if (_waitForAnyInputCoroutine == null)
                return;

            mono.StopCoroutine(_waitForAnyInputCoroutine);
            _waitForAnyInputCoroutine = null;
        }

        private IEnumerator waitForAnyInputRoutine()
        {
            while (!_isRevealTransitionPlaying)
            {
                if (Input.anyKeyDown)
                {
                    tryBeginRevealTransition();
                    yield break;
                }

                yield return null;
            }
        }

        private void tryBeginRevealTransition()
        {
            if (_isRevealTransitionPlaying)
                return;

            if (mono.btnBox == null)
            {
                SCDebugHelper.LogError("[UIPanelStart] btnBox 未绑定，无法显示开始页按钮。");
                removeFirstInputListener();
                stopInputWaiting();
                return;
            }

            _isRevealTransitionPlaying = true;
            removeFirstInputListener();
            stopInputWaiting();
            stopRevealTransition();
            _revealTransitionCoroutine = mono.StartCoroutine(playRevealTransitionRoutine());
        }

        private void stopRevealTransition()
        {
            if (_revealTransitionCoroutine == null)
                return;

            mono.StopCoroutine(_revealTransitionCoroutine);
            _revealTransitionCoroutine = null;
        }

        private IEnumerator playRevealTransitionRoutine()
        {
            setBackgroundVisible(mono.bg1, false, nameof(mono.bg1));
            setWhiteFlashOverlayVisible(true, 1f);

            yield return new WaitForSecondsRealtime(WhiteFlashHoldDuration);

            setBackgroundVisible(mono.bg2, true, nameof(mono.bg2));
            SetButtonBoxVisible(true);

            float elapsed = 0f;
            while (elapsed < WhiteFlashFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / WhiteFlashFadeDuration);
                setWhiteFlashOverlayVisible(true, alpha);
                yield return null;
            }

            setWhiteFlashOverlayVisible(false, 0f);
            _revealTransitionCoroutine = null;
        }

        private void setWhiteFlashOverlayVisible(bool _visible, float _alpha)
        {
            Image overlay = getOrCreateWhiteFlashOverlay();
            if (overlay == null)
                return;

            overlay.gameObject.SetActive(_visible);
            if (!_visible)
                return;

            Color color = overlay.color;
            color.a = _alpha;
            overlay.color = color;
        }

        private Image getOrCreateWhiteFlashOverlay()
        {
            if (_whiteFlashOverlay != null)
                return _whiteFlashOverlay;

            GameObject overlayObject = new GameObject("white_flash_overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlayObject.transform.SetParent(mono.transform, false);
            overlayObject.transform.SetAsLastSibling();

            RectTransform rectTransform = overlayObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            _whiteFlashOverlay = overlayObject.GetComponent<Image>();
            _whiteFlashOverlay.sprite = getWhiteFlashSprite();
            _whiteFlashOverlay.color = new Color(1f, 1f, 1f, 0f);
            _whiteFlashOverlay.raycastTarget = false;
            overlayObject.SetActive(false);
            return _whiteFlashOverlay;
        }

        private static Sprite getWhiteFlashSprite()
        {
            if (_s_whiteFlashSprite != null)
                return _s_whiteFlashSprite;

            _s_whiteFlashSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f));
            return _s_whiteFlashSprite;
        }
    }
}
