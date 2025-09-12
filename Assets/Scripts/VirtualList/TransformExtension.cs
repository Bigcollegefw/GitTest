using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TKFramework
{
	public static class TransformExtension
	{
		public static string GetRootPath(this Transform transform)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(transform.name);
			Transform parent = transform.parent;
			while (parent != null)
			{
				sb.Insert(0, parent.name + "/");
				parent = parent.parent;
			}
			return sb.ToString();
		}

		public static T GetChildComponent<T>(this Transform transform, string path)
		{
			Transform childTf = transform.Find(path);
			if (childTf == null)
			{
				return default(T);
			}

			T component = childTf.GetComponent<T>();
			if (component == null)
			{
				Debug.Log(transform.GetRootPath() + "/" + path + "对象不存在组件" + nameof(T));
				return default(T);
			}

			return component;
		}

		public static T TryAddComponent<T>(this Transform transform, string path) where T : Component
		{
			Transform childTf = transform.Find(path);
			if (childTf == null)
			{
				return default(T);
			}

			T component = childTf.GetComponent<T>();
			if (component != null)
			{
				return component;
			}
			else
			{
				component = childTf.gameObject.AddComponent<T>();
				return component;
			}
		}



		/// <summary>
		/// 适用于数值，或者拼接数值,不可用于文本！！！！！
		///	text之所以用string，是因为object时，DoTween会报错
		/// </summary>
		/// <param name="transform"></param>
		/// <param name="path"></param>
		/// <param name="text"></param>
		public static void SetTMPUGUI(this Transform transform, string path, string text)
		{
			TextMeshProUGUI textMeshProUGUI = transform.GetChildComponent<TextMeshProUGUI>(path);
			if (textMeshProUGUI != null)
			{
				textMeshProUGUI.text = text;
			}
		}

		/// <summary>
		/// 适用于数值，或者拼接数值,不可用于文本！！！！！
		/// </summary>
		/// <param name="transform"></param>
		/// <param name="path"></param>
		/// <param name="text"></param>
		public static void SetTMPUGUI(this Transform transform, string path, int num)
		{
			TextMeshProUGUI textMeshProUGUI = transform.GetChildComponent<TextMeshProUGUI>(path);
			if (textMeshProUGUI != null)
			{
				textMeshProUGUI.text = num.ToString();
			}
		}

		/// <summary>
		/// 适用于数值，或者拼接数值,不可用于文本！！！！！
		/// </summary>
		/// <param name="transform"></param>
		/// <param name="path"></param>
		/// <param name="text"></param>
		public static void SetTMPUGUI(this Transform transform, string path, float num)
		{
			TextMeshProUGUI textMeshProUGUI = transform.GetChildComponent<TextMeshProUGUI>(path);
			if (textMeshProUGUI != null)
			{
				textMeshProUGUI.text = num.ToString();
			}
		}




		public static void OnChangeText(this Transform transform, string path, Action<string> action)
		{
			TMP_InputField field = transform.GetChildComponent<TMP_InputField>(path);
			if (field == null)
			{
				Debug.Log(transform.GetRootPath() + "/" + path + "对象不存在组件" + nameof(TMP_InputField));
				return;
			}
			field.onValueChanged.RemoveAllListeners();
			field.onValueChanged.AddListener((string value) =>
			{
				action(value);
			});
		}

		public static void OnSliderChangeValue(this Transform transform, string path, Action action)
		{
			Slider slider = transform.GetChildComponent<Slider>(path);
			if (slider == null)
			{
				Debug.Log(transform.GetRootPath() + "/" + path + "对象不存在组件" + nameof(Slider));
				return;
			}
			slider.onValueChanged.RemoveAllListeners();
			slider.onValueChanged.AddListener((float value) =>
			{
				action();
			});
		}

		public static void OnSliderChangeValue(this Transform transform, string path, Action<float> action)
		{
			Slider slider = transform.GetChildComponent<Slider>(path);
			if (slider == null)
			{
				Debug.Log(transform.GetRootPath() + "/" + path + "对象不存在组件" + nameof(Slider));
				return;
			}
			slider.onValueChanged.RemoveAllListeners();
			slider.onValueChanged.AddListener((float value) =>
			{
				action(value);
			});
		}


		/// <summary>
		/// 设置子角色激活状态
		/// </summary>
		/// <param name="path"></param>
		/// <param name="state"></param>
		public static void SetChildActive(this Transform transform, string path, bool state)
		{
			Transform childTf = transform.Find(path);
			if (childTf != null)
			{
				childTf.gameObject.SetActive(state);
			}
		}

		/// <summary>
		/// 子角色激活状态取反
		/// </summary>
		/// <param name="path"></param>
		public static void SetChildToggleState(this Transform transform, string path)
		{
			Transform childTf = transform.Find(path);
			if (childTf != null)
			{
				bool state = childTf.gameObject.activeSelf;
				childTf.gameObject.SetActive(!state);
			}
		}

		public static void ClearChild(this Transform transform, string path)
		{
			Transform childTf = transform.Find(path);
			if (childTf != null)
			{
				for (int i = childTf.childCount - 1; i >= 0; i--)
				{
					Transform tf = childTf.GetChild(i);
					GameObject.Destroy(tf.gameObject);
				}
			}
		}
	}

}
