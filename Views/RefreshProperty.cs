using System;
using System.Windows;

namespace WpfApp
{
	public class RefreshProperty<T> : Property<T>
	{
		public RefreshProperty(T value, Func<bool> isAutorefresh, Action<object, RoutedEventArgs> refresh)
			 : base(value)
		{
			this.isAutorefresh = isAutorefresh;
			this.refresh = refresh;
		}

		public override T Value {
			get => pvalue;
			set
			{
				Set(ref pvalue, value);
				if (isAutorefresh())
					refresh(null, null);
			}
		}

		private readonly Func<bool> isAutorefresh;
		private readonly Action<object, RoutedEventArgs> refresh;
	}
}
