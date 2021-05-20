using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Diagnostics;
using System.IO;

// PPM
	// PBM Portable Bit Map - 0 1
	// PGM Portable Grayscale Map - Skala szarości
	// PPM Portable Pixel Map - RGB
//
// Tekst
	// PBM P1
	// PGM P2
	// PPM P3
//
// Binarny
	// PBM P4
	// PGM P5
	// PPM P6
//
/*
P5 2 2
10 11 12 13
 */
/*
P6 2 2
10 11 12 13 10 11 12 13 10 11 12 13
 */

/*
Header Width Height
#asdad
1 2 4 5 7 8 9 0 10
1 2 4
*/

namespace WpfApp
{
	public delegate double NiblackFormulae(double mean, double std);
	public unsafe delegate void PixelEffectCallback(byte* read, byte* write);
	public unsafe delegate void ChannelsEffectCallback(IEnumerable<byte> read, byte* write);

	public interface IPixelEffect
	{
		unsafe void Apply(byte* r, byte* w);
		int Channels { get; set; }
	}

	public class ChannelsEffect : IPixelEffect
	{
		public ChannelsEffect(ChannelsEffectCallback callback) => this.callback = callback;

		public int Channels { get; set; }

		public unsafe void Apply(byte* r, byte* w)
		{

		}

		private readonly ChannelsEffectCallback callback;
	}

	public class PixelEffect : IPixelEffect
	{
		public PixelEffect(PixelEffectCallback callback) => this.callback = callback;

		public unsafe void Apply(byte* r, byte* w) => callback.Invoke(r, w);

		private readonly PixelEffectCallback callback;

		public int Channels { get; set; }
	}

	public class Writer : IDisposable
	{
		public Writer(string filename, bool isBinary, bool ignoreWhitespace)
		{
			if (isBinary)
			{
				fileStream = new FileStream(filename, FileMode.OpenOrCreate);
				binWriter = new BinaryWriter(fileStream);
			}
			else strWriter = new StreamWriter(filename);

			this.isBinary = isBinary;
			this.ignoreWhitespace = ignoreWhitespace;
		}

		public void Write(string value)
		{
			if (ignoreWhitespace)
				value = value.Replace(" ", string.Empty).Trim();
			if (isBinary)
			{
				foreach (char ch in value)
					Write((byte)ch);
			}
			else
				strWriter.Write(value);
		}
		public void Write(byte value)
		{
			if (isBinary)
				binWriter.Write(value);
			else
				strWriter.Write(value);
		}

		public void Write(char value)
		{
			if (ignoreWhitespace && char.IsWhiteSpace(value))
				return;

			if (isBinary)
				binWriter.Write(value);
			else
				strWriter.Write(value);
		}

		public void Dispose()
		{
			fileStream?.Dispose();
			binWriter?.Dispose();
			strWriter?.Dispose();
		}

		private readonly FileStream fileStream;
		private readonly BinaryWriter binWriter;
		private readonly StreamWriter strWriter;
		private readonly bool isBinary;
		private readonly bool ignoreWhitespace;
	}

	public unsafe static class Effects
	{

		public static readonly PixelEffect Grayscale = new(
			(r, w) => w[0] = w[1] = w[2] = (byte)((r[0] + r[1] + r[2]) / 3)
		);

		public static readonly ChannelsEffect MinRGB = new(
			(r, w) => {
				var k = r.Select((v, i) => (v, i)).OrderBy(i => i.v).First();
				w[0] = w[1] = w[2] = 0;
				w[k.i] = k.v;
			}
		);

		public static Image PpmRead(Image rBmp)
		{
			const int value = 1;

			PpmWrite(rBmp);

			string filename = $"ppm_p{value}.bin";

			var bytes = File.ReadAllBytes(filename);

			char c = (char)bytes[0];
			int n = bytes[1] - '0';
			int width  = bytes[2] - '0';
			int height = bytes[3] - '0';

			;

			return rBmp;
		}

