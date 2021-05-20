using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace WpfApp
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			this.bitmap = new Bitmap(OldFilename);
			this.MainImage.Source = CreateBitmapSource(bitmap);
			this.DataContext = this;

			foreach (MethodInfo method in typeof(Effects)
				.GetMethods()
				.Where(m => m.ReturnType == typeof(Image)))
			{
				string name = method.Name;
				if (method.GetCustomAttribute<NameAttribute>() is NameAttribute attr)
					name = attr.Name;
				var panel = new StackPanel();
				var btn = new Button
				{
					Content = name,
					IsEnabled = false
				};
				panel.Children.Add(btn);

				var parameters = method.GetParameters();

				List<Slider> sliders = new();

				void action(object sender, RoutedEventArgs e)
				{
					object[] methodParams = sliders
						.Select(i => Convert.ChangeType(i.Value, (Type)i.Tag))
						.Prepend(GetBitmap())
						.ToArray();

					this.bitmap = (Image)method.Invoke(null, methodParams);
					this.MainImage.Source = CreateBitmapSource(bitmap);
				}

				foreach (var parameter in parameters.Skip(1))
					if (parameter.ParameterType.IsPrimitive)
					{
						var slider = new Slider
						{
							Minimum = 0,
							Value = (double)Convert.ChangeType(
								parameter.HasDefaultValue ? parameter.DefaultValue : 0,
								typeof(double)
							),
							Maximum = 255,
							AutoToolTipPlacement = System.Windows.Controls.Primitives.AutoToolTipPlacement.TopLeft,
							Tag = parameter.ParameterType
						};
						slider.ValueChanged += (s, e) =>
						{
							if (IsAutoRefreshOn)
								action(null, null);
						};
						sliders.Add(slider);
						panel.Children.Add(new Label { Content = parameter.Name });
						panel.Children.Add(slider);
					}

				btn.IsEnabled = true;
				btn.Click += action;

				MainPanel.Children.Add(panel);
			}
		}

		public bool IsAutoRefresh() => IsAutoRefreshOn;

		public Property<bool> IsAutoRefreshOn { get; } = new(false);

		private static BitmapSource CreateBitmapSource(Bitmap bmp)
		{
			using var memoryStream = new MemoryStream();
			bmp.Save(memoryStream, ImageFormat.Png);
			memoryStream.Seek(0, SeekOrigin.Begin);
			var bmpDecoder = BitmapDecoder.Create(
				memoryStream,
				BitmapCreateOptions.PreservePixelFormat,
				BitmapCacheOption.OnLoad
			);
			var writeable = new WriteableBitmap(bmpDecoder.Frames.Single());
			writeable.Freeze();
			return writeable;
		}

		private Image GetBitmap() => IsAutoRefreshOn ? new Image(Filename) : new Image(bitmap);

		private void MainSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) =>
			this.MainLabel.Content = (int)this.MainSlider.Value;

		private void CN_Click(object sender, RoutedEventArgs e)
		{
			this.bitmap = CrossingNumber.Apply(GetBitmap(), out _);
			this.MainImage.Source = CreateBitmapSource(bitmap);
		}

		private void K3M_Click(object sender, RoutedEventArgs e)
		{
			this.bitmap = K3M.Apply(GetBitmap());
			this.MainImage.Source = CreateBitmapSource(bitmap);
		}

		private void Reset_Click(object sender, RoutedEventArgs e)
		{
			this.bitmap = new Bitmap(OldFilename);
			this.MainImage.Source = CreateBitmapSource(bitmap);
		}

		private Bitmap bitmap;

		private void Save_Click(object sender, RoutedEventArgs e) => this.bitmap?.Save(Filename);

		private const string OldFilename = "test.png";
		private const string Filename    = "test.png";
	}
}
