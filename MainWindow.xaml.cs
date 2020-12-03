using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WpfApp
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private Bitmap bitmap;

		public MainWindow()
		{
			InitializeComponent();
			this.bitmap = new Bitmap("apple.png");
			this.MainImage.Source = CreateBitmapSource(bitmap);
		}

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
			WriteableBitmap writeable = new WriteableBitmap(bmpDecoder.Frames.Single());
			writeable.Freeze();
			return writeable;
		}

		private void Median_Click(object sender, RoutedEventArgs e)
		{
			this.bitmap = Effects.MedianFilter(this.bitmap);
			this.MainImage.Source = CreateBitmapSource(bitmap);
		}

		private void Sharpen_Click(object sender, RoutedEventArgs e)
		{
			this.bitmap = Effects.Filter(this.bitmap, 
				new[] 
				{ 
					0, -1, 0,
					-1, 5, -1,
					0, -1, 0
				});
			this.MainImage.Source = CreateBitmapSource(bitmap);
		}

		private void Otsu_Click(object sender, RoutedEventArgs e)
		{
			this.bitmap = Effects.Otsu(this.bitmap);
			this.MainImage.Source = CreateBitmapSource(bitmap);
		}

		private void Reset_Click(object sender, RoutedEventArgs e)
		{
			this.bitmap = new Bitmap("apple.png");
			this.MainImage.Source = CreateBitmapSource(bitmap);
		}

		private void MainSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => 
			this.MainLabel.Content = (int)this.MainSlider.Value;

		private void Niblack_Click(object sender, RoutedEventArgs e)
		{
			this.bitmap = Effects.Niblack(this.bitmap);
			this.MainImage.Source = CreateBitmapSource(bitmap);
		}

		private void Savuola_Click(object sender, RoutedEventArgs e)
		{
			this.bitmap = Effects.Savuola(this.bitmap);
			this.MainImage.Source = CreateBitmapSource(bitmap);
		}

		private void Phansalkar_Click(object sender, RoutedEventArgs e)
		{
			this.bitmap = Effects.Phansalkar(this.bitmap);
			this.MainImage.Source = CreateBitmapSource(bitmap);
		}

		private void Grayscale_Click(object sender, RoutedEventArgs e)
		{
			this.bitmap = Effects.Grayscale(this.bitmap);
			this.MainImage.Source = CreateBitmapSource(bitmap);
		}
	}
}
