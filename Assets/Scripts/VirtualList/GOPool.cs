using System.Collections.Generic;
using UnityEngine;

namespace TKFramework
{
	public class GOPool
	{
		public List<GameObject> cachePool = new List<GameObject>();
		public HashSet<GameObject> usePool = new HashSet<GameObject>();
		public int initSize = 0;
		public GameObject prefab;
		public Transform parent;

		public GOPool(GameObject prefab, Transform parent, int initSize = 0)
		{
			this.prefab = prefab;
			this.parent = parent;
			this.initSize = initSize;

			for (int i = 0; i < initSize; i++)
			{
				GameObject item = GameObject.Instantiate(prefab);
				item.transform.SetParent(parent.transform, false);
				item.SetActive(false);
				cachePool.Add(item);
			}

			if (prefab.scene.name != null)  //不是预制体
			{
				prefab.SetActive(false);
				cachePool.Add(prefab);
			}
		}


		public GameObject Get()
		{
			GameObject item;
			if (cachePool.Count == 0)
			{
				item = GameObject.Instantiate(prefab);
			}
			else
			{
				item = cachePool[0];
				cachePool.RemoveAt(0);
			}

			usePool.Add(item);
			item.transform.SetParent(parent, false);
			item.SetActive(true);
			return item;
		}


		public GameObject Get(Transform parent, bool worldPositionStays)
		{
			GameObject item = Get();
			item.transform.SetParent(parent, worldPositionStays);
			return item;
		}

		public void Recycle(GameObject item)
		{
			if (item == null) return;
			usePool.Remove(item);
			item.SetActive(false);
			item.transform.SetParent(parent, false);
			if (cachePool.Contains(item) == false)
				cachePool.Add(item);
		}

		public void RecycleAll()
		{
			foreach (var item in usePool)
			{
				item.SetActive(false);
				item.transform.SetParent(parent.transform, false);
				if (cachePool.Contains(item) == false)
					cachePool.Add(item);
			}
			usePool.Clear();
		}
	}
}
