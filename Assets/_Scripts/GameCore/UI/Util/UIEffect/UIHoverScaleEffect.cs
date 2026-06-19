using DG.Tweening;
using SCFrame;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameCore.UI
{
    /// <summary>
    /// 鼠标移入移出时的缩放动效。挂到 Button 或任意可接收指针事件的 UI 节点即可。
    /// </summary>
    [DisallowMultipleComponent]
    public class UIHoverScaleEffect : UIEffectBase
    {
        [Header("移入缩放倍率（相对初始 Scale）")]
        [SerializeField] private float hoverScale = 1.1f;

        private Vector3 _originScale;

        private Action<PointerEventData, object[]> _onEnter;
        private Action<PointerEventData, object[]> _onExit;

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
            this.AddMouseEnter(_onEnter);
            this.AddMouseExit(_onExit);
        }

        protected override void UnregisterEvents()
        {
            if (_onEnter != null)
                this.RemoveMouseEnter(_onEnter);
            if (_onExit != null)
                this.RemoveMouseExit(_onExit);
        }

        protected override void ResetState()
        {
            if (TargetRect != null)
                TargetRect.localScale = _originScale;
        }

        private void onMouseEnter(PointerEventData _data, object[] _args)
        {
            if (!CanPlayEffect())
                return;

            playScale(_originScale * hoverScale);
        }

        private void onMouseExit(PointerEventData _data, object[] _args)
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
