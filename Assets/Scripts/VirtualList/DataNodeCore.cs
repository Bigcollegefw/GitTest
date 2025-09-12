using System;
using UnityEngine;
using System.Reflection;
using UnityEditor;
namespace TKFramework
{
	/// <summary>
	/// 二叉树节点
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class BinaryTreeNode<T>
	{
		public T value;
		public int height;
		public BinaryTreeNode<T> left;
		public BinaryTreeNode<T> right;
		public BinaryTreeNode<T> parent;


		public BinaryTreeNode(T value, int height = 1)
		{
			this.value = value;
			this.height = height;
		}

		public BinaryTreeNode(BinaryTreeNode<T> parent, T value, int height = 1)
		{
			this.parent = parent;
			this.value = value;
			this.height = height;
		}
	}


	/// <summary>
	/// 
	/// </summary>
	public class TimerNode : IComparable
	{

		public float endTime;
		public Action<TimerNode> endCallBack;

		public float interval;
		public float nextIntervalTime;
		public int intervalCallNum;
		public Action<TimerNode> intervalCallBack;

		public float originLeftTime;
		public float LeftTime => endTime - Time.time;

		public TimerNode(float leftTime, Action<TimerNode> endCallBack)
		{
			originLeftTime = leftTime;
			this.endCallBack = endCallBack;
			UpdateEndTime();
		}

		public TimerNode(float leftTime, float interval, Action<TimerNode> intervalCallBack)
		{
			originLeftTime = leftTime;
			this.interval = interval;
			this.intervalCallBack = intervalCallBack;
			this.endCallBack = intervalCallBack;
			UpdateIntervalEndTime();
			UpdateEndTime();
		}

		public TimerNode(float leftTime, float interval, Action<TimerNode> intervalCallBack, Action<TimerNode> endCallBack)
		{
			originLeftTime = leftTime;
			this.interval = interval;
			this.intervalCallBack = intervalCallBack;
			this.endCallBack = endCallBack;
			UpdateEndTime();
			UpdateIntervalEndTime();
		}

		public void UpdateEndTime()
		{
			this.endTime = Time.time + originLeftTime;
		}

		public void UpdateIntervalEndTime()
		{

			if (nextIntervalTime == 0)
			{
				nextIntervalTime = Time.time + interval;
			}
			else
			{
				nextIntervalTime += interval;
			}
		}

		private float GetNextCallTime()
		{
			if (interval == 0)
				return endTime;
			return Mathf.Min(nextIntervalTime, endTime);
		}

		public int CompareTo(object obj)
		{
			TimerNode other = (TimerNode)obj;
			float nextTimer = GetNextCallTime();
			float otherNextTimer = other.GetNextCallTime();
			return nextTimer.CompareTo(otherNextTimer);
		}
	}
}
