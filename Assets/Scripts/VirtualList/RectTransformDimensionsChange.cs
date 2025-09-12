using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TKFramework
{
    public class RectTransformDimensionsChange : MonoBehaviour
    {
        public int index;
        public Action<int> action;

        void OnRectTransformDimensionsChange()
        {
            action?.Invoke(index);
        }
    }
}
