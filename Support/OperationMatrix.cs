namespace WpfApp
{
	public static class OperationMatrix
	{
		public static int[] Default(int stride, int channels) => new[]
		{
			-channels - stride, 0 - stride, channels - stride,
			-channels         , 0         , channels         ,
			-channels + stride, 0 + stride, channels + stride,
		};
	}
}
