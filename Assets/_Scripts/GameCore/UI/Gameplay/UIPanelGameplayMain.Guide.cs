using SCFrame;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
{
    public partial class UIPanelGameplayMain
    {
        private const long FirstGuideLevelId = 1001;
        private const long FirstGuideCustomerId = 1002;
        private const long FirstGuideNeedId = 2001;
        private const int FirstGuideFloor = 7;
        private const string GuideMaskResName = "panel_gameplay_guide_mask";

        private UIMonoGameplayGuideMask _guideMask;
        private GameplayGuideStep _guideStep = GameplayGuideStep.None;

        private enum GameplayGuideStep
        {
            None,
            CustomerInfo,
            FloorButton,
            Confirm,
            CloseDoor,
            Finished
        }

        private void TryStartFirstCustomerGuide()
        {
            if (_hasPlayedFirstCustomerGuide || _guideStep != GameplayGuideStep.None)
                return;

            if (_currentLevelRefData == null
                || _currentLevelRefData.id != FirstGuideLevelId
                || _currentCustomerRefData == null
                || _currentCustomerRefData.id != FirstGuideCustomerId
                || _currentNeedRefData == null
                || _currentNeedRefData.id != FirstGuideNeedId)
            {
                return;
            }

            _hasPlayedFirstCustomerGuide = true;
            ShowGuideStep(GameplayGuideStep.CustomerInfo);
        }

        private void ShowGuideStep(GameplayGuideStep guideStep)
        {
            _guideStep = guideStep;
            EnsureGuideMask();
            if (_guideMask == null)
                return;

            _guideMask.SetVisible(true);
            _guideMask.transform.SetAsLastSibling();

            switch (guideStep)
            {
                case GameplayGuideStep.CustomerInfo:
                    SetGuideTarget(GetAnimalInfoPanelRect(), "先看客人的资料。\n考考说登记楼层是七楼，可以在这里确认。", true);
                    break;
                case GameplayGuideStep.FloorButton:
                    SetGuideTarget(GetFloorButtonRect(FirstGuideFloor), "资料确认后，按下 7 楼。", false);
                    break;
                case GameplayGuideStep.Confirm:
                    SetGuideTarget(mono.btnConfirm?.transform as RectTransform, "楼层选好后，点击确认。", false);
                    break;
                case GameplayGuideStep.CloseDoor:
                    SetGuideTarget(mono.btnCloseDoor?.transform as RectTransform, "最后关门，送考考回七楼。", false);
                    break;
                default:
                    FinishGuide();
                    break;
            }
        }

        private void AdvanceGuideAfterCustomerInfoViewed()
        {
            if (_guideStep == GameplayGuideStep.CustomerInfo)
                ShowGuideStep(GameplayGuideStep.FloorButton);
        }

        private void AdvanceGuideAfterFloorSelected(int floor)
        {
            if (_guideStep == GameplayGuideStep.FloorButton && floor == FirstGuideFloor)
                ShowGuideStep(GameplayGuideStep.Confirm);
        }

        private void AdvanceGuideAfterConfirm()
        {
            if (_guideStep == GameplayGuideStep.Confirm)
                ShowGuideStep(GameplayGuideStep.CloseDoor);
        }

        private void AdvanceGuideAfterCloseDoor()
        {
            if (_guideStep == GameplayGuideStep.CloseDoor)
                FinishGuide();
        }

        private bool IsBlockedByGuide(RectTransform target)
        {
            return _guideStep != GameplayGuideStep.None
                && _guideStep != GameplayGuideStep.Finished
                && target != GetCurrentGuideTarget();
        }

        private RectTransform GetCurrentGuideTarget()
        {
            switch (_guideStep)
            {
                case GameplayGuideStep.CustomerInfo:
                    return GetAnimalInfoPanelRect();
                case GameplayGuideStep.FloorButton:
                    return GetFloorButtonRect(FirstGuideFloor);
                case GameplayGuideStep.Confirm:
                    return mono.btnConfirm?.transform as RectTransform;
                case GameplayGuideStep.CloseDoor:
                    return mono.btnCloseDoor?.transform as RectTransform;
                default:
                    return null;
            }
        }

        private void FinishGuide()
        {
            _guideStep = GameplayGuideStep.Finished;
            if (_guideMask != null)
                _guideMask.SetVisible(false);
        }

        private void EnsureGuideMask()
        {
            if (_guideMask != null)
                return;

            _guideMask = ResourcesHelper.Load<UIMonoGameplayGuideMask>(GuideMaskResName, mono.transform);
            if (_guideMask == null)
            {
                Debug.LogError($"Gameplay 新手引导遮罩加载失败：resName={GuideMaskResName}");
                return;
            }

            RectTransform guideRect = _guideMask.transform as RectTransform;
            if (guideRect != null)
            {
                guideRect.anchorMin = Vector2.zero;
                guideRect.anchorMax = Vector2.one;
                guideRect.offsetMin = Vector2.zero;
                guideRect.offsetMax = Vector2.zero;
            }

            if (_guideMask.continueButton != null)
                _guideMask.continueButton.onClick.AddListener(AdvanceGuideAfterCustomerInfoViewed);
        }

        private void SetGuideTarget(RectTransform target, string tipText, bool enableContinueClick)
        {
            if (target == null)
            {
                Debug.LogError($"Gameplay 新手引导目标为空：step={_guideStep}");
                FinishGuide();
                return;
            }

            _guideMask.SetTarget(target, tipText, enableContinueClick);
        }

        private RectTransform GetAnimalInfoPanelRect()
        {
            RectTransform textRect = mono.txtAnimalInfo?.rectTransform;
            if (textRect == null)
                return null;

            RectTransform panelRect = textRect.parent as RectTransform;
            return panelRect != null ? panelRect : textRect;
        }

        private RectTransform GetFloorButtonRect(int floor)
        {
            if (mono.numberButtons == null)
                return null;

            for (int index = 0; index < mono.numberButtons.Count; index++)
            {
                Button button = mono.numberButtons[index];
                if (button != null && GetFloorFromButton(button) == floor)
                    return button.transform as RectTransform;
            }

            return null;
        }
    }
}
