using System.Collections.Generic;
using System.Drawing;

namespace WpfApp
{
	public static class K3M
	{
		public unsafe static Bitmap Apply(Image bmp)
		{
			lookups ??= new[] { A1, A2, A3, A4, A5 };

			var ptr = bmp.GetData();
			fixed (byte* f = ptr)
			{
				List<int> ones = new();
				for (int i = bmp.Stride + bmp.Channels; i < bmp.Length - bmp.Stride - bmp.Channels; i++)
					if (ptr[i] == One)
						ones.Add(i);

				List<int> borders = new();

				int count = 0;
				int lastCount = 0;
				bool any = false;
				while (true)
				{
					count = 0;

					foreach (int black in ones)
						if (ptr[black] == One && A0.Contains(ComputeSum(f + black, bmp.Width)))
						{
							borders.Add(black);
							++count;
						}

					if (count == lastCount && !any)
						break;
					any = false;
					lastCount = count;

					foreach (HashSet<int> lookup in lookups)
						foreach (var border in borders)
							if (ptr[border] == One && lookup.Contains(ComputeSum(f + border, bmp.Width)))
							{
								ptr[border] = Zero;
								any = true;
							}
				}
			}

			return bmp.SetData(ptr).Free();
		}

		private static int[] GetOffsets(int stride, int channels) => new[]
			{
				-channels - stride, 0 - stride, channels - stride,
				-channels         , 0         , channels         ,
				-channels + stride, 0 + stride, channels + stride,
			};

		private unsafe static int ComputeSum(byte* ptr, int w)
		{
			int sum = 0;
			int[] offsets = GetOffsets(w * 3, 3);

			for (int i = 0; i < Matrix.Length; i++)
				sum += ptr[offsets[i]] == One ? Matrix[i] : 0;

			return sum;
		}

		private const byte Zero = byte.MaxValue;
		private const byte One = byte.MinValue;

		private static readonly int[] Matrix =
		{
			128,  1, 2,
			 64,  0, 4,
		     32, 16, 8
		};

		private static readonly HashSet<int> A0 = new HashSet<int>(new[] { 3, 6, 7, 12, 14, 15, 24, 28, 30, 31, 48, 56, 60, 62, 63, 96, 112, 120, 124, 126, 127, 129, 131, 135, 143, 159, 191, 192, 193, 195, 199, 207, 223, 224, 225, 227, 231, 239, 240, 241, 243, 247, 248, 249, 251, 252, 253, 254 });

		private static HashSet<int>[] lookups;

		private static readonly HashSet<int> A1 = new HashSet<int>(new[] { 7, 14, 28, 56, 112, 131, 193, 224 });
		private static readonly HashSet<int> A2 = new HashSet<int>(new[] { 7, 14, 15, 28, 30, 56, 60, 112, 120, 131, 135, 193, 195, 224, 225, 240 });
		private static readonly HashSet<int> A3 = new HashSet<int>(new[] { 7, 14, 15, 28, 30, 31, 56, 60, 62, 112, 120, 124, 131, 135, 143, 193, 195, 199, 224, 225, 227, 240, 241, 248 });
		private static readonly HashSet<int> A4 = new HashSet<int>(new[] { 7, 14, 15, 28, 30, 31, 56, 60, 62, 63, 112, 120, 124, 126, 131, 135, 143, 159, 193, 195, 199, 207, 224, 225, 227, 231, 240, 241, 243, 248, 249, 252 });
		private static readonly HashSet<int> A5 = new HashSet<int>(new[] { 7, 14, 15, 28, 30, 31, 56, 60, 62, 63, 112, 120, 124, 126, 131, 135, 143, 159, 191, 193, 195, 199, 207, 224, 225, 227, 231, 239, 240, 241, 243, 248, 249, 251, 252, 254 });
	}
}
