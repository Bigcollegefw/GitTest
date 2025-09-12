using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;


namespace TKFramework
{
    public class AutoController : FxiedController
    {
        public override void OnItemLoad(Transform tf, int index)
        {
            tf.GetChildComponent<TextMeshProUGUI>("Text").text = index.ToString();
            // tf.OnClick("Button", index, OnItemClick);
            Vector2 size = index % 3 == 0 ? new Vector2(105, 105) : new Vector2(200, 200);
            size = index % 3 == 1 ? new Vector2(150, 150) : size;
            tf.GetComponent<RectTransform>().sizeDelta = size;
        }

        public override void OnItemClick(int index)
        {
            Transform tf = itemList.GetItem(index);
            tf.GetChildComponent<TextMeshProUGUI>("Text").text = "点击了" + index.ToString();
            tf.GetComponent<RectTransform>().sizeDelta = new Vector2(130, 130);
        }
    }
}



