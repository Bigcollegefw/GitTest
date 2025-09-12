using System.Collections.Generic;
using UnityEngine;
using System;


namespace TKFramework
{
	/// <summary>
	/// 平衡二叉树
	/// 相比于搜索二叉树，不存在好坏情况，因为一直维护左右高度相差不超过1	特性
	/// 缺点：插入、删除时频繁的调整结构，
	/// 优点：当结构是一次性构建，多次查找时效果最高
	/// 红黑树查找删除的最优解，但是代码内容很对，各种旋转调整。
	/// 就略过
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class AVLT<T> : BST<T> where T : IComparable<T>
	{
		public int BalanceThreshold { get; set; } = 5;
		public AVLT() { }
		public AVLT(T value)
		{
			Add(value);
		}
		public AVLT(IEnumerable<T> collection)
		{
			foreach (var item in collection)
			{
				Add(item);
			}
		}

		/// <summary>
		/// 插入值，新增节点
		/// 保证左节点值小于根节点，根节点值小于右节点
		/// 维持左右高度相差不超过1						
		/// 假设平衡破坏点是A，A的子树是B		LL
		/// LL___A右旋   RR___A左旋				A	   A右旋				B
		/// LR___B左旋__A右旋				B			-->				C		A
		/// RL___B右旋__A左旋			C		D					D
		/// </summary>
		/// <param name="value"></param>
		protected override BinaryTreeNode<T> Insert(BinaryTreeNode<T> node, T value)
		{
			base.Insert(node, value);
			node = BalanceNode(node);
			return node;
		}

		protected override BinaryTreeNode<T> Remove(BinaryTreeNode<T> node, T value)
		{
			node = base.Remove(node, value);
			if (node != null)
				node = BalanceNode(node);
			return node;
		}

		private BinaryTreeNode<T> BalanceNode(BinaryTreeNode<T> node)
		{
			int balanceFactor = Height(node.left) - Height(node.right);
			if (balanceFactor > BalanceThreshold)
			{
				if (BalanceFactor(node.left) >= 0)
				{
					return RightRotate(node);
				}
				else
				{
					node.left = LeftRotate(node.left);
					return RightRotate(node);
				}
			}

			if (balanceFactor < -BalanceThreshold)
			{
				if (BalanceFactor(node.right) <= 0)
				{
					return LeftRotate(node);
				}
				else
				{
					node.right = RightRotate(node.right);
					return LeftRotate(node);
				}
			}

			return node;
		}

		private BinaryTreeNode<T> RightRotate(BinaryTreeNode<T> node)
		{
			BinaryTreeNode<T> newRoot = node.left;
			newRoot.parent = node.parent;

			node.left = newRoot.right;
			if (newRoot.right != null)
				newRoot.right.parent = node;

			newRoot.right = node;
			node.parent = newRoot;

			UpdateHeight(node);
			UpdateHeight(newRoot);

			return newRoot;
		}

		private BinaryTreeNode<T> LeftRotate(BinaryTreeNode<T> node)
		{
			BinaryTreeNode<T> newRoot = node.right;
			newRoot.parent = node.parent;

			node.right = newRoot.left;
			if (newRoot.left != null)
				newRoot.left.parent = node;

			newRoot.left = node;
			node.parent = newRoot;

			UpdateHeight(node);
			UpdateHeight(newRoot);
			return newRoot;
		}

	}

}
