using System.Collections.Generic;
using UnityEngine;
using System;


namespace TKFramework
{
	/// <summary>
	/// 二叉搜索树，相比于二叉树拥有左节点值小于根节点，根节点值小于右节点的特点
	/// 可以进行更快速的查找，插入，删除操作
	/// 但是最坏的情况下和二叉树效果一样
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class BST<T> : BinaryTree<T> where T : IComparable<T>
	{
		public enum ChangeType { Add, Remove }
		public BinaryTreeNode<T> minNode;
		public BinaryTreeNode<T> maxNode;
		public BST() { }
		public BST(IEnumerable<T> collection)
		{
			foreach (T value in collection)
				Add(value);
		}

		public override void Add(T value)
		{
			base.Add(value);
			UpdateMinMaxNode(ChangeType.Add, value);
		}

		/// <summary>
		/// 插入值，新增节点
		/// 保证左节点值小于根节点，根节点值小于右节点
		/// </summary>
		/// <param name="value"></param>
		protected override BinaryTreeNode<T> Insert(BinaryTreeNode<T> node, T value)
		{
			if (node.value.CompareTo(value) == 1)
			{
				if (node.left == null)
					node.left = new BinaryTreeNode<T>(node, value);
				else
					node.left = Insert(node.left, value);   //插入后可能改变节点
			}
			else
			{
				if (node.right == null)
					node.right = new BinaryTreeNode<T>(node, value);
				else
					node.right = Insert(node.right, value); //插入后可能改变节点
			}

			UpdateHeight(node);
			return node;
		}


		public override void Remove(T value)
		{
			base.Remove(value);
			UpdateMinMaxNode(ChangeType.Remove, value);
		}

		/// <summary>
		/// 删除节点
		/// 若节点是叶子节点，直接删除
		/// 若节点只有左子树，或右子树，返回对应的子树
		/// 否则，从节点的右子树节点开始获取值最小的节点与当前节点值交换
		/// 更新节点高度
		/// </summary>
		/// <param name="value"></param>
		protected override BinaryTreeNode<T> Remove(BinaryTreeNode<T> node, T value)
		{
			if (node.value.CompareTo(value) == 1)
			{
				if (node.left == null) return node;//没找到删除元素，返回就行
				node.left = Remove(node.left, value);
			}
			else if (node.value.CompareTo(value) == -1)
			{
				if (node.right == null) return node;//没找到删除元素，返回就行
				node.right = Remove(node.right, value);
			}
			else
			{
				if (node.left == null && node.right == null)
				{
					Count--;
					return null;
				}

				BinaryTreeNode<T> removeNode = node.left == null ? FindMin(node.right) : FindMax(node.left);
				node.value = removeNode.value;
				removeNode.value = value;
				if (node.left == null)
					node.right = Remove(node.right, value);
				else
					node.left = Remove(node.left, value);
			}

			UpdateHeight(node);
			return node;
		}

		/// <summary>
		/// 平衡二叉树查找：二分查找
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		protected override BinaryTreeNode<T> Find(BinaryTreeNode<T> node, T value)
		{
			if (node == null)
				return null;

			if (node.value.CompareTo(value) == 0)
				return node;
			else if (node.value.CompareTo(value) == 1)
				return Find(node.left, value);
			else
				return Find(node.right, value);
		}

		/// <summary>
		/// 查找从节点开始值最小的节点
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public BinaryTreeNode<T> FindMin(BinaryTreeNode<T> node)
		{
			BinaryTreeNode<T> minNode = node;
			while (minNode != null && minNode.left != null)
			{
				minNode = minNode.left;
			}
			return minNode;
		}

		/// <summary>
		/// 查找从节点开始值最大的节点
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public BinaryTreeNode<T> FindMax(BinaryTreeNode<T> node)
		{
			BinaryTreeNode<T> maxNode = node;
			while (maxNode != null && maxNode.right != null)
			{
				maxNode = maxNode.right;
			}
			return maxNode;
		}

		public void UpdateMinMaxNode(ChangeType changeType, T value)
		{
			bool isUpdateMin = minNode == null
				|| (changeType == ChangeType.Remove && minNode.value.CompareTo(value) == 0)
				|| (changeType == ChangeType.Add && minNode.value.CompareTo(value) == 1);
			bool isUpdateMax = maxNode == null
				|| (changeType == ChangeType.Remove && maxNode.value.CompareTo(value) == 0)
				|| (changeType == ChangeType.Add && maxNode.value.CompareTo(value) == -1);

			minNode = isUpdateMin ? FindMin(root) : minNode;
			maxNode = isUpdateMax ? FindMax(root) : maxNode;
		}
	}

}
