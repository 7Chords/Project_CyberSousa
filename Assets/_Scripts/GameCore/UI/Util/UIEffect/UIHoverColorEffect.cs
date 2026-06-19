using DG.Tweening;
using SCFrame;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameCore.UI
{
    /// <summary>
    /// 鼠标移入移出时的颜色过渡动效。挂到带 Graphic 组件（Image / Text 等）的节点，或指定 targetGraphic。
    /// </summary>
    [DisallowMultipleComponent]
    public class UIHoverColorEffect : UIEffectBase
    {
        [Header("颜色目标（为空则使用自身 Graphic）")]
        [SerializeField] private Graphic targetGraphic;

        [Header("移入颜色")]
        [SerializeField] private Color hoverColor = new Color(1f, 1f, 1f, 1f);

        [Header("是否仅作用于 RGB，保留原始 Alpha")]
        [SerializeField] private bool keepOriginAlpha = true;

        private Color _originColor;
        private Action<PointerEventData, object[]> _onEnter;
        private Action<PointerEventData, object[]> _onExit;

        protected override void Awake()
        {
            base.Awake();
            if (targetGraphic == null)
                targetGraphic = GetComponent<Graphic>();

            if (targetGraphic == null)
                SCDebugHelper.LogError($"[{nameof(UIHoverColorEffect)}] 未找到 Graphic 组件: {name}");
            else
                _originColor = targetGraphic.color;
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
            if (targetGraphic != null)
                targetGraphic.color = _originColor;
        }

        private void onMouseEnter(PointerEventData _data, object[] _args)
        {
            if (!CanPlayEffect() || targetGraphic == null)
                return;

            playColor(getHoverColor());
        }

        private void onMouseExit(PointerEventData _data, object[] _args)
        {
            if (targetGraphic == null)
                return;

            if (!CanPlayEffect())
            {
                ResetState();
                return;
            }

            playColor(_originColor);
        }

        private Color getHoverColor()
        {
            if (!keepOriginAlpha)
                return hoverColor;

            Color color = hoverColor;
            color.a = _originColor.a;
            return color;
        }

        private void playColor(Color _targetColor)
        {
            KillAllTweens();
            RegTween(targetGraphic.DOColor(_targetColor, duration).SetEase(ease));
        }
    }
}
