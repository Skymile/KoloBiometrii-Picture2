using System.Drawing;
using System.Drawing.Imaging;

namespace WpfApp
{
	public static class BitmapExtensions
	{
		public static BitmapData LockBits(this Bitmap bitmap, ImageLockMode lockMode) =>
			bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), lockMode, PixelFormat.Format24bppRgb);
	}
}
