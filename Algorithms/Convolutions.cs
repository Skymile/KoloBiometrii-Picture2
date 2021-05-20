namespace WpfApp
{
	public static class Convolutions
	{
		public static readonly int[] Sharpen = new[]
		{
			0 , -1,  0,
			-1,  5, -1,
			0 , -1,  0
		};

		public static readonly int[] BoxBlur = new[]
		{
			1, 1, 1,
			1, 1, 1,
			1, 1, 1
		};

		public static readonly int[] GaussianBlur = new[]
		{
			1, 2, 1,
			2, 4, 2,
			1, 2, 1
		};

		public static readonly int[] Emboss = new[]
		{
			0, 0, 0,
			0, 1, 0,
			-2, 0, 0
		};

		public static readonly int[] SobelHorizontal = new[]
		{
			1, 2, 1,
			0, 0, 0,
			-1, -2, -1
		};

		public static readonly int[] SobelVertical = new[]
		{
			1, 0, -1,
			2, 0, -2,
			1, 0, -1
		};
	}
}
// 438