		public static Image PpmWrite(Image rBmp)
		{
			var data = rBmp.GetData();

			const int value = 1;

			string filename = $"ppm_p{value}.bin";

			using var writer = new Writer(filename, value < 4, value < 4);

			const byte zero = 0;
			const byte one = 1;

			writer.Write($"P{value} {rBmp.Width} {rBmp.Height}\n");
			for (int i = 0; i < rBmp.Height; i++)
			{
				for (int j = 0; j < rBmp.Stride; j += rBmp.Channels)
				{
					int offset = i * rBmp.Stride + j;
					byte first = value % 3 == 1
						? data[offset] < 128 ? zero : one
						: data[offset];

					if (rBmp.Channels >= 3)
					{
						writer.Write(first);

						if (value % 3 == 0)
						{
							writer.Write(' ');
							writer.Write(data[offset + 1]);
							writer.Write(' ');
							writer.Write(data[offset + 2]);
						}
					}
					else if (rBmp.Channels == 1)
						writer.Write(first);
					else throw new InvalidDataException(nameof(rBmp.Channels));

					writer.Write(' ');
				}
				writer.Write("\n");
			}

			return rBmp.Free();
		}

		public static Image NearestNeighbourUpscale(Image rBmp)
		{
			var bytes = rBmp.GetData();
			using var wBmp = new Image(
				new Bitmap(rBmp.Width * 2, rBmp.Height * 2, PixelFormat.Format24bppRgb)
			);

			var newBytes = wBmp.GetData();
			for (int i = 0; i < rBmp.Width; i++)
				for (int j = 0; j < rBmp.Height; j++)
					for (int k = 0; k < 3; k++)
					{
						byte value = bytes[i * 3 + j * rBmp.Stride + k];

						for (int x = 0; x < 2; x++)
							for (int y = 0; y < 2; y++)
								newBytes[
									(i * 2 + x) * 3 +
									(j * 2 + y) * wBmp.Stride + k
								] = value;
					}

			rBmp.Dispose();
			return wBmp.SetData(newBytes);
		}

		public static Image BicubicVerticalUpscale(Image rBmp)
		{
			byte[] rData = rBmp.GetData();

			using var wBmp = new Image(
				new Bitmap(rBmp.Width, rBmp.Height * 2, PixelFormat.Format24bppRgb)
			);

			const float ratio = 0.5f;
			byte[] wData = wBmp.GetData();

			for (int y = 0; y < wBmp.Height; y++)
			{
				int outRow = y * rBmp.Stride;

				float center = (y + 0.5f) * ratio - 0.5f;
				int c = (int)center;

				int[] offsets =
				{
					Math.Max(c - 1, 0) * rBmp.Stride,
					(c + 0) * rBmp.Stride,
					Math.Min(c + 1, rBmp.Height - 1) * rBmp.Stride,
					Math.Min(c + 2, rBmp.Height - 1) * rBmp.Stride,
				};

				float d1 = center - c;
				float d2 = 1 - d1;
				float d3 = 2 - d1;
				float d4 = -d1;

				float[] weights =
				{
					0f,
					(1.5f * d1 - 2.5f) * (d1 * d1) + 1,
					(1.5f * d2 - 2.5f) * (d2 * d2) + 1,
					-0.5f * (d4 * d4) * (d3 - 1),
				};
				weights[0] = 1 - weights[1] - weights[2] - weights[3];

				for (int x = 0; x < wBmp.Width; x++)
					for (int k = 0; k < 3; k++)
					{
						int offset = x * 3 + k;

						float val = 0f;
						for (int v = 0; v < 4; v++)
							val += rData[offsets[v] + offset] * weights[v];

						wData[outRow++] = (byte)Math.Clamp(val, 0f, 255f);
					}
			}

			rBmp.Free();
			return wBmp.SetData(wData);
		}

		public static Image BicubicDebug(Image img)
		{
			var sw = Stopwatch.StartNew();
			var tmp = BicubicHorizontalUpscale(img);
			Debug.WriteLine(sw.ElapsedTicks);
			return tmp;
		}
		// 9 221 969 | 4 896 642
		// Benchmark.NET

