using SCFrame.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
{
    public class UIMonoCommonContainer : _ASCUIMonoBase
    {
        [Header("Item模板资源obj名")]
        public string prefabItemObjName;
        [Header("布局组件")]
        public LayoutGroup layoutGroup;
    }

}
