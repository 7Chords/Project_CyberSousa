using DG.Tweening;
using SCFrame;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameCore.UI
{
    /// <summary>
    /// 鼠标移入时在图形内部边缘显示描边的 UI 动效。挂到 Image 等 Graphic 节点即可使用。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public class UIHoverOutlineEffect : UIEffectBase
    {
        private const string OutlineShaderName = "GameCore/UI/Outline";

        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");

        [Header("Graphic 目标（为空则使用自身）")]
        [SerializeField] private Graphic targetGraphic;

        [Header("描边颜色")]
        [SerializeField] private Color outlineColor = Color.white;

        [Header("描边宽度（像素）")]
        [SerializeField] private float outlineWidth = 2f;

        private Material _originMaterial;
        private Material _outlineMaterial;
        private bool _hasStoredOriginMaterial;
        private bool _outlineMaterialApplied;
        private float _currentOutlineWidth;
        private Action<PointerEventData, object[]> _onEnter;
        private Action<PointerEventData, object[]> _onExit;

        protected override void Awake()
        {
            base.Awake();
            if (targetGraphic == null)
                targetGraphic = GetComponent<Graphic>();

            if (targetGraphic == null)
                SCDebugHelper.LogError($"[{nameof(UIHoverOutlineEffect)}] 未找到 Graphic 组件: {name}");
        }

        protected override void OnDestroy()
        {
            restoreOriginalMaterial();
            if (_outlineMaterial != null)
            {
                if (Application.isPlaying)
                    Destroy(_outlineMaterial);
                else
                    DestroyImmediate(_outlineMaterial);
            }

            base.OnDestroy();
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
            applyOutlineWidth(0f);
            restoreOriginalMaterial();
        }

        private void onMouseEnter(PointerEventData eventData, object[] args)
        {
            if (!CanPlayEffect() || targetGraphic == null)
                return;

            applyOutlineMaterial();
            playOutlineWidth(outlineWidth);
        }

        private void onMouseExit(PointerEventData eventData, object[] args)
        {
            if (targetGraphic == null)
                return;

            if (!CanPlayEffect())
            {
                ResetState();
                return;
            }

            playOutlineWidth(0f, restoreOriginalMaterial);
        }

        private void ensureOutlineMaterial()
        {
            if (_outlineMaterial != null)
                return;

            Shader outlineShader = Shader.Find(OutlineShaderName);
            if (outlineShader == null)
            {
                SCDebugHelper.LogError($"[{nameof(UIHoverOutlineEffect)}] 未找到 Shader: {OutlineShaderName}");
                return;
            }

            _outlineMaterial = new Material(outlineShader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private void applyOutlineMaterial()
        {
            if (targetGraphic == null)
                return;

            ensureOutlineMaterial();
            if (_outlineMaterial == null)
                return;

            if (!_hasStoredOriginMaterial)
            {
                _originMaterial = targetGraphic.material;
                _hasStoredOriginMaterial = true;
            }

            syncMaterialFromGraphic();
            applyOutlineWidth(0f);
            targetGraphic.material = _outlineMaterial;
            _outlineMaterialApplied = true;
        }

        private void restoreOriginalMaterial()
        {
            if (!_outlineMaterialApplied || targetGraphic == null)
                return;

            if (_originMaterial != null)
                targetGraphic.material = _originMaterial;
            else
                targetGraphic.material = targetGraphic.defaultMaterial;

            _outlineMaterialApplied = false;
        }

        private void syncMaterialFromGraphic()
        {
            if (_outlineMaterial == null || targetGraphic == null)
                return;

            Texture mainTexture = targetGraphic.mainTexture;
            if (mainTexture != null)
                _outlineMaterial.SetTexture(MainTexId, mainTexture);

            if (_originMaterial != null)
                _outlineMaterial.CopyPropertiesFromMaterial(_originMaterial);

            if (mainTexture != null)
                _outlineMaterial.SetTexture(MainTexId, mainTexture);

            _outlineMaterial.SetColor(ColorId, targetGraphic.color);
            _outlineMaterial.SetColor(OutlineColorId, outlineColor);
        }

        private void playOutlineWidth(float targetWidth, TweenCallback onComplete = null)
        {
            KillAllTweens();
            Tween tween = DOTween.To(() => _currentOutlineWidth, applyOutlineWidth, targetWidth, duration).SetEase(ease);
            if (onComplete != null)
                tween.OnComplete(onComplete);
            RegTween(tween);
        }

        private void applyOutlineWidth(float width)
        {
            _currentOutlineWidth = width;
            if (_outlineMaterial != null)
                _outlineMaterial.SetFloat(OutlineWidthId, width);
        }
    }
}
