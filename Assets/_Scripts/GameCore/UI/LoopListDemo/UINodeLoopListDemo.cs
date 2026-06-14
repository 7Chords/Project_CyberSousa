using SCFrame;
using SCFrame.UI;
using UnityEngine;

namespace GameCore.UI
{
    public class UINodeLoopListDemo : _ASCUINodeBase
    {
        private GameObject _panelGO;
        private UIPanelLoopListDemo _panel;
        private UIMonoLoopListDemo _mono;

        public UINodeLoopListDemo(SCUIShowType _showType) : base(_showType)
        {
        }

        public override bool needHideWhenEnterNewSameTypeNode => true;
        public override bool needShowWhenQuitNewSameTypeNode => true;
        public override bool canQuitByEsc => false;
        public override bool canQuitByMouseRight => false;
        public override bool ignoreOnUIList => false;
        public override bool needMoveToBottomWhenHide => false;

        public override string GetNodeName()
        {
            return nameof(UINodeLoopListDemo);
        }

        public override string GetResName()
        {
            return "panel_loop_list_demo";
        }

        public override void CopyData(_ASCUINodeBase _anotherNode)
        {
        }

        public override void OnEnterNode()
        {
            _panelGO = ResourcesHelper.LoadGameObject(GetResName(), GetRootTransform(), true);
            if (_panelGO == null)
            {
                Debug.LogError($"[{GetNodeName()}] UI resource not found: {GetResName()}");
                return;
            }

            _mono = _panelGO.GetComponent<UIMonoLoopListDemo>();
            if (_mono == null)
            {
                Debug.LogError($"[{GetNodeName()}] {nameof(UIMonoLoopListDemo)} missing on resource: {GetResName()}");
                return;
            }

            _panel = new UIPanelLoopListDemo(_mono, _m_showType);
            _panel.Initialize();
        }

        public override void OnHideNode()
        {
            _panel?.HidePanel();
        }

        public override void OnQuitNode()
        {
            _panel?.Discard();
            _panel = null;
            _mono = null;
            _panelGO = null;
        }

        public override void OnShowNode()
        {
            _panel?.ShowPanel();
        }
    }
}
