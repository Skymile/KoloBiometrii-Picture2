using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WpfApp
{
	public class Image : IDisposable
	{
		public Image(Bitmap bmp) =>
			this.bmp = bmp;
		public Image(Image image) =>
			this.bmp = new Bitmap(image.bmp.Width, image.bmp.Height, image.bmp.PixelFormat);
		public Image(string filename) =>
			this.bmp = new Bitmap(filename);

		public unsafe Image Apply(IPixelEffect pixelEffect)
		{
			var bytes = GetData();
			var write = new byte[bytes.Length];
			fixed (byte* r = bytes)
			fixed (byte* w = write)
				for (int i = 0; i < Length; i += Channels)
					pixelEffect.Apply(r + i, w + i);
			return SetData(write).Free();
		}

		public Image Copy()
		{
			var bmp = new Image(this);
			var data = GetData();
			bmp.GetData();
			bmp.SetData(data).Free();
			Free();
			return bmp;
		}

		public static implicit operator Bitmap(Image img) => img.bmp;

		public byte[] GetData()
		{
			data ??= bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.ReadWrite, bmp.PixelFormat);
			isDisposed = false;
			var bytes = new byte[data.Stride * data.Height];
			Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
			return bytes;
		}

		public Image SetData(byte[] array)
		{
			data ??= bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.ReadWrite, bmp.PixelFormat);
			isDisposed = false;
			Marshal.Copy(array, 0, data.Scan0, array.Length);
			return this;
		}

		~Image() => Dispose();
		public void Dispose()
		{
			if (data is not null)
			{
				if (!isDisposed)
				{
					isDisposed = true;
					bmp.UnlockBits(data);
					data = null;
				}
				GC.SuppressFinalize(this);
			}
		}

		public Image Free()
		{
			if (!isDisposed)
				this.bmp.UnlockBits(data);
			data = null;
			return this;
		}

		public int Length => data.Stride * data.Height;
		public int Stride => data.Stride;
		public int Height => data.Height;
		public int Width => data.Width;
		public int Channels => System.Drawing.Image.GetPixelFormatSize(bmp.PixelFormat) / 8;

		private BitmapData data;
		private bool isDisposed = false;
		public readonly Bitmap bmp;
	}
}
