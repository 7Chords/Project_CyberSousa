using DG.Tweening;
using SCFrame;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameCore.UI
{
    /// <summary>
    /// 鼠标按下抬起时的缩放动效。适合只需要点击反馈、不需要移入放大的控件。
    /// 同一节点请勿与 <see cref="UIHoverScaleEffect"/> 或 <see cref="UIScaleInteractEffect"/> 同时挂载。
    /// </summary>
    [DisallowMultipleComponent]
    public class UIPressScaleEffect : UIEffectBase
    {
        [Header("按下缩放倍率（相对初始 Scale）")]
        [SerializeField] private float pressScale = 0.92f;

        private Vector3 _originScale;

        private Action<PointerEventData, object[]> _onDown;
        private Action<PointerEventData, object[]> _onUp;

        protected override void Awake()
        {
            base.Awake();
            if (TargetRect != null)
                _originScale = TargetRect.localScale;
        }

        protected override void RegisterEvents()
        {
            _onDown = onMouseDown;
            _onUp = onMouseUp;
            this.AddMouseLeftClickDown(_onDown);
            this.AddMouseLeftClickUp(_onUp);
        }

        protected override void UnregisterEvents()
        {
            if (_onDown != null)
                this.RemoveMouseLeftClickDown(_onDown);
            if (_onUp != null)
                this.RemoveMouseLeftClickUp(_onUp);
        }

        protected override void ResetState()
        {
            if (TargetRect != null)
                TargetRect.localScale = _originScale;
        }

        private void onMouseDown(PointerEventData _data, object[] _args)
        {
            if (!CanPlayEffect())
                return;

            playScale(_originScale * pressScale);
        }

        private void onMouseUp(PointerEventData _data, object[] _args)
        {
            if (!CanPlayEffect())
            {
                ResetState();
                return;
            }

            playScale(_originScale);
        }

        private void playScale(Vector3 _targetScale)
        {
            KillAllTweens();
            RegTween(TargetRect.DOScale(_targetScale, duration).SetEase(ease));
        }
    }
}
