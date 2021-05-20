using System.ComponentModel;

namespace WpfApp
{
	public class Property<T> : INotifyPropertyChanged
	{
		public Property(T value) => this.pvalue = value;

		public event PropertyChangedEventHandler PropertyChanged;

		public virtual T Value
		{
			get => pvalue;
			set => Set(ref pvalue, value);
		}

		public static implicit operator T(Property<T> property) =>
			property is null ? default : property.Value;

		protected void Set(ref T field, T value)
		{
			field = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(field)));
		}

		protected T pvalue;
	}
}
