using DG.Tweening;
using UnityEngine;

namespace GameCore.UI
{
    public partial class UIPanelGameplayMain
    {

        public void OpenElevatorDoor(TweenCallback onComplete = null)
        {
            RefreshFloorSceneBackgroundUi(_currentFloor);
            Tween tween = mono.elevatorView?.PlayOpen(onComplete);
            if (tween == null)
                onComplete?.Invoke();
        }


        private void EnsureElevatorDoorOpenForCustomer(TweenCallback onComplete = null)
        {
            if (mono.elevatorView != null && mono.elevatorView.IsDoorOpen())
            {
                RefreshFloorSceneBackgroundUi(_currentFloor);
                onComplete?.Invoke();
                return;
            }

            OpenElevatorDoor(onComplete);
        }


        public void CloseElevatorDoor(TweenCallback onComplete = null)
        {
            if (mono.elevatorView != null && !mono.elevatorView.IsDoorOpen())
            {
                onComplete?.Invoke();
                return;
            }

            AudioMgr.instance.PlaySfx(AudioKeys.ElevatorDoorClose);
            AudioMgr.instance.PlaySfx(AudioKeys.Ui);
            Tween tween = mono.elevatorView?.PlayClose(onComplete);
            if (tween == null)
                onComplete?.Invoke();
        }


        private void SetElevatorDoorClosedInstant()
        {
            mono.elevatorView?.SetDoorClosedInstant();
        }


        private void ShowCurrentCustomer(TweenCallback onFullyDisplayed = null)
        {
            if (_currentCustomerRefData == null)
                return;

            AudioMgr.instance.PlaySfx(AudioKeys.CustomerStep);
            _isCustomerInfoVisible = false;
            RefreshCustomerPortraitUi();
            BringCustomerLayerToFront();
            ResetCustomerPortraitTransform();
            mono.customerMoveScaleEffect?.SetOriginInstant();

            Tween fadeTween = mono.customerFadeEffect?.PlayFadeIn();
            if (fadeTween == null)
            {
                if (mono.customerCanvasGroup != null)
                    mono.customerCanvasGroup.alpha = 1f;
                CompleteCustomerEnterDisplay(onFullyDisplayed);
                return;
            }

            fadeTween.OnComplete(() => CompleteCustomerEnterDisplay(onFullyDisplayed));
        }


        private void CompleteCustomerEnterDisplay(TweenCallback onFullyDisplayed = null)
        {
            _isCustomerInfoVisible = true;
            RefreshCustomerUi();
            onFullyDisplayed?.Invoke();
        }


        private void HideCurrentCustomer(TweenCallback onComplete = null)
        {
            _isCustomerInfoVisible = false;
            RefreshCustomerUi();

            Tween tween = mono.customerFadeEffect?.PlayFadeOut(onComplete);
            if (tween == null)
            {
                if (mono.customerCanvasGroup != null)
                    mono.customerCanvasGroup.alpha = 0f;
                onComplete?.Invoke();
            }
        }


        private void SetCustomerHiddenInstant()
        {
            ClearCustomerPortraitUi();
            ResetCustomerPortraitTransform();
            RestoreCustomerLayerHomeParent();
            mono.customerMoveScaleEffect?.SetOriginInstant();
            mono.customerFadeEffect?.SetVisibleInstant(false);
            if (mono.customerCanvasGroup != null)
                mono.customerCanvasGroup.alpha = 0f;
            RefreshCustomerUi();
        }


        private void PlayCustomerBoardElevator(TweenCallback onComplete = null)
        {
            BringCustomerLayerToFront();
            mono.customerMoveScaleEffect?.SetOriginInstant();
            mono.customerFadeEffect?.SetVisibleInstant(true);
            if (mono.customerCanvasGroup != null)
                mono.customerCanvasGroup.alpha = 1f;

            Tween moveTween = mono.customerMoveScaleEffect?.PlayEnter();
            Tween flipTween = CreateCustomerPortraitFlipToBackTween();
            if (moveTween == null && flipTween == null)
            {
                SwitchCustomerPortraitToBack();
                onComplete?.Invoke();
                return;
            }

            Sequence sequence = DOTween.Sequence();
            if (moveTween != null)
                sequence.Join(moveTween);
            if (flipTween != null)
                sequence.Join(flipTween);
            if (onComplete != null)
                sequence.OnComplete(onComplete);
        }


        private Tween CreateCustomerPortraitFlipToBackTween()
        {
            if (mono.imgDialoguePortrait == null || !mono.imgDialoguePortrait.enabled)
            {
                SwitchCustomerPortraitToBack();
                return null;
            }

            if (_currentCustomerRefData == null
                || !IsValidPortraitResName(_currentCustomerRefData.portraitBackResName))
            {
                SwitchCustomerPortraitToBack();
                return null;
            }

            RectTransform portraitRect = mono.imgDialoguePortrait.rectTransform;
            portraitRect.DOKill();
            portraitRect.localRotation = Quaternion.identity;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(
                portraitRect
                    .DOLocalRotate(new Vector3(0f, 90f, 0f), CustomerPortraitFlipHalfDuration)
                    .SetEase(Ease.InQuad)
                    .SetUpdate(true));
            sequence.AppendCallback(SwitchCustomerPortraitToBack);
            sequence.Append(
                portraitRect
                    .DOLocalRotate(Vector3.zero, CustomerPortraitFlipHalfDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true));
            return sequence;
        }