		public static unsafe Image BicubicHorizontalUpscale(Image rBmp)
		{
/*
var list = new List<int>();
for (int i = 1; i < 31)
	list.Add(i);
var list = Enumerable.Range(1, 30).ToList();

foreach (var i in list)
	Console.WriteLine(
		i % 15 == 0 ? "FizzBuzz" :
		i % 3 == 0 ? "Fizz" :
		i % 5 == 0 ? "Buzz" : i.ToString()
	)

1 2 3 4 5 6 7 .. 30

	 n % 3 => Fizz
	 n % 5 => Buzz
	 n % 3 && n % 5 => FizzBuzz
	 n => n
 */
			byte[] rData = rBmp.GetData();

			using var wBmp = new Image(
				new Bitmap(rBmp.Width * 2, rBmp.Height, PixelFormat.Format24bppRgb)
			);

			const float centerOffset = 0.5f * 0.5f - 0.5f;
			const float ratio = 0.5f;

			byte[] wData = wBmp.GetData();

			int wwidth = wBmp.Width;
			int rwidth = rBmp.Width;
			int wstride = wBmp.Stride;
			int rstride = rBmp.Stride;

			float* weights = stackalloc float[4];
			int* offsets = stackalloc int[4];

			for (int y = 0; y < wBmp.Height; y++)
			{
				int outRow = y * wstride;
				int inRow  = y * rstride;

				for (int x = 0; x < wwidth; x++)
				{
					float center = x * ratio + centerOffset;
					int c = (int)center;

					float d1 = center - c;
					float d2 = 1 - center + c;

					float d3 = 2 - d1;
					float d4 = -d1;

					weights[3] = -0.5f * (d4 * d4) * (d3 - 1);
					weights[2] = (1.5f * d2 - 2.5f) * (d2 * d2) + 1;
					weights[1] = (1.5f * d1 - 2.5f) * (d1 * d1) + 1;

					offsets[3] = Math.Min(c + 2, rwidth - 1) * 3;
					offsets[2] = Math.Min(c + 1, rwidth - 1) * 3;
					offsets[1] = c * 3;
					offsets[0] = Math.Max(c - 1, 0) * 3;

					weights[0] = 1 - weights[1] - weights[2] - weights[3];

					int k = 0;
					float val = 0f;
					int v = 0;
					val += rData[inRow + offsets[v] + k] * weights[v];
					val += rData[inRow + offsets[v] + k] * weights[++v];
					val += rData[inRow + offsets[v] + k] * weights[++v];
					val += rData[inRow + offsets[v] + k] * weights[++v];
					wData[outRow++] = (byte)Math.Clamp(val, 0f, 255f);
					++k;
					val = 0f;

					v = 0;
					val += rData[inRow + offsets[v] + k] * weights[v];
					val += rData[inRow + offsets[v] + k] * weights[++v];
					val += rData[inRow + offsets[v] + k] * weights[++v];
					val += rData[inRow + offsets[v] + k] * weights[++v];
					wData[outRow++] = (byte)Math.Clamp(val, 0f, 255f);
					++k;
					val = 0f;

					v = 0;
					val += rData[inRow + offsets[v] + k] * weights[v];
					val += rData[inRow + offsets[v] + k] * weights[++v];
					val += rData[inRow + offsets[v] + k] * weights[++v];
					val += rData[inRow + offsets[v] + k] * weights[++v];
					wData[outRow++] = (byte)Math.Clamp(val, 0f, 255f);
				}
			}

			rBmp.Free();
			return wBmp.SetData(wData);
		}

