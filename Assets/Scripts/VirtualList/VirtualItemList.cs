using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TKFramework
{
    public enum ItemCountMode { Add, Reduce, Total }
    public class VirtualItemList : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        #region 枚举
        public enum Direction { Horizontal, Vertical }
        public enum SizeType { Fixed, Auto }
        private enum IndexType { None, Before, After }
        private enum Align { LeftOrTop, Center, RightOrBottom }
        private enum OffsetType { Start, Same, End }
        private enum ScrollEdge { None, Start, End }
        #endregion
        private class ItemNode
        {
            public int index;
            public Transform tf;
        }

        [Serializable]
        public class Padding
        {
            public float left;
            public float right;
            public float top;
            public float bottom;
        }

        #region 变量
        [SerializeField] private Scrollbar scrollbar;
        [SerializeField] private Transform contentTf;
        [SerializeField] private Transform itemPrefab;
        [SerializeField] private SizeType sizeType;
        [SerializeField] private Direction direction = Direction.Vertical;
        [SerializeField] private Align childAlign = Align.Center;
        [SerializeField] private Padding padding = new Padding();
        [SerializeField] private Vector2 spacing = new Vector2(10, 10);
        [SerializeField] private int crossDireCount = 1;

        private float defaultDireItemLen = 100;
        public AVLT<float> direItemLenTree = new AVLT<float>();
        public SortedDictionary<int, Vector2> itemSizeDict = new SortedDictionary<int, Vector2>();
        private HashSet<int> cacheChangeSizeList = new HashSet<int>();
        private LinkedList<ItemNode> itemLinkedList = new LinkedList<ItemNode>();
        private GOPool gOPool;
        private Vector2 showOffsetRange;
        private OffsetType offsetType;
        private Action OnScrollToStart;
        private Action OnScrollToEnd;
        private Action<Transform, int> OnItemLoad;
        private DrivenRectTransformTracker tracker;
        private int itemCount;
        private int maxShowNum;
        private int rowOrColCount;
        private float direSpacingLen;
        private float contentWidth;
        private float contentHeight;
        private float direContentLen;
        private float itemWidth;
        private float itemHeight;
        private float direItemLen;
        private float oldOffsetPos;
        private float offsetPos;
        private float maxOffsetPos;
        private bool isDraging = false;
        private bool isTrigger;

        private Dictionary<Direction, Dictionary<Align, Vector2>> povitOrAnchorDict = new Dictionary<Direction, Dictionary<Align, Vector2>>()
            {
                { Direction.Horizontal, new Dictionary<Align, Vector2>() { { Align.LeftOrTop, new Vector2(0, 1) }, { Align.Center, new Vector2(0, 0.5f) }, { Align.RightOrBottom, new Vector2(0, 0) } } },
                { Direction.Vertical, new Dictionary<Align, Vector2>() { { Align.LeftOrTop, new Vector2(0, 1) }, { Align.Center, new Vector2(0.5f, 1) }, { Align.RightOrBottom, new Vector2(1, 1) } } }
            };
        #endregion

        #region Monobehaviour函数
        private void Awake()
        {
            crossDireCount = Math.Max(1, crossDireCount);
            gOPool = new GOPool(itemPrefab.gameObject, contentTf, 1);
            direItemLenTree.Add(defaultDireItemLen);

            direSpacingLen = direction == Direction.Horizontal ? spacing.x : spacing.y;
            contentWidth = contentTf.GetComponent<RectTransform>().rect.width;
            contentHeight = contentTf.GetComponent<RectTransform>().rect.height;
            direContentLen = direction == Direction.Horizontal ? contentWidth : contentHeight;

            CalculateItemSize();
            UpdateShowOffsetRange();
            scrollbar.onValueChanged.AddListener(OnScrollbarValueChanged);
        }

        private void LateUpdate()
        {
            HandleItemSizeChanged();
        }
        #endregion

        #region 外部可用函数

        public void SetItemNum(int num, Action<Transform, int> OnItemLoad)
        {
            UpdateItemCount(ItemCountMode.Total, num);
            this.OnItemLoad = OnItemLoad;
            for (int i = 0; i < itemCount; i++)
            {
                if (i >= maxShowNum) break;
                InstanceItem(i);
            }
        }

        public void ModifyItemCount(ItemCountMode mode, int num)
        {
            int realNum = num;
            ItemCountMode realMode = mode;
            if (mode == ItemCountMode.Total)
            {
                Init();
                realMode = ItemCountMode.Add;
                realNum = num;
            }

            if (realMode == ItemCountMode.Add)
            {
                AddItemNum(realNum);
                return;
            }
            ReduceItemNum(realNum);
            ResetTriggerState();
        }

        public Transform GetItem(int index)
        {
            foreach (var item in itemLinkedList)
            {
                if (item.index == index)
                    return item.tf;
            }

            return null;
        }


        // TODO
        /// <summary>
        /// 跳转到指定的索引，isCenter是否居中显示
        /// </summary>
        public void JumpToItem(int index, bool isCenter)
        {
            if (index < 0 || index >= itemCount) return;

            oldOffsetPos = offsetPos;

            int rowOrColIndex = index / crossDireCount;
            float spacingLen = rowOrColIndex == 0 ? 0 : direSpacingLen * rowOrColIndex;
            float paddingLen = direction == Direction.Horizontal ? padding.left : padding.top;
            float beforeItemLen = GetBeforeDireItemLen(index);

            // 计算目标元素的起始位置
            float targetPos = beforeItemLen + spacingLen + paddingLen;

            if (isCenter)
            {
                // 尝试居中显示
                int offsetFactor = direction == Direction.Horizontal ? 1 : -1;
                float centerOffset = (direContentLen - direItemLen) / 2;
                float centeredPos = targetPos + centerOffset * offsetFactor;

                // 检查居中后是否会导致上方空白（太接近开始位置）
                if (centeredPos <= 0)
                {
                    // 让目标元素处于显示区域顶部，添加顶部padding距离
                    float topPadding = direction == Direction.Horizontal ? padding.left : padding.top;
                    offsetPos = targetPos - topPadding;
                    offsetPos = Mathf.Max(0, offsetPos); // 确保不小于0
                }
                // 检查居中后是否会导致下方空白（太接近结束位置）
                else if (centeredPos >= maxOffsetPos)
                {
                    // 让目标元素处于显示区域底部，需要减去底部padding
                    float bottomPadding = direction == Direction.Horizontal ? padding.right : padding.bottom;
                    float bottomOffset = direContentLen - direItemLen - bottomPadding;
                    offsetPos = targetPos + bottomOffset * offsetFactor;
                    // 确保不超出最大偏移
                    offsetPos = Mathf.Min(offsetPos, maxOffsetPos);
                }
                else
                {
                    // 可以正常居中
                    offsetPos = centeredPos;
                }
            }
            else
            {
                // 不居中的情况，智能选择顶部或底部
                float middlePos = maxOffsetPos / 2;

                if (targetPos <= middlePos)
                {
                    // 目标位置在前半部分，显示在顶部，添加顶部padding距离
                    float topPadding = direction == Direction.Horizontal ? padding.left : padding.top;
                    offsetPos = targetPos - topPadding;
                    offsetPos = Mathf.Max(0, offsetPos); // 确保不小于0
                }
                else
                {
                    // 目标位置在后半部分，显示在底部，需要减去底部padding
                    int offsetFactor = direction == Direction.Horizontal ? 1 : -1;
                    float bottomPadding = direction == Direction.Horizontal ? padding.right : padding.bottom;
                    float bottomOffset = direContentLen - direItemLen - bottomPadding;
                    offsetPos = targetPos + bottomOffset * offsetFactor;
                    // 确保不超出最大偏移
                    offsetPos = Mathf.Min(offsetPos, maxOffsetPos);
                }
            }

            // 确保偏移位置在有效范围内
            offsetPos = Mathf.Clamp(offsetPos, 0, maxOffsetPos);

            // 触发位置变化回调，刷新可见区域的items
            bool isJump = MathF.Abs(offsetPos - oldOffsetPos) > direContentLen && direContentLen >= direItemLen;
            Action action = isJump ? OnJumpChangeOffsetPos : OnChangeOffsetPos;
            action();

            UpdateScrollBarValue(false); // 使用false避免重复触发回调
            oldOffsetPos = offsetPos;
        }

        // TODO
        /// <summary>
        /// 跳转到顶部
        /// </summary>
        public void JumpToTop()
        {
            if (itemCount <= 0) return;

            oldOffsetPos = offsetPos;
            offsetPos = 0;

            // 手动设置items显示前几个元素，确保从索引0开始
            // 计算应该显示的起始索引（顶部总是从0开始）
            int targetStartIndex = 0;

            // 手动设置当前显示的items
            LinkedListNode<ItemNode> node = itemLinkedList.First;
            int currentIndex = targetStartIndex;
            while (node != null && currentIndex < itemCount && currentIndex < maxShowNum)
            {
                LinkedListNode<ItemNode> curNode = node;
                node = curNode.Next;

                ItemNode itemNode = curNode.Value;
                itemNode.index = currentIndex;
                itemNode.tf.name = "Item" + itemNode.index;
                RefreshItemPos(itemNode);
                OnVirtualItemLoad(itemNode.tf, itemNode.index);

                currentIndex++;
            }

            // 如果还有多余的node，回收它们
            while (node != null)
            {
                LinkedListNode<ItemNode> curNode = node;
                node = curNode.Next;

                ModifyItemIndex(curNode, IndexType.Before);
            }

            UpdateScrollBarValue(false); // 使用false避免重复触发回调
            oldOffsetPos = offsetPos;
        }

        // TODO
        /// <summary>
        /// 跳转到底部
        /// </summary>
        public void JumpToBottom()
        {
            if (itemCount <= 0) return;

            oldOffsetPos = offsetPos;

            // 使用maxOffsetPos确保内容显示在底部
            offsetPos = maxOffsetPos;

            // 手动设置items显示最后几个元素，而不是依赖OnJumpChangeOffsetPos的索引计算
            // 计算应该显示的起始索引
            int targetStartIndex = Mathf.Max(0, itemCount - maxShowNum);

            // 为了确保最后一个元素能显示，需要更好的起始索引计算
            // 优先确保最后一个元素(itemCount - 1)能显示，然后向前推算
            int lastElementIndex = itemCount - 1;
            int maxPossibleStart = Mathf.Max(0, lastElementIndex - maxShowNum + 1);

            // 在保证最后元素显示的前提下，尽量对齐到行的开始
            int alignedStart = (maxPossibleStart / crossDireCount) * crossDireCount;

            // 如果对齐后的起始位置仍能包含最后一个元素，则使用对齐的位置
            // 否则使用能确保最后元素显示的位置
            if (alignedStart + maxShowNum > lastElementIndex)
            {
                targetStartIndex = alignedStart;
            }
            else
            {
                targetStartIndex = maxPossibleStart;
            }

            // 手动设置当前显示的items
            LinkedListNode<ItemNode> node = itemLinkedList.First;
            int currentIndex = targetStartIndex;
            while (node != null && currentIndex < itemCount)
            {
                LinkedListNode<ItemNode> curNode = node;
                node = curNode.Next;

                ItemNode itemNode = curNode.Value;
                itemNode.index = currentIndex;
                itemNode.tf.name = "Item" + itemNode.index;
                RefreshItemPos(itemNode);
                OnVirtualItemLoad(itemNode.tf, itemNode.index);

                currentIndex++;
            }

            // 如果还有多余的node，回收它们
            while (node != null)
            {
                LinkedListNode<ItemNode> curNode = node;
                node = curNode.Next;

                ModifyItemIndex(curNode, IndexType.Before);
            }

            UpdateScrollBarValue(false); // 使用false避免重复触发回调
            oldOffsetPos = offsetPos;
        }

        public void Refresh()
        {
            LinkedListNode<ItemNode> node = itemLinkedList.First;
            while (node != null)
            {
                LinkedListNode<ItemNode> curNode = node;
                node = curNode.Next;

                ItemNode itemNode = curNode.Value;
                OnVirtualItemLoad(itemNode.tf, itemNode.index);
            }
        }

        public void RefreshItem(int index)
        {
            LinkedListNode<ItemNode> node = itemLinkedList.First;
            while (node != null)
            {
                LinkedListNode<ItemNode> curNode = node;
                node = curNode.Next;

                ItemNode itemNode = curNode.Value;
                if (itemNode.index == index)
                {
                    OnVirtualItemLoad(itemNode.tf, itemNode.index);
                    return;
                }
            }
        }

        /// <summary>
        /// 判断是否已经处于最顶部
        /// </summary>
        /// <returns>true表示已在顶部</returns>
        public bool IsAtTop()
        {
            // 容差值，用于处理浮点数精度问题
            float tolerance = 0.1f;
            return offsetPos <= tolerance;
        }

        /// <summary>
        /// 判断是否已经处于最底部
        /// </summary>
        /// <returns>true表示已在底部</returns>
        public bool IsAtBottom()
        {
            // 如果没有内容或最大偏移为0，认为已在底部
            if (itemCount <= 0 || maxOffsetPos <= 0)
                return true;

            // 容差值，用于处理浮点数精度问题
            float tolerance = 0.1f;
            return offsetPos >= maxOffsetPos - tolerance;
        }

        /// <summary>
        /// 获取当前滚动进度 (0.0 到 1.0)
        /// </summary>
        /// <returns>滚动进度，0为顶部，1为底部</returns>
        public float GetScrollProgress()
        {
            if (maxOffsetPos <= 0)
                return 0f;

            return Mathf.Clamp01(offsetPos / maxOffsetPos);
        }

        /// <summary>
        /// 判断指定索引的item是否在当前可见区域内
        /// </summary>
        /// <param name="index">item索引</param>
        /// <returns>true表示可见</returns>
        public bool IsItemVisible(int index)
        {
            foreach (var item in itemLinkedList)
            {
                if (item.index == index)
                    return true;
            }
            return false;
        }

        #endregion

        #region  Item相关

        //实例化Item
        private void InstanceItem(int index)
        {
            Transform tf = gOPool.Get().transform;
            tf.name = "Item" + index;
            tf.SetAsLastSibling();
            SetItemPivotAndAnchor(tf);

            ItemNode itemNode = new ItemNode();
            itemNode.index = index;
            itemNode.tf = tf;
            itemLinkedList.AddLast(itemNode);
            cacheChangeSizeList.Add(index);
            RefreshItemPos(itemNode);
            OnVirtualItemLoad(tf, index);
        }

        //设置锚点
        private void SetItemPivotAndAnchor(Transform tf)
        {
            RectTransform rt = tf.GetComponent<RectTransform>();
            rt.pivot = povitOrAnchorDict[direction][childAlign];
            rt.anchorMin = povitOrAnchorDict[direction][childAlign];
            rt.anchorMax = povitOrAnchorDict[direction][childAlign];
            if (sizeType == SizeType.Fixed)
                rt.sizeDelta = new Vector2(itemWidth, itemHeight);
            if (sizeType == SizeType.Auto)
                tf.TryAddComponent<RectTransformDimensionsChange>("");

            DrivenTransformProperties drivenProperties = DrivenTransformProperties.Anchors | DrivenTransformProperties.Pivot;
            drivenProperties |= DrivenTransformProperties.AnchoredPosition;
            tracker.Add(this, rt, drivenProperties);
        }

        //处理ItemSize变化
        public void HandleItemSizeChanged()
        {
            if (sizeType == SizeType.Fixed || cacheChangeSizeList.Count == 0)
                return;

            foreach (var index in cacheChangeSizeList)
            {
                Transform tf = GetItem(index);
                if (tf == null) continue;
                Vector2 size = tf.GetComponent<RectTransform>().sizeDelta;
                bool isExist = itemSizeDict.TryGetValue(index, out Vector2 cacheSize);
                float len = direction == Direction.Horizontal ? size.x : size.y;
                float cacheLen = direction == Direction.Horizontal ? cacheSize.x : cacheSize.y;
                itemSizeDict[index] = size;
                if (isExist && cacheLen != len)
                    direItemLenTree.Remove(cacheLen);
                if (!isExist || cacheLen != len)
                    direItemLenTree.Add(len);
            }
            cacheChangeSizeList.Clear();
            offsetType = OffsetType.Same;
            UpdateMaxOffsetPos();
            UpdateShowOffsetRange();
            OnChangeOffsetPos();
        }

        #endregion

        #region Item位置刷新
        //刷新Item位置
        private void RefreshItemPos(ItemNode itemNode)
        {
            RectTransform rt = itemNode.tf.GetComponent<RectTransform>();

            Vector2 itemOffsetPos = new Vector2(0, 0);
            Vector3 offsetDire = direction == Direction.Horizontal ? Vector3.left : Vector3.up;
            int rowOrColIndex = itemNode.index / crossDireCount;
            int offsetIndex = itemNode.index % crossDireCount;

            if (childAlign == Align.Center)
            {
                if (direction == Direction.Horizontal)
                {
                    float height = itemHeight * crossDireCount - itemHeight + spacing.y * (crossDireCount - 1);
                    itemOffsetPos = new Vector2(0, height / 2);
                }
                else if (direction == Direction.Vertical)
                {
                    float width = itemWidth * crossDireCount - itemWidth + spacing.x * (crossDireCount - 1);
                    itemOffsetPos = new Vector2(-width / 2, 0);
                }
            }

            if (direction == Direction.Horizontal)
            {
                float factorY = childAlign == Align.RightOrBottom ? 1 : -1;
                float paddingY = 0;
                paddingY = childAlign == Align.LeftOrTop ? -padding.top : paddingY;
                paddingY = childAlign == Align.RightOrBottom ? padding.bottom : paddingY;

                float beforeDireItemLen = GetBeforeDireItemLen(itemNode.index);
                float offsetX = beforeDireItemLen + spacing.x * rowOrColIndex + padding.left;
                float offsetY = offsetIndex * (itemHeight + spacing.y) * factorY + paddingY;
                itemOffsetPos += new Vector2(offsetX, offsetY);
            }
            else if (direction == Direction.Vertical)
            {
                float factorX = childAlign == Align.RightOrBottom ? -1 : 1;
                float paddingX = 0;
                paddingX = childAlign == Align.LeftOrTop ? padding.left : paddingX;
                paddingX = childAlign == Align.RightOrBottom ? -padding.right : paddingX;

                float beforeDireItemLen = GetBeforeDireItemLen(itemNode.index);
                float offsetX = offsetIndex * (itemWidth + spacing.x) * factorX + paddingX;
                float offsetY = -beforeDireItemLen - spacing.y * rowOrColIndex - padding.top;
                itemOffsetPos += new Vector2(offsetX, offsetY);
            }

            rt.anchoredPosition = (Vector3)itemOffsetPos + offsetDire * this.offsetPos;
        }

        #endregion

        #region Item索引修改

        //改变Item索引
        private void ModifyItemIndex(LinkedListNode<ItemNode> node, IndexType indexType)
        {
            ItemNode itemNode = node.Value;
            if (indexType == IndexType.None)
                return;

            bool hasLastNode = itemLinkedList.Last.Value.index == itemCount - 1;
            if (indexType == IndexType.After && hasLastNode)
                return;

            bool hasFirstNode = itemLinkedList.First.Value.index == 0;
            if (indexType == IndexType.Before && hasFirstNode)
            {
                if (node.Previous != null && node.Previous.Value.index == itemNode.index - 1)
                    return;

                int closestIndex = 0;
                foreach (var item in itemLinkedList)
                {
                    if (item.index > closestIndex && item.index < itemNode.index)
                        closestIndex = item.index;
                }

                itemNode.index = closestIndex + 1;
                itemNode.tf.SetAsLastSibling();
            }
            else if (indexType == IndexType.Before && !hasFirstNode)
            {
                itemLinkedList.Remove(node);
                itemNode.index = itemLinkedList.First.Value.index - 1;
                itemLinkedList.AddFirst(node);
                itemNode.tf.SetAsFirstSibling();
            }
            else if (indexType == IndexType.After)
            {
                itemLinkedList.Remove(node);
                itemNode.index = itemLinkedList.Last.Value.index + 1;
                itemLinkedList.AddLast(node);
                itemNode.tf.SetAsLastSibling();
            }

            itemNode.tf.name = "Item" + itemNode.index;
            RefreshItemPos(itemNode);
            OnVirtualItemLoad(itemNode.tf, itemNode.index);
        }

        #endregion

        #region Item数量相关

        private void UpdateItemCount(ItemCountMode operation, int num)
        {
            switch (operation)
            {
                case ItemCountMode.Add:
                    itemCount += num;
                    break;
                case ItemCountMode.Reduce:
                    itemCount -= num;
                    break;
                case ItemCountMode.Total:
                    itemCount = num;
                    break;
            }
            itemCount = Math.Max(itemCount, 0);
            UpdateMaxOffsetPos();
        }

        private void AddItemNum(int num)
        {
            UpdateItemCount(ItemCountMode.Add, num);
            if (itemLinkedList.Count < maxShowNum)
            {
                int startIndex = itemLinkedList.First == null ? 0 : itemLinkedList.Last.Value.index + 1;
                for (int i = startIndex; i < itemCount; i++)
                {
                    if (i >= maxShowNum) break;
                    InstanceItem(i);
                }
            }
            UpdateScrollBarValue(true);
        }

        private void ReduceItemNum(int num)
        {
            UpdateItemCount(ItemCountMode.Reduce, num);
            if (itemCount < maxShowNum)
            {
                int index = 0;
                LinkedListNode<ItemNode> node = itemLinkedList.First;
                while (node != null)
                {
                    LinkedListNode<ItemNode> curNode = node;
                    node = curNode.Next;

                    if (index >= itemCount)
                    {
                        itemLinkedList.Remove(curNode);
                        gOPool.Recycle(curNode.Value.tf.gameObject);
                    }
                    else if (curNode.Value.index >= itemCount)
                    {
                        ModifyItemIndex(curNode, IndexType.Before);
                    }
                    index++;
                }
            }
            UpdateScrollBarValue(true);
        }

        #endregion

        #region Item回调(加载、大小改变)
        //ItemLoad回调函数
        public void OnVirtualItemLoad(Transform tf, int index)
        {
            if (sizeType == SizeType.Auto)
            {
                RectTransformDimensionsChange compoent = tf.GetComponent<RectTransformDimensionsChange>();
                bool isExist = itemSizeDict.TryGetValue(index, out Vector2 cacheSize);
                cacheSize = isExist ? cacheSize : new Vector2(defaultDireItemLen, defaultDireItemLen);
                tf.GetComponent<RectTransform>().sizeDelta = cacheSize;

                compoent.index = index;
                compoent.action = OnItemSizeChanged;
            }

            OnItemLoad?.Invoke(tf, index);
        }

        //ItemSize改变回调函数
        public void OnItemSizeChanged(int index)
        {
            Transform tf = GetItem(index);
            if (tf == null) return;

            bool isExist = itemSizeDict.TryGetValue(index, out Vector2 cacheSize);
            Vector2 size = tf.GetComponent<RectTransform>().sizeDelta;
            if (isExist && size == cacheSize) return;

            cacheChangeSizeList.Add(index);
        }

        #endregion

        #region 功能函数

        //初始化变量
        private void Init()
        {
            offsetPos = 0;
            oldOffsetPos = 0;
            itemCount = 0;
            itemLinkedList.Clear();
            itemSizeDict.Clear();
            gOPool.RecycleAll();
            scrollbar.SetValueWithoutNotify(0);
        }

        //计算ItemSize
        private void CalculateItemSize()
        {
            RectTransform rt = itemPrefab.GetComponent<RectTransform>();
            RectTransform parentRt = rt.parent.GetComponent<RectTransform>();
            bool isPrefab = rt.gameObject.scene.name == null;   //预制体
            bool isFixedX = isPrefab || rt.anchorMin.x == rt.anchorMax.x;
            bool isFixedY = isPrefab || rt.anchorMin.y == rt.anchorMax.y;

            itemWidth = isFixedX ? rt.rect.width : parentRt.rect.width * (rt.anchorMax.x - rt.anchorMin.x);
            itemHeight = isFixedY ? rt.rect.height : parentRt.rect.height * (rt.anchorMax.y - rt.anchorMin.y);

            float fixedDireItemLen = direction == Direction.Horizontal ? itemWidth : itemHeight;
            direItemLen = sizeType == SizeType.Fixed ? fixedDireItemLen : defaultDireItemLen;
        }

        // TODO
        //获取索引之前的布局方向的距离
        private float GetBeforeDireItemLen(int index)
        {
            if (sizeType == SizeType.Fixed)
            {
                int rowOrColIndex = index / crossDireCount;
                if (index == itemCount)     //比最后一个都大，那就是取所有DireItemLen的和
                {
                    rowOrColIndex = (index + crossDireCount - 1) / crossDireCount;
                }
                return rowOrColIndex * direItemLen;
            }

            // 对于Auto尺寸，需要按行来计算距离，而不是按每个元素
            float beforeItemLen = 0;
            int targetRow = index / crossDireCount;

            for (int row = 0; row < targetRow; row++)
            {
                // 找到该行中最高/最宽的元素
                float maxRowSize = 0;
                for (int col = 0; col < crossDireCount; col++)
                {
                    int itemIndex = row * crossDireCount + col;
                    if (itemIndex >= itemCount) break;

                    bool hasSize = itemSizeDict.TryGetValue(itemIndex, out Vector2 size);
                    size = hasSize ? size : new Vector2(defaultDireItemLen, defaultDireItemLen);
                    float itemSize = direction == Direction.Horizontal ? size.x : size.y;
                    maxRowSize = Mathf.Max(maxRowSize, itemSize);
                }
                beforeItemLen += maxRowSize;
            }

            return beforeItemLen;
        }

        public void UpdateMaxOffsetPos()
        {
            rowOrColCount = (itemCount + crossDireCount - 1) / crossDireCount;
            float spacingLen = rowOrColCount == 0 ? 0 : direSpacingLen * (rowOrColCount - 1);
            float paddingLen = direction == Direction.Horizontal ? padding.left + padding.right : padding.top + padding.bottom;
            float beforeItemLen = GetBeforeDireItemLen(itemCount);
            float maxLen = beforeItemLen + spacingLen + paddingLen;
            maxOffsetPos = Mathf.Max(0, maxLen - direContentLen);
            scrollbar.size = maxLen == 0 ? 1 : direContentLen / maxLen;
        }

        private void UpdateShowOffsetRange()
        {
            float maxDireItemLen = sizeType == SizeType.Fixed ? direItemLen : direItemLenTree.maxNode.value;
            float minDireItemLen = sizeType == SizeType.Fixed ? direItemLen : direItemLenTree.minNode.value;
            float minOffset = direction == Direction.Horizontal ? -maxDireItemLen : -direContentLen;
            float maxOffset = direction == Direction.Horizontal ? direContentLen : maxDireItemLen;
            showOffsetRange = new Vector2(minOffset, maxOffset);

            maxShowNum = ((int)(direContentLen / minDireItemLen) + 2) * crossDireCount;
        }

        private void UpdateScrollBarValue(bool isNotify)
        {
            offsetPos = Mathf.Clamp(offsetPos, 0, maxOffsetPos);
            float value = maxOffsetPos == 0 ? 0 : offsetPos / maxOffsetPos;
            if (!isNotify)
            {
                scrollbar.SetValueWithoutNotify(value);
                return;
            }

            bool isDifferent = value != scrollbar.value;    // 避免相同值，不触发onValueChanged
            float smallFloat = value == 0 ? -0.0001f : 0.0001f;
            value = isDifferent ? value : value + smallFloat;
            scrollbar.value = value;
        }

        #endregion

        #region 边界触发器
        public void SetScrollTrigger(Action onScrollToStart, Action onScrollToEnd)
        {
            OnScrollToStart = onScrollToStart;
            OnScrollToEnd = onScrollToEnd;
            ResetTriggerState();
        }

        public void ResetTriggerState()
        {
            isTrigger = false;
        }

        public void CheckIfScrollTrigger()
        {
            if (isTrigger || maxOffsetPos <= 0)
                return;
            ScrollEdge scrollEdge = offsetPos < 0 ? ScrollEdge.Start : ScrollEdge.None;
            scrollEdge = offsetPos > maxOffsetPos ? ScrollEdge.End : scrollEdge;
            if (scrollEdge == ScrollEdge.None)
                return;

            Action action = scrollEdge == ScrollEdge.Start ? OnScrollToStart : OnScrollToEnd;
            action?.Invoke();
            isTrigger = true;
        }

        #endregion

        #region 滑动位置改变回调
        //滑动位置改变回调
        private void OnChangeOffsetPos()
        {
            bool checkEnd = offsetType != OffsetType.End;
            bool checkStart = offsetType != OffsetType.Start;
            bool isHorizontal = direction == Direction.Horizontal;
            LinkedListNode<ItemNode> node = itemLinkedList.First;
            while (node != null)
            {
                LinkedListNode<ItemNode> curNode = node;
                node = curNode.Next;

                RectTransform rt = curNode.Value.tf.GetComponent<RectTransform>();
                RefreshItemPos(curNode.Value);

                float pos = isHorizontal ? rt.anchoredPosition.x : rt.anchoredPosition.y;
                bool isOver = curNode.Value.index >= itemCount;
                IndexType posType = IndexType.None;
                if (isHorizontal)
                {
                    if (checkEnd && pos >= showOffsetRange.y)
                        posType = IndexType.Before;
                    else if (checkStart && pos <= showOffsetRange.x)
                        posType = IndexType.After;
                }
                else
                {
                    if (checkEnd && pos <= showOffsetRange.x)
                        posType = IndexType.Before;
                    else if (checkStart && pos >= showOffsetRange.y)
                        posType = IndexType.After;
                }
                posType = isOver ? IndexType.Before : posType;
                ModifyItemIndex(curNode, posType);
            }
        }

        // TODO
        //滑动位置大幅改变回调
        private void OnJumpChangeOffsetPos()
        {
            // 计算当前偏移位置对应的行索引
            int rowIndex = (int)(offsetPos / (direItemLen + direSpacingLen));
            // 计算起始元素索引（需要考虑多列布局）
            int startIndex = rowIndex * crossDireCount;

            LinkedListNode<ItemNode> node = itemLinkedList.First;
            int currentIndex = startIndex;

            while (node != null)
            {
                LinkedListNode<ItemNode> curNode = node;
                node = curNode.Next;

                ItemNode itemNode = curNode.Value;
                itemNode.index = currentIndex;

                if (itemNode.index >= itemCount)    //超过上限，插入到最前
                {
                    ModifyItemIndex(curNode, IndexType.Before);
                    return;
                }

                itemNode.tf.name = "Item" + itemNode.index;
                RefreshItemPos(itemNode);
                OnVirtualItemLoad(itemNode.tf, itemNode.index);

                currentIndex++;
            }
        }

        #endregion

        #region 系统事件回调函数
        private void OnScrollbarValueChanged(float value)
        {
            offsetPos = maxOffsetPos * value;
            offsetType = offsetPos > oldOffsetPos ? OffsetType.End : offsetPos < oldOffsetPos ? OffsetType.Start : OffsetType.Same;
            bool isJump = MathF.Abs(offsetPos - oldOffsetPos) > direContentLen && direContentLen >= direItemLen;
            Action action = isJump ? OnJumpChangeOffsetPos : OnChangeOffsetPos;
            action();

            oldOffsetPos = offsetPos;
        }
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            isDraging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDraging) return;
            float delta = direction == Direction.Horizontal ? -eventData.delta.x : eventData.delta.y;
            offsetPos += delta;
            CheckIfScrollTrigger();
            UpdateScrollBarValue(true);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDraging) return;
            isDraging = false;
        }

        public void OnScroll(PointerEventData eventData)
        {
            offsetPos -= eventData.scrollDelta.y;
            CheckIfScrollTrigger();
            UpdateScrollBarValue(true);
        }

        #endregion
    }
}

