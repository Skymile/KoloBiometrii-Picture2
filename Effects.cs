﻿using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace WpfApp
{
	public static class BitmapExtensions
	{
		public static BitmapData LockBits(this Bitmap bitmap, ImageLockMode lockMode) =>
			bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.ReadOnly, bitmap.PixelFormat);
	}
	//FiraCode

	public static class Effects
	{
		public unsafe static Bitmap Apply(Bitmap bmp)
		{
			int[] matrix =
			{
				1, 1, 1,
				1, 1, 1,
				1, 1, 1,
			};

			bmp = MedianFilter(bmp);
			return bmp;
		}

		private static int[] GetOffsets(int stride, int channels) => new[]
			{
				-channels - stride, 0 - stride, channels - stride,
				-channels         , 0         , channels		 ,
				-channels + stride, 0 + stride, channels + stride,
			};


		public unsafe static Bitmap Filter(Bitmap bmp, int[] matrix)
		{
			BitmapData readData = bmp.LockBits(ImageLockMode.ReadOnly);
			var writeBmp = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format24bppRgb);
			BitmapData writeData = writeBmp.LockBits(ImageLockMode.WriteOnly);

			byte* r = (byte*)readData.Scan0.ToPointer();
			byte* w = (byte*)writeData.Scan0.ToPointer();

			int stride = readData.Stride;
			int height = bmp.Height;
			int len = stride * height;

			int[] offsets = GetOffsets(stride, 3);

			int div = matrix.Sum();
			if (div == 0)
				div = 1;

			for (int i = stride + 3; i < len - stride - 3; i++)
			{
				int sum = 0;
				for (int j = 0; j < offsets.Length; j++)
					sum += r[i + offsets[j]] * matrix[j];

				sum /= div;

				if (sum > 255) sum = 255;
				if (sum <   0) sum = 0;

				w[i] = (byte)sum;
			}

			bmp.UnlockBits(readData);
			writeBmp.UnlockBits(writeData);
			return writeBmp;
		}

		public unsafe static Bitmap MedianFilter(Bitmap bmp)
		{
			BitmapData readData = bmp.LockBits(ImageLockMode.ReadOnly);
			var writeBmp = new Bitmap(bmp.Width, bmp.Height, bmp.PixelFormat);
			BitmapData writeData = writeBmp.LockBits(ImageLockMode.WriteOnly);

			byte* r = (byte*)readData.Scan0.ToPointer();
			byte* w = (byte*)writeData.Scan0.ToPointer();

			int stride = readData.Stride;
			int height = bmp.Height;
			int len = stride * height;

			int[] offsets = GetOffsets(stride, 3);

			for (int i = stride + 3; i < len - stride - 3; i += 3)
			{
				for (int k = 0; k < 3; k++)
				{
					List<byte> list = new List<byte>();
					foreach (var offset in offsets)
						list.Add(r[i + k + offset]);
					list.Sort();
					w[i + k] = list[list.Count / 2];
				}
			}

			bmp.UnlockBits(readData);
			writeBmp.UnlockBits(writeData);
			return writeBmp;
		}


		public unsafe static Bitmap Otsu(Bitmap bmp)
		{
			BitmapData data = bmp.LockBits(
				new Rectangle(Point.Empty, bmp.Size),
				ImageLockMode.ReadWrite,
				PixelFormat.Format24bppRgb
			);

			byte* ptr = (byte*)data.Scan0.ToPointer();

			int stride = bmp.Width * 3;
			int height = bmp.Height;

			int len = stride * height;
			
			for (int i = 0; i < len; i += 3)
				ptr[i] = ptr[i + 1] = ptr[i + 2] = (byte)(
					(ptr[i] + ptr[i + 1] + ptr[i + 2]) / 3
				);

			int[] histogram = new int[256];
			for (int i = 0; i < len; i += 3)
				++histogram[ptr[i]];

			int histSum = histogram.Sum();

			int sum = 0;
			for (int i = 0; i < 256; i++)
				sum += i * histogram[i];

			float sumB = 0;
			float varMax = 0;

			int back = 0;
			int threshold = 0;

			for (int i = 0; i < 256; i++)
			{
				back += histogram[i];
				if (back == 0)
					continue;

				long fore = histSum - back;
				if (fore == 0)
					break;

				sumB += i * histogram[i];

				float backMean = sumB / back;
				float foreMean = (sum - sumB) / fore;

				float varBetween = (float)
					back * fore * (backMean - foreMean) * (backMean - foreMean);

				if (varBetween > varMax)
				{
					varMax = varBetween;
					threshold = i;
				}
			}

			for (int i = 0; i < len; i++)
				ptr[i] = ptr[i] > threshold ? byte.MaxValue : byte.MinValue;

			bmp.UnlockBits(data);
			return bmp;
		}
	}
}