		public static Image BicubicHorizontalUpscale2(Image rBmp)
		{
			byte[] rData = rBmp.GetData();

			using var wBmp = new Image(
				new Bitmap(rBmp.Width * 2, rBmp.Height, PixelFormat.Format24bppRgb)
			);

			const float centerOffset = 0.5f * 0.5f - 0.5f;
			const float ratio = 0.5f;

			byte[] wData = wBmp.GetData();

			for (int y = 0; y < wBmp.Height; y++)
			{
				int outRow = y * wBmp.Stride;
				int inRow = y * rBmp.Stride;

				for (int x = 0; x < wBmp.Width; x++)
				{
					float center = x * ratio + centerOffset;
					int c = (int)center;

					float d1 = center - c;
					float d2 = 1 - d1;
					float d3 = 2 - d1;
					float d4 = -d1;

					float[] weights =
					{
						0f,
						(1.5f * d1 - 2.5f) * (d1 * d1) + 1,
						(1.5f * d2 - 2.5f) * (d2 * d2) + 1,
						-0.5f * (d4 * d4) * (d3 - 1),
					};
					weights[0] = 1 - weights[1] - weights[2] - weights[3];

					int[] offsets =
					{
						Math.Max(c - 1, 0) * 3,
						c * 3,
						Math.Min(c + 1, rBmp.Width - 1) * 3,
						Math.Min(c + 2, rBmp.Width - 1) * 3,
					};

					for (int k = 0; k < 3; k++)
					{
						float val = 0f;
						for (int v = 0; v < 4; v++)
							val += rData[inRow + offsets[v] + k] * weights[v];

						wData[outRow++] = (byte)Math.Clamp(val, 0f, 255f);
					}
				}
			}

			rBmp.Free();
			return wBmp.SetData(wData);
		}

		//public static readonly ChannelsEffect Min = new(
		//	(r, w) =>
		//);

		public static Image Predator(Image bmp)
		{
			var sobel = Sobel(bmp.Copy());
			//MinRGB(bmp);
			bmp.Apply(MinRGB);
			Pixelize(bmp);

			var data = bmp.GetData();
			var sobelData = sobel.GetData();

			for (int i = 0; i < bmp.Length; i++)
			{
				if (sobelData[i] > 32)
					data[i] = (byte)((data[i] + sobelData[i] * 4) / 2);
			}
			return bmp.SetData(data).Free();
		}

		public static Image Sobel(Image bmp) =>
			Filter(bmp, Convolutions.SobelHorizontal, Convolutions.SobelVertical);

		public static Image Emboss(Image bmp) =>
			Filter(bmp, Convolutions.Emboss);

		public static Image Sharpen(Image bmp) =>
			Filter(bmp, Convolutions.Sharpen);

		public static Image Gradient(Image bmp)
		{
			const int maxValue = 256;

			var bytes = bmp.GetData();

			double wRatio = bmp.Width / maxValue;
			double hRatio = bmp.Height / maxValue;

			int wX = (int)wRatio;
			int wY = (int)hRatio;

			for (int x = 0; x < maxValue; x++)
			{
				for (int y = 0; y < maxValue; y++)
				{
					for (int oX = 0; oX < wX; oX++)
						for (int oY = 0; oY < wY; oY++)
						{
							int o = (int)(
								(x * wRatio + oX) * bmp.Channels +
								(y * hRatio + oY) * bmp.Stride
							);

							int b = (int)x - y;

							if (b < 0) b = 0;
							if (b > 255) b = 255;

							bytes[o + 0] = (byte)x;//B
							bytes[o + 1] = (byte)y;//G
							bytes[o + 2] = (byte)b;//R
						}
				}
			}

			return bmp.SetData(bytes).Free();
		}

		public static Image Mandelbrot(
				Image bmp,
				int maxIter = 25,
				double detailLevel = 4,
				double dX = 1,
				double dY = 1
			)
		{
			var bytes = bmp.GetData();
			for (int i = 0; i < bytes.Length; i++)
				bytes[i] = 0;

			var r = new Random();
			var colors = new byte[maxIter][];
			for (int i = 0; i < colors.Length; i++)
			{
				var rgb = new byte[3];
				r.NextBytes(rgb);
				colors[i] = rgb;
			}

			for (double y = 0; y < bmp.Height; y++)
				for (double x = 0; x < bmp.Width; x++)
				{
					double offsetX = x / bmp.Width * 2 - dX;
					double offsetY = y / bmp.Height * 2 - dY;

					double zX = 0;
					double zY = 0;

					int k = -1;
					double abs;
					do
					{
						(zX, zY) = (
							zX * zX - zY * zY + offsetX,
							2 * zX * zY + offsetY
						);
						abs = zX * zX + zY * zY;
						++k;
					} while (abs <= detailLevel && k < maxIter);

					if (k < maxIter)
					{
						byte[] color = colors[k];
						int o = (int)(x * bmp.Channels + y * bmp.Stride);

						bytes[o + 0] = color[0];
						bytes[o + 1] = color[1];
						bytes[o + 2] = color[2];
					}
				}

			return bmp.SetData(bytes).Free();
		}

