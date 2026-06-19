using DG.Tweening;
using SCFrame;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameCore.UI
{
    /// <summary>
    /// 按钮交互缩放动效（移入放大 + 按下缩小）。推荐挂到 Button 上使用。
    /// 同一节点请勿与 <see cref="UIHoverScaleEffect"/> 或 <see cref="UIPressScaleEffect"/> 同时挂载。
    /// </summary>
    [DisallowMultipleComponent]
    public class UIScaleInteractEffect : UIEffectBase
    {
        [Header("移入缩放")]
        [SerializeField] private bool enableHoverScale = true;
        [SerializeField] private float hoverScale = 1.08f;

        [Header("按下缩放")]
        [SerializeField] private bool enablePressScale = true;
        [SerializeField] private float pressScale = 0.95f;

        private Vector3 _originScale;
        private bool _isPointerInside;
        private bool _isPointerDown;

        private Action<PointerEventData, object[]> _onEnter;
        private Action<PointerEventData, object[]> _onExit;
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
            _onEnter = onMouseEnter;
            _onExit = onMouseExit;
            _onDown = onMouseDown;
            _onUp = onMouseUp;

            this.AddMouseEnter(_onEnter);
            this.AddMouseExit(_onExit);
            if (enablePressScale)
            {
                this.AddMouseLeftClickDown(_onDown);
                this.AddMouseLeftClickUp(_onUp);
            }
        }

        protected override void UnregisterEvents()
        {
            if (_onEnter != null)
                this.RemoveMouseEnter(_onEnter);
            if (_onExit != null)
                this.RemoveMouseExit(_onExit);
            if (_onDown != null)
                this.RemoveMouseLeftClickDown(_onDown);
            if (_onUp != null)
                this.RemoveMouseLeftClickUp(_onUp);
        }

        protected override void ResetState()
        {
            if (TargetRect != null)
                TargetRect.localScale = _originScale;
            _isPointerInside = false;
            _isPointerDown = false;
        }

        private void onMouseEnter(PointerEventData _data, object[] _args)
        {
            if (!CanPlayEffect())
                return;

            _isPointerInside = true;
            refreshScale();
        }

        private void onMouseExit(PointerEventData _data, object[] _args)
        {
            _isPointerInside = false;
            _isPointerDown = false;
            if (!CanPlayEffect())
            {
                ResetState();
                return;
            }

            refreshScale();
        }

        private void onMouseDown(PointerEventData _data, object[] _args)
        {
            if (!CanPlayEffect())
                return;

            _isPointerDown = true;
            refreshScale();
        }

        private void onMouseUp(PointerEventData _data, object[] _args)
        {
            _isPointerDown = false;
            if (!CanPlayEffect())
            {
                ResetState();
                return;
            }

            refreshScale();
        }

        private void refreshScale()
        {
            float scaleFactor = 1f;
            if (_isPointerDown && enablePressScale)
                scaleFactor = pressScale;
            else if (_isPointerInside && enableHoverScale)
                scaleFactor = hoverScale;

            KillAllTweens();
            RegTween(TargetRect.DOScale(_originScale * scaleFactor, duration).SetEase(ease));
        }
    }
}
