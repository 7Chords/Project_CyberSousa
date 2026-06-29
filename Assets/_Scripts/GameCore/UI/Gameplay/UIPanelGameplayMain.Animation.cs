using DG.Tweening;
using UnityEngine;

namespace GameCore.UI
{
    public partial class UIPanelGameplayMain
    {

        public void OpenElevatorDoor(TweenCallback onComplete = null)
        {
            Tween tween = mono.elevatorView?.PlayOpen(onComplete);
            if (tween == null)
                onComplete?.Invoke();
        }


        public void CloseElevatorDoor(TweenCallback onComplete = null)
        {
            Tween tween = mono.elevatorView?.PlayClose(onComplete);
            if (tween == null)
                onComplete?.Invoke();
        }


        private void SetElevatorDoorClosedInstant()
        {
            mono.elevatorView?.SetDoorClosedInstant();
        }


        private void ShowCurrentCustomer()
        {
            if (_currentCustomerRefData == null)
                return;

            ResetCustomerPortraitTransform();
            mono.customerMoveScaleEffect?.SetOriginInstant();
            Tween tween = mono.customerFadeEffect?.PlayFadeIn();
            if (tween == null && mono.customerCanvasGroup != null)
                mono.customerCanvasGroup.alpha = 1f;
        }


        private void HideCurrentCustomer(TweenCallback onComplete = null)
        {
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
            ResetCustomerPortraitTransform();
            mono.customerMoveScaleEffect?.SetOriginInstant();
            mono.customerFadeEffect?.SetVisibleInstant(false);
            if (mono.customerCanvasGroup != null)
                mono.customerCanvasGroup.alpha = 0f;
        }


        private void PlayCustomerBoardElevator(TweenCallback onComplete = null)
        {
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


        private void PlayCustomerLeaveElevator(TweenCallback onComplete = null)
        {
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


        private void PlayElevatorTravelToFloor(int targetFloor, TweenCallback onComplete)
        {
            Sequence sequence = DOTween.Sequence();
            int fromFloor = _currentFloor;
            if (fromFloor != targetFloor)
            {
                GameTimeMgr.instance.PlayElevatorTravelAdvance(ElevatorTravelAnimDuration);
                sequence.AppendCallback(() => AnimateFloorText(targetFloor));
                Tween shakeTween = mono.elevatorView?.PlayTravelShake();
                if (shakeTween != null)
                    sequence.Join(shakeTween);
            }

            sequence.AppendInterval(0.08f);
            sequence.AppendCallback(() =>
            {
                _currentFloor = targetFloor;
                RefreshFloorUi();
                onComplete?.Invoke();
            });
        }


        private void AnimateFloorText(int floor)
        {
            string text = $"{floor}";
            if (mono.floorTextEffect != null)
            {
                mono.floorTextEffect.PlayTextChange(text);
                return;
            }

            if (mono.txtCurrentFloor != null)
                mono.txtCurrentFloor.text = text;
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