		[Name("GoL - Seeds")]
		public static Image CellularAutomata(Image bmp, int count = 1)
		{
			var r = bmp.GetData();
			var w1 = new byte[r.Length];
			var w2 = new byte[r.Length];

			for (int i = 0; i < bmp.Length; i++)
				w1[i] = w2[i] = r[i];

			int[] offsets = OperationMatrix.Default(bmp.Stride, bmp.Channels);

			for (int i = 0; i < bmp.Length; i++)
				w2[i] = 255;

			for (int i = 0; i < bmp.Length; i += bmp.Channels)
			{
				int blackCount = 0;
				foreach (int offset in offsets)
					if (offset != 0)
					{
						int o = i + offset;
						if (o > 0 && o < bmp.Channels && w1[o] == bmp.Channels)
							++blackCount;
					}
				if (blackCount == 2)
					w2[i + 0] = w2[i + 1] = w2[i + 2] = 0;
			}

			return count % 2 == 0 ? bmp.SetData(w1) : bmp.SetData(w2);
		}

		public static Image Clusters(Image bmp)
		{
			byte[] r = bmp.GetData();
			byte[] w = new byte[r.Length];
			for (int i = 0; i < bmp.Length; i++)
				w[i] = r[i];

			int[] offsets = OperationMatrix.Default(bmp.Stride, bmp.Channels);

			var IndexToGroup = new Dictionary<int, int>();
			var GroupToColor = new Dictionary<int, byte[]>();

			int groupCount = 0;
			var rand = new Random();

			for (int i = 0; i < bmp.Length; i += bmp.Channels)
			{
				if (IndexToGroup.ContainsKey(i))
					continue;

				var all = new HashSet<int>();
				var current = new List<int>();
				var next = new List<int>();

				current.Add(i);
				int value = r[i] / 10;
				IndexToGroup[i] = groupCount;
				byte[] rgb = new byte[bmp.Channels];
				rand.NextBytes(rgb);
				GroupToColor[groupCount] = rgb;

				while (current.Count > 0)
				{
					// Znajduje kolejnych spełniających warunek
					// Wszystkie Current są zamienione na All
					// Wszystkie Next są teraz Current

					foreach (int k in current)
						if (!all.Contains(k))
						{
							all.Add(k);
							IndexToGroup[k] = groupCount;

							foreach (int offset in offsets)
							{
								int o = k + offset;
								if (o > 0 && o < bmp.Length && value == r[o] / 10)
									next.Add(o);
							}
						}
					current = next;
					next = new List<int>();
				}
				++groupCount;
			}

			foreach (var i in IndexToGroup)
			{
				byte[] rgb = GroupToColor[i.Value];

				for (int k = 0; k < rgb.Length; k++)
					w[i.Key + k] = rgb[k];
			}
			return bmp.SetData(w).Free();
		}

		public static Image Apply(Image bmp)
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

		public static Image Threshold(Image bmp, byte threshold = 128)
		{
			var ptr = bmp.GetData();
			for (int i = 0; i < bmp.Length; i += 3)
				ptr[i] = ptr[i + 1] = ptr[i + 2] =
					(ptr[i] + ptr[i + 1] + ptr[i + 2]) / 3 > threshold ? byte.MaxValue : byte.MinValue;
			return bmp.SetData(ptr).Free();
		}

		public static Image Phansalkar(Image bmp, double pow = 2, double q = 10, double ratio = 0.5, double div = 0.25) =>
			Niblack(bmp, (mean, std) => mean * (1 + pow * Math.Exp(-q * mean) + ratio * (std / div - 1)));

		public static Image Savuola(Image bmp, double ratio = 0.5, double div = 2) =>
			Niblack(bmp, (mean, std) => mean * (1 + ratio * (std / div - 1)));

