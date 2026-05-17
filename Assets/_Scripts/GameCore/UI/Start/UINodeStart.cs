using SCFrame;
using SCFrame.UI;
using UnityEngine;

namespace GameCore.UI
{
    public class UINodeStart : _ASCUINodeBase
    {
        public UINodeStart(SCUIShowType _showType) : base(_showType)
        {
        }

        public override bool needHideWhenEnterNewSameTypeNode => true;

        public override bool needShowWhenQuitNewSameTypeNode => true;

        public override bool canQuitByEsc => false;

        public override bool canQuitByMouseRight => false;

        public override bool ignoreOnUIList => false;

        public override bool needMoveToBottomWhenHide => false;

        private GameObject _m_panelGO;
        private UIPanelStart _m_startPanel;
        private UIMonoStart _m_startMono;

        public override void CopyData(_ASCUINodeBase _anotherNode)
        {
        }

        public override string GetNodeName()
        {
            return nameof(UINodeStart);
        }

        public override string GetResName()
        {
            return "panel_start";
        }

        public override void OnEnterNode()
        {
            _m_panelGO = ResourcesHelper.LoadGameObject(GetResName(), GetRootTransform(), true);
            if (_m_panelGO == null)
            {
                Debug.LogError("UI resource not found: " + GetResName());
                return;
            }
            _m_startMono = _m_panelGO.GetComponent<UIMonoStart>();
            if (_m_startMono == null)
            {
                Debug.LogError("UIMonoBattleMain is missing on resource: " + GetResName());
                return;
            }
            _m_startPanel = new UIPanelStart(_m_startMono, _m_showType);
            _m_startPanel.Initialize();
        }

        public override void OnHideNode()
        {
            if (_m_startPanel == null)
                return;
            _m_startPanel.HidePanel();
            SCCommon.SetGameObjectEnable(_m_panelGO, false);
        }

        public override void OnQuitNode()
        {
            if (_m_startPanel == null)
                return;
            _m_startPanel.Discard();
            SCCommon.DestoryGameObject(_m_panelGO);
        }

        public override void OnShowNode()
        {
            if (_m_startPanel == null)
                return;
            SCCommon.SetGameObjectEnable(_m_panelGO, true);
            _m_startPanel.ShowPanel();
        }
    }
}
