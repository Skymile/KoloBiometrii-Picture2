using System.ComponentModel;
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
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		public MainWindow()
		{
			InitializeComponent();
			this.bitmap = new Bitmap(Filename);
			//Threshold = 67;
			this.MainImage.Source = CreateBitmapSource(bitmap);
			this.DataContext = this;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void Set<T>(ref T field, T value)
		{
			field = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(field)));
		}

		public bool IsAutoRefreshOn
		{
			get => this.isAutoRefreshOn;
			set => Set(ref this.isAutoRefreshOn, value);
		}
		private bool isAutoRefreshOn;

		public byte Threshold
		{
			get => this.threshold;
			set
			{
				Set(ref this.threshold, value);
				if (IsAutoRefreshOn)
					Threshold_Click(null, null);
			}
		}
		private byte threshold = 128;

		public double SauvolaRatio
		{
			get => this.sauvolaRatio;
			set
			{
				Set(ref this.sauvolaRatio, value);
				if (IsAutoRefreshOn)
					Savuola_Click(null, null);
			}
		}
		private double sauvolaRatio = 0.2;

		public double SauvolaDiv
		{
			get => this.sauvolaDiv;
			set
			{
				Set(ref this.sauvolaDiv, value);
				if (IsAutoRefreshOn)
					Savuola_Click(null, null);
			}
		}
		private double sauvolaDiv = 0;

		public double NiblackRatio
		{
			get => this.niblackRatio;
			set
			{
				Set(ref this.niblackRatio, value);
				if (IsAutoRefreshOn)
					Niblack_Click(null, null);
			}
		}
		private double niblackRatio = 0.2;

		public double NiblackOffsetC
		{
			get => this.niblackOffsetC;
			set
			{
				Set(ref this.niblackOffsetC, value);
				if (IsAutoRefreshOn)
					Niblack_Click(null, null);
			}
		}
		private double niblackOffsetC = 0;

		public double PhansalkarPow
		{
			get => phansalkarPow;
			set
			{
				Set(ref this.phansalkarPow, value);
				if (IsAutoRefreshOn)
					Phansalkar_Click(null, null);
			}
		}

		public double PhansalkarQ
		{
			get => phansalkarQ;
			set
			{
				Set(ref this.phansalkarQ, value);
				if (IsAutoRefreshOn)
					Phansalkar_Click(null, null);
			}
		}

		public double PhansalkarRatio
		{
			get => phansalkarRatio;
			set
			{
				Set(ref this.phansalkarRatio, value);
				if (IsAutoRefreshOn)
					Phansalkar_Click(null, null);
			}
		}

		public double PhansalkarDiv
		{
			get => phansalkarDiv;
			set
			{
				Set(ref this.phansalkarDiv, value);
				if (IsAutoRefreshOn)
					Phansalkar_Click(null, null);
			}
		}

		private double phansalkarPow   = 2;
		private double phansalkarQ     = 10;
		private double phansalkarRatio = 0.5;
		private double phansalkarDiv   = 0.25;

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

		private void Niblack_Click(object sender, RoutedEventArgs e)
		{
			this.bitmap = Effects.Niblack(GetBitmap(), null, this.NiblackRatio, this.NiblackOffsetC);
			this.MainImage.Source = CreateBitmapSource(bitmap);
		}

		private void Savuola_Click(object sender, RoutedEventArgs e)
		{
			this.bitmap = Effects.Savuola(GetBitmap(), sauvolaRatio, sauvolaDiv);
			this.MainImage.Source = CreateBitmapSource(bitmap);
		}

		private void Phansalkar_Click(object sender, RoutedEventArgs e)
		{
			this.bitmap = Effects.Phansalkar(GetBitmap(), phansalkarPow, phansalkarQ, phansalkarRatio, phansalkarDiv);
			this.MainImage.Source = CreateBitmapSource(bitmap);
		}

		private void Grayscale_Click(object sender, RoutedEventArgs e)
		{
			this.bitmap = Effects.Grayscale(this.bitmap);
			this.MainImage.Source = CreateBitmapSource(bitmap);
		}

		private void Pixelize_Click(object sender, RoutedEventArgs e)
		{
			this.bitmap = Effects.Pixelize(this.bitmap);
			this.MainImage.Source = CreateBitmapSource(bitmap);
		}

		private void Threshold_Click(object sender, RoutedEventArgs e)
		{
			this.bitmap = Effects.Threshold(GetBitmap(), this.threshold);
			this.MainImage.Source = CreateBitmapSource(bitmap);
		}

		private Bitmap GetBitmap() => IsAutoRefreshOn ? new Bitmap(Filename) : bitmap;

		private void MainSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) =>
			this.MainLabel.Content = (int)this.MainSlider.Value;

		private Bitmap bitmap;

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			this.bitmap?.Save("apple2.png");
		}

		private const string Filename = "apple2.png";

		private void K3M_Click(object sender, RoutedEventArgs e)
		{
			this.bitmap = K3M.Apply(GetBitmap());
			this.MainImage.Source = CreateBitmapSource(bitmap);
		}
	}
}
