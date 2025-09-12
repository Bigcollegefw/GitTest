using UnityEngine;
//using UnityEngine.InputSystem;
using TKFramework;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

namespace TKFramework
{
    public class FxiedController : MonoBehaviour
    {
        public VirtualItemList itemList;
        public int num = 30;
        public bool isCenter = true;


        public void Start()
        {
            itemList.SetItemNum(num, OnItemLoad);
            itemList.SetScrollTrigger(OnScrollTop, OnScrollBottom);
        }

        public void Update()
        {

        }

        public virtual void OnItemLoad(Transform tf, int index)
        {
            tf.GetChildComponent<TextMeshProUGUI>("Text").text = index.ToString();
            // tf.OnClick("Button", index, OnItemClick);
        }

        public virtual void OnItemClick(int index)
        {
            Transform tf = itemList.GetItem(index);
            tf.GetChildComponent<TextMeshProUGUI>("Text").text = "点击了" + index.ToString();
        }

        public void OnScrollTop()
        {
            Debug.Log("scroll to top");
        }

        public void OnScrollBottom()
        {
            Debug.Log("scroll to bottom");
        }
    }
}