		public static Image Niblack(Image bmp, NiblackFormulae formulae = null, double ratio = 0.2, double offsetC = 0)
		{
			formulae ??= (mean, std) => ratio * std + mean + offsetC;
			var r = bmp.GetData();
			var w = new byte[r.Length];
			int[] offsets = OperationMatrix.Default(bmp.Stride, 3);

			for (int i = bmp.Stride + 3; i < bmp.Length - bmp.Stride - 3; i++)
			{
				double mean = 0.0;
				foreach (int o in offsets)
					mean += r[i + o];
				mean /= offsets.Length;

				double std = 0.0;
				foreach (int o in offsets)
					std += (r[i + o] - mean) * (r[i + o] - mean);
				std /= offsets.Length - 1;
				std = Math.Sqrt(std);

				double result = formulae(mean, std);
				w[i] = r[i] >= result ? byte.MaxValue : byte.MinValue;
			}
			return bmp.SetData(w).Free();
		}

		public static Image Filter(Image bmp, params int[][] matrices)
		{
			var r = bmp.GetData();
			var w = new byte[r.Length];
			int[] offsets = OperationMatrix.Default(bmp.Stride, 3);

			int[] div = new int[matrices.Length];
			for (int i = 0; i < div.Length; i++)
			{
				div[i] = matrices[i].Sum();
				if (div[i] == 0)
					div[i] = 1;
			}

			for (int i = bmp.Stride + 3; i < bmp.Length - bmp.Stride - 3; i++)
			{
				int[] sum = new int[matrices.Length];

				for (int k = 0; k < matrices.Length; k++)
				{
					for (int j = 0; j < offsets.Length; j++)
						sum[k] += r[i + offsets[j]] * matrices[k][j];

					sum[k] /= div[k];

					if (sum[k] > 255) sum[k] = 255;
					if (sum[k] < 0) sum[k] = 0;
				}

				w[i] = (byte)sum.Sum();
			}
			return bmp.SetData(w).Free();
		}

		public static Image MedianFilter(Image bmp) => GetSorted(bmp, 4);
		public static Image Min(Image bmp) => GetSorted(bmp, 0);
		public static Image Max(Image bmp) => GetSorted(bmp, 8);

		private static Image GetSorted(Image bmp, int index)
		{
			var r = bmp.GetData();
			var w = new byte[r.Length];
			int[] offsets = OperationMatrix.Default(bmp.Stride, 3);

			for (int i = bmp.Stride + 3; i < bmp.Length - bmp.Stride - 3; i += 3)
				for (int k = 0; k < 3; k++)
				{
					List<byte> list = new List<byte>();
					foreach (var offset in offsets)
						list.Add(r[i + k + offset]);
					list.Sort();
					w[i + k] = list[4];
				}
			return bmp.SetData(w).Free();
		}

		public static Image Pixelize(Image bmp)
		{
			var bytes = bmp.GetData();
			int[] offsets = OperationMatrix.Default(bmp.Stride, 3);

			int blockWidth = 3;
			int blockHeight = 3;

			for (int y = blockHeight / 2; y < bmp.Height - blockHeight / 2; y += blockHeight)
				for (int x = blockWidth / 2 * 3; x < bmp.Stride - blockWidth / 2 * 3; x += blockWidth * 3)
					for (int k = 0; k < 3; k++)
					{
						int i = x + y * bmp.Stride + k;

						int sum = 0;
						foreach (int o in offsets)
							sum += bytes[i + o];
						sum /= blockHeight * blockWidth;

						if (sum < 0) sum = 0;
						if (sum > 255) sum = 255;

						foreach (int o in offsets)
							bytes[i + o] = (byte)sum;
					}
			return bmp.SetData(bytes).Free();
		}

		public static Image Otsu(Image bmp)
		{
			var bytes = bmp.GetData();
			for (int i = 0; i < bmp.Length; i += 3)
				bytes[i] = bytes[i + 1] = bytes[i + 2] = (byte)(
					(bytes[i] + bytes[i + 1] + bytes[i + 2]) / 3
				);

			int[] histogram = new int[256];
			for (int i = 0; i < bmp.Length; i += 3)
				++histogram[bytes[i]];

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

			for (int i = 0; i < bytes.Length; i++)
				bytes[i] = bytes[i] > threshold ? byte.MaxValue : byte.MinValue;
			return bmp.SetData(bytes).Free();
		}
	}
}
