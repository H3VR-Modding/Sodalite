using System;
using System.Collections;
using System.Collections.Generic;

namespace Sodalite.Utilities;

/// <summary>
///     Because .NET 3.5 doesn't include the ObservableCollection class, here's my own implementation.
///     This is a HashSet that gives event callbacks when an item is added or removed.
/// </summary>
/// <typeparam name="T">The type of the element inside</typeparam>
public class ObservableHashSet<T> : IList<T>
{
#pragma warning disable 1591
	private readonly List<T> _list = new();

	public event Action<T>? ItemAdded;

	public event Action<T>? ItemRemoved;

	public void Add(T item)
	{
		if (_list.Contains(item)) return;
		_list.Add(item);
		ItemAdded?.Invoke(item);
	}

	public bool Remove(T item)
	{
		var ret = _list.Remove(item);
		if (ret) ItemRemoved?.Invoke(item);
		return ret;
	}

	public void RemoveAt(int index)
	{
		if (index >= _list.Count) throw new IndexOutOfRangeException();
		Remove(_list[index]);
	}

	#region Forwarding to List<T>

	public T this[int index]
	{
		get => _list[index];
		set => _list[index] = value;
	}

	public int Count => _list.Count;
	public bool IsReadOnly => false;

	public void Clear()
	{
		_list.Clear();
	}

	public bool Contains(T item)
	{
		return _list.Contains(item);
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		_list.CopyTo(array, arrayIndex);
	}

	public IEnumerator<T> GetEnumerator()
	{
		return _list.GetEnumerator();
	}

	public int IndexOf(T item)
	{
		return _list.IndexOf(item);
	}

	public void Insert(int index, T item)
	{
		_list.Insert(index, item);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _list.GetEnumerator();
	}

	#endregion

#pragma warning restore 1591
}
