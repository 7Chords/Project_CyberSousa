using SCFrame;
using SCFrame.UI;
using System;
using UnityEngine;

namespace GameCore.UI
{
    /// <summary>
    /// 通用 UI 节点模板
    /// 各业务面板指定自己的 Mono 与 Panel 类型即可，无需重复写 Node 生命周期。
    /// </summary>
    public class UINodeCommon<TMono, TPanel> : _ASCUINodeBase
        where TMono : _ASCUIMonoBase
        where TPanel : _ASCUIPanelBase<TMono>
    {
        private bool _m_needHideWhenEnterNewSameTypeNode;
        private bool _m_needShowWhenQuitNewSameTypeNode;
        private bool _m_canQuitByEsc;
        private bool _m_canQuitByMouseRight;
        private bool _m_ignoreOnUIList;
        private bool _m_needMoveToBottomWhenHide;

        private string _m_uiResName;
        private string _m_uiNodeName;

        private GameObject _m_panelGO;
        private TMono _m_mono;
        private TPanel _m_panel;

        public UINodeCommon(SCUIShowType _showType,
            string _uiResName,
            string _uiNodeName,
            bool _needHideWhenEnterNewSameTypeNode,
            bool _needShowWhenQuitNewSameTypeNode,
            bool _canQuitByEsc,
            bool _canQuitByMouseRight,
            bool _ignoreOnUIList = false,
            bool _needMoveToBottomWhenHide = false) : base(_showType)
        {
            _m_uiResName = _uiResName;
            _m_uiNodeName = _uiNodeName;
            _m_needHideWhenEnterNewSameTypeNode = _needHideWhenEnterNewSameTypeNode;
            _m_needShowWhenQuitNewSameTypeNode = _needShowWhenQuitNewSameTypeNode;
            _m_canQuitByEsc = _canQuitByEsc;
            _m_canQuitByMouseRight = _canQuitByMouseRight;
            _m_ignoreOnUIList = _ignoreOnUIList;
            _m_needMoveToBottomWhenHide = _needMoveToBottomWhenHide;
        }

        public override bool needHideWhenEnterNewSameTypeNode => _m_needHideWhenEnterNewSameTypeNode;
        public override bool needShowWhenQuitNewSameTypeNode => _m_needShowWhenQuitNewSameTypeNode;
        public override bool canQuitByEsc => _m_canQuitByEsc;
        public override bool canQuitByMouseRight => _m_canQuitByMouseRight;
        public override bool ignoreOnUIList => _m_ignoreOnUIList;
        public override bool needMoveToBottomWhenHide => _m_needMoveToBottomWhenHide;

        public override string GetNodeName() => _m_uiNodeName;
        public override string GetResName() => _m_uiResName;

        public override void CopyData(_ASCUINodeBase _anotherNode)
        {
        }

        public override void OnEnterNode()
        {
            _m_panelGO = ResourcesHelper.LoadGameObject(GetResName(), GetRootTransform(), true);
            if (_m_panelGO == null)
            {
                Debug.LogError($"[{GetNodeName()}] UI resource not found: {GetResName()}");
                return;
            }

            _m_mono = _m_panelGO.GetComponent<TMono>();
            if (_m_mono == null)
            {
                Debug.LogError($"[{GetNodeName()}] {typeof(TMono).Name} is missing on resource: {GetResName()}");
                SCCommon.DestoryGameObject(_m_panelGO);
                _m_panelGO = null;
                return;
            }

            _m_panel = createPanel(_m_mono, _m_showType);
            _m_panel.Initialize();
        }

        public override void OnShowNode()
        {
            if (_m_panel == null)
                return;

            SCCommon.SetGameObjectEnable(_m_panelGO, true);
            _m_panel.ShowPanel();
        }

        public override void OnHideNode()
        {
            if (_m_panel == null)
                return;

            _m_panel.HidePanel();
            SCCommon.SetGameObjectEnable(_m_panelGO, false);
        }

        public override void OnQuitNode()
        {
            if (_m_panel == null)
                return;

            _m_panel.Discard();
            _m_panel = null;
            _m_mono = null;
            _m_panelGO = null;
        }

        protected virtual TPanel createPanel(TMono _mono, SCUIShowType _showType)
        {
            return (TPanel)Activator.CreateInstance(typeof(TPanel), _mono, _showType);
        }
    }
}
