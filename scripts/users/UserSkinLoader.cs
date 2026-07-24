using Godot;
using System.Collections;

public static class UserSkinLoader
{
	public enum SkinError: byte
	{
		Ok,
		Error,
		FileSizeTooBig,
		ImageTooBig,
	}

	public struct RawResult
	{
		public Image Image;
		public SkinError Error;
	}

	public static RawResult LoadRawFromFile(string path)
	{
		var result = new RawResult();
		result.Error = SkinError.Ok;

		var image = new Image();

		Error error = image.Load(path);
		if (error != Error.Ok)
		{
			result.Error = SkinError.Error;
			return result;
		}
		
		result.Image = image;

		return result;
	}


}
