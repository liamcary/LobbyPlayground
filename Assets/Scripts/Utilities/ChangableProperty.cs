using System;
using System.Collections.Generic;

public class ChangableProperty<T> where T : IComparable
{
	public T Value
	{
		get => _value;
		set
		{
			if (EqualityComparer<T>.Default.Equals(_value, value)) {
				return;
			}

			_value = value;
			OnValueChanged?.Invoke(_value);
		}
	}

	public Action<T> OnValueChanged;

	T _value;

	public ChangableProperty(T value = default)
	{
		_value = value;
	}
}
