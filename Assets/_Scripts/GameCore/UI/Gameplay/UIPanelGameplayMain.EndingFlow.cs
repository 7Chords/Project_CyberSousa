using DG.Tweening;
using UnityEngine;

namespace GameCore.UI
{
    public partial class UIPanelGameplayMain
    {
        private const long FinalSpecialCustomerNotConfirmedDialogueId = 1592;
        private const long FinalSpecialCustomerConfirmedDialogueId = 1642;

        private bool ShouldPlayFinalSpecialCustomerDepartureDialogue(bool needBoardAnimation)
        {
            return needBoardAnimation && IsFinalDayLastSpecialCustomer();
        }


        private void PlayFinalSpecialCustomerDepartureDialogue(TweenCallback onComplete = null)
        {
            _isCustomerInfoVisible = false;
            RefreshCustomerUi();

            mono.customerMoveScaleEffect?.SetEnterStateInstant();
            mono.customerFadeEffect?.SetVisibleInstant(true);
            if (mono.customerCanvasGroup != null)
                mono.customerCanvasGroup.alpha = 1f;

            Tween flipToFrontTween = CreateCustomerPortraitFlipToFrontTween();
            long dialogueStartId = _hasConfirmedCurrentCustomer
                ? FinalSpecialCustomerConfirmedDialogueId
                : FinalSpecialCustomerNotConfirmedDialogueId;

            TweenCallback startDialogueAction = () => StartDialogue(dialogueStartId, () =>
            {
                Tween flipBackTween = CreateCustomerPortraitFlipToBackTween();
                if (flipBackTween != null)
                {
                    flipBackTween.OnComplete(() => PlayCustomerLeaveElevator(onComplete));
                    return;
                }

                SwitchCustomerPortraitToBack();
                PlayCustomerLeaveElevator(onComplete);
            }, false);

            if (flipToFrontTween != null)
            {
                flipToFrontTween.OnComplete(startDialogueAction);
                return;
            }

            SwitchCustomerPortraitToFront();
            startDialogueAction.Invoke();
        }
    }
}
