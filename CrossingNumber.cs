﻿using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace WpfApp
{
	// Zwraca double i bierze za parametry double mean, double std
	public enum MinutiaeType
	{
		Ending,      // 1 czarny piksel
		Bifurcation, // 3 czarne piksele po bokach
		Crossing,    // 4 czarne piksele po bokach
	}

	public static class CrossingNumber
	{
		public unsafe static Bitmap Apply(Bitmap bmp, out (MinutiaeType Type, int count)[] minutiaes)
		{
			minutiaes = default;

			var data = bmp.LockBits(ImageLockMode.ReadWrite);

			int stride = data.Stride;
			int height = data.Height;

			int offset = stride + 3;

			byte* ptr = (byte*)data.Scan0.ToPointer();

			for (int i = offset; i < stride * height - offset; i += 3)
				if (ptr[i] != White)
				{
					int sum = 0;
					if (ptr[i - 3] != White) ++sum;
					if (ptr[i + 3] != White) ++sum;
					if (ptr[i - stride] != White) ++sum;
					if (ptr[i + stride] != White) ++sum;

					if (sum == 1)
						ptr[i] = 254;
					else if (sum == 3)
						ptr[i + 1] = 254;
					else if (sum == 4)
						ptr[i + 2] = 254;
				}

			bmp.UnlockBits(data);
			return bmp;
		}

		private const byte White = 255;
	}
}