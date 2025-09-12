namespace TKFramework
{
	/// <summary>
	/// 游戏事件的接口，所有事件数据类都要继承并实现这个接口
	/// </summary>
	public interface IGameEvent { }

	/// <summary>
	/// 比较数据的优先级, 优先队列判断优先级
	/// A.Priority(B) true   A的优先级比B高
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IPriority<T>
	{
		public bool Priority(T A);
	}


	/// <summary>
	/// 多语言文本刷新接口
	/// </summary>
	public interface ILangRefresh
	{
		public void OnLangRefresh();
	}

	/// <summary>
	/// 通过字符串数组设置值接口
	/// </summary>
	public interface ISetValue
	{
		public void SetValue(string[] stringArr);
	}
}
