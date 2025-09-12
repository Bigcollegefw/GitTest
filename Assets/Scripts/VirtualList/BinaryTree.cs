using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TKFramework
{
	public class BinaryTree<T> : IEnumerable<T>
	{
		public BinaryTreeNode<T> root;
		public int Count { get; protected set; }
		public BinaryTree() { }
		public BinaryTree(IEnumerable<T> collection)
		{
			foreach (T value in collection)
			{
				Add(value);
			}
		}

		/// <summary>
		/// 插入，层先法查找，第一个左子树或右子树为null的节点插入
		/// </summary>
		/// <param name="value"></param>
		public virtual void Add(T value)
		{
			if (root == null)
			{
				root = new BinaryTreeNode<T>(value);
				Count = 1;
				return;
			}

			root = Insert(root, value); //经过旋转，根节点可能发生改变
			Count++;
		}

		protected virtual BinaryTreeNode<T> Insert(BinaryTreeNode<T> node, T value)
		{
			Queue<BinaryTreeNode<T>> queue = new Queue<BinaryTreeNode<T>>();
			queue.Enqueue(node);

			while (queue.Count > 0)
			{
				BinaryTreeNode<T> curNode = queue.Dequeue();
				bool hasEmptyChild = curNode.left == null || curNode.right == null;
				if (curNode.left == null)
					curNode.left = new BinaryTreeNode<T>(curNode, value);
				else
					curNode.right = new BinaryTreeNode<T>(curNode, value);

				if (hasEmptyChild)
				{
					UpdateHeightRecursive(curNode);
					break;
				}

				if (curNode.left != null) queue.Enqueue(node.left);
				if (curNode.right != null) queue.Enqueue(node.right);
				queue.Enqueue(node.left);
			}
			return node;
		}

		public virtual void Remove(T value)
		{
			if (root == null)
				return;

			root = Remove(root, value);
		}

		/// <summary>
		/// 删除节点
		/// 找到对应的节点
		/// 若非叶子节点，将节点子节点最下面的叶子结点替换
		/// 若是叶子节点，直接删除
		/// </summary>
		/// <param name="value"></param>
		protected virtual BinaryTreeNode<T> Remove(BinaryTreeNode<T> node, T value)
		{
			Queue<BinaryTreeNode<T>> queue = new Queue<BinaryTreeNode<T>>();
			queue.Enqueue(node);
			while (queue.Count > 0)
			{
				BinaryTreeNode<T> curNode = queue.Dequeue();
				if (curNode.value.Equals(value))
				{
					BinaryTreeNode<T> removeNode = curNode;
					while (removeNode.left != null || removeNode.right != null)
					{
						removeNode = removeNode.left == null ? removeNode.right : removeNode.left;
					}
					node.value = removeNode.value;

					Count--;
					if (removeNode.parent == null) return null;     //父节点为空，那就是根节点了
					if (removeNode.parent.left == removeNode) removeNode.parent.left = null;
					if (removeNode.parent.right == removeNode) removeNode.parent.right = null;
					return node;
				}

				if (curNode.left != null) queue.Enqueue(node.left);
				if (curNode.right != null) queue.Enqueue(node.right);
				queue.Enqueue(node.left);
			}

			return node;
		}

		public BinaryTreeNode<T> Find(T value)
		{
			if (root == null)
				return null;

			return Find(root, value);
		}

		// 按层先法，即广度优先查找
		protected virtual BinaryTreeNode<T> Find(BinaryTreeNode<T> root, T value)
		{
			Queue<BinaryTreeNode<T>> queue = new Queue<BinaryTreeNode<T>>();
			queue.Enqueue(root);
			BinaryTreeNode<T> node;
			while (queue.Count > 0)
			{
				node = queue.Dequeue();
				if (node.value.Equals(value))
					return node;

				if (node.left != null) queue.Enqueue(node.left);
				if (node.right != null) queue.Enqueue(node.right);
			}

			return null;
		}

		public bool Contains(T value)
		{
			BinaryTreeNode<T> node = Find(root, value);
			return node != null;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return InOrder().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>
		/// 前序遍历
		/// </summary>
		/// <returns></returns>
		public List<T> PreOrder()
		{
			List<T> list = new List<T>();
			PreOrderRecursive(root, list);
			return list;
		}

		/// <summary>
		/// 中序遍历
		/// </summary>
		/// <returns></returns>
		public List<T> InOrder()
		{
			List<T> list = new List<T>();
			InorderRecursive(root, list);
			return list;
		}

		/// <summary>
		/// 后序遍历
		/// </summary>
		/// <returns></returns>
		public List<T> PostOrder()
		{
			List<T> list = new List<T>();
			PostorderRecursive(root, list);
			return list;
		}

		/// <summary>
		/// 层先法遍历
		/// </summary>
		/// <returns></returns>
		public List<T> LevOrder()
		{
			List<T> list = new List<T>();
			if (root == null)
				return list;

			Queue<BinaryTreeNode<T>> queue = new Queue<BinaryTreeNode<T>>();
			queue.Enqueue(root);
			LevOrderRecursive(queue, list);

			return list;
		}

		/// <summary>
		/// 获取平衡因子，左子树高度减右子数的高度
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public int BalanceFactor(BinaryTreeNode<T> node)
		{
			return Height(node.left) - Height(node.right);
		}

		/// <summary>
		/// 获取节点的高度，null的话为0，默认为1
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public int Height(BinaryTreeNode<T> node)
		{
			if (node == null) return 0;
			return node.height;
		}

		/// <summary>
		/// 根据子节点更新树的高度
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public int UpdateHeight(BinaryTreeNode<T> node)
		{
			node.height = Mathf.Max(Height(node.left), Height(node.right)) + 1;
			return node.height;
		}

		//递归更新树的高度、
		public void UpdateHeightRecursive(BinaryTreeNode<T> node)
		{
			while (node != null)
			{
				UpdateHeight(node);
				node = node.parent;
			}
		}



		//前序递归遍历
		private void PreOrderRecursive(BinaryTreeNode<T> node, List<T> list)
		{
			if (node == null)
				return;

			list.Add(node.value);
			PreOrderRecursive(node.left, list);
			PreOrderRecursive(node.right, list);
		}

		//中序递归遍历
		private void InorderRecursive(BinaryTreeNode<T> node, List<T> list)
		{
			if (node == null)
				return;

			InorderRecursive(node.left, list);
			list.Add(node.value);
			InorderRecursive(node.right, list);
		}

		//后序递归遍历
		private void PostorderRecursive(BinaryTreeNode<T> node, List<T> list)
		{
			if (node == null)
				return;

			PostorderRecursive(node.left, list);
			PostorderRecursive(node.right, list);
			list.Add(node.value);
		}

		//层序递归遍历
		private void LevOrderRecursive(Queue<BinaryTreeNode<T>> queue, List<T> list)
		{
			BinaryTreeNode<T> root = queue.Dequeue();
			list.Add(root.value);

			if (root.left != null)
				queue.Enqueue(root.left);
			if (root.right != null)
				queue.Enqueue(root.right);

			if (queue.Count > 0)
				LevOrderRecursive(queue, list);
		}


	}

}