        private void ResetCustomerPortraitTransform()
        {
            if (mono.imgDialoguePortrait == null)
                return;

            mono.imgDialoguePortrait.rectTransform.DOKill();
            mono.imgDialoguePortrait.rectTransform.localRotation = Quaternion.identity;
        }


        private void BringCustomerLayerToFront()
        {
            if (mono.customerCanvasGroup == null)
                return;

            Transform customerTransform = mono.customerCanvasGroup.transform;
            if (_customerLayerHomeParent == null)
                _customerLayerHomeParent = customerTransform.parent;

            Transform frontParent = FindChildTransformRecursive(mono.transform, "CenterBand");
            if (frontParent == null)
                frontParent = mono.transform;

            if (customerTransform.parent != frontParent)
                customerTransform.SetParent(frontParent, true);

            customerTransform.SetAsLastSibling();
        }


        private void RestoreCustomerLayerHomeParent()
        {
            if (mono.customerCanvasGroup == null || _customerLayerHomeParent == null)
                return;

            Transform customerTransform = mono.customerCanvasGroup.transform;
            if (customerTransform.parent != _customerLayerHomeParent)
                customerTransform.SetParent(_customerLayerHomeParent, true);
        }


        private void PlayCustomerLeaveElevator(TweenCallback onComplete = null)
        {
            _isCustomerInfoVisible = false;
            RefreshCustomerUi();

            mono.customerMoveScaleEffect?.SetEnterStateInstant();
            mono.customerFadeEffect?.SetVisibleInstant(true);
            Tween moveTween = mono.customerMoveScaleEffect?.PlayExit();
            Tween fadeTween = mono.customerFadeEffect?.PlayFadeOut();

            if (moveTween == null && fadeTween == null)
            {
                onComplete?.Invoke();
                return;
            }

            Sequence sequence = DOTween.Sequence();
            if (moveTween != null)
                sequence.Join(moveTween);
            if (fadeTween != null)
                sequence.Join(fadeTween);
            if (onComplete != null)
                sequence.OnComplete(onComplete);
        }


        private void PlayElevatorTravelToFloor(int targetFloor, TweenCallback onComplete, bool playArrivalBell = true)
        {
            Sequence sequence = DOTween.Sequence();
            int fromFloor = _currentFloor;
            if (fromFloor != targetFloor)
            {
                AudioMgr.instance.PlaySfx(AudioKeys.ElevatorTravel);
                _isFloorDisplayAnimating = true;
                GameTimeMgr.instance.PlayElevatorTravelAdvance(ElevatorTravelAnimDuration);
                Tween floorTween = AnimateFloorTextSequence(fromFloor, targetFloor, ElevatorTravelAnimDuration);
                if (floorTween != null)
                    sequence.Join(floorTween);
                else
                    _isFloorDisplayAnimating = false;

                Tween shakeTween = mono.elevatorView?.PlayTravelShake(ElevatorTravelAnimDuration);
                if (shakeTween != null)
                    sequence.Join(shakeTween);
            }

            sequence.AppendInterval(0.08f);
            sequence.AppendCallback(() =>
            {
                _isFloorDisplayAnimating = false;
                SetCurrentFloorDisplayInstant(targetFloor);
                if (fromFloor != targetFloor && playArrivalBell)
                    AudioMgr.instance.PlaySfx(AudioKeys.ElevatorBell);
                onComplete?.Invoke();
            });
        }


        private Tween AnimateFloorTextSequence(int fromFloor, int toFloor, float totalDuration)
        {
            if (mono.floorTextEffect != null)
                return mono.floorTextEffect.PlayFloorCountSequence(fromFloor, toFloor, totalDuration);

            if (fromFloor == toFloor || mono.txtCurrentFloor == null)
                return null;

            mono.txtCurrentFloor.text = $"{fromFloor}";

            int direction = fromFloor < toFloor ? 1 : -1;
            int stepCount = Mathf.Abs(toFloor - fromFloor);
            float stepDuration = totalDuration / stepCount;

            Sequence sequence = DOTween.Sequence();
            for (int stepIndex = 0; stepIndex < stepCount; stepIndex++)
            {
                int nextFloor = fromFloor + direction * (stepIndex + 1);
                sequence.AppendCallback(() => mono.txtCurrentFloor.text = $"{nextFloor}");
                sequence.AppendInterval(stepDuration);
            }

            return sequence;
        }


        private void SetCurrentFloorDisplayInstant(int floor)
        {
            _currentFloor = floor;
            string text = $"{floor}";
            if (mono.floorTextEffect != null)
                mono.floorTextEffect.SetTextInstant(text);
            else if (mono.txtCurrentFloor != null)
                mono.txtCurrentFloor.text = text;
        }
    }
}
