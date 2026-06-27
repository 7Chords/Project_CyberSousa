using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
{
    public partial class UIPanelGameplayMain
    {

        private Text FindChildText(string objectName)
        {
            Transform target = FindChildTransformRecursive(mono.transform, objectName);
            if (target == null)
                return null;

            return target.GetComponent<Text>();
        }


        private Image FindChildImage(string objectName)
        {
            Transform target = FindChildTransformRecursive(mono.transform, objectName);
            if (target == null)
                return null;

            return target.GetComponent<Image>();
        }


        private RectTransform FindChildRect(string objectName)
        {
            Transform target = FindChildTransformRecursive(mono.transform, objectName);
            if (target == null)
                return null;

            return target as RectTransform;
        }


        private Transform FindChildTransformRecursive(Transform parent, string objectName)
        {
            if (parent == null)
                return null;

            if (parent.name == objectName)
                return parent;

            for (int index = 0; index < parent.childCount; index++)
            {
                Transform child = parent.GetChild(index);
                Transform result = FindChildTransformRecursive(child, objectName);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}
