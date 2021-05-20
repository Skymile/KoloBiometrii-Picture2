using System;

namespace WpfApp
{
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	public sealed class NameAttribute : Attribute
	{
		public NameAttribute(string name) => this.Name = name;

		public readonly string Name;
	}
}
