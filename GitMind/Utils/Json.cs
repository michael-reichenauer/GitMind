using Newtonsoft.Json;


namespace GitMind.Utils
{
	public static class Json
	{
		private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
		{
			Formatting = Formatting.Indented,
		};

		public static string AsJson<T>(T obj) => JsonConvert.SerializeObject(obj, typeof(T), Settings);

		public static T As<T>(string json) => JsonConvert.DeserializeObject<T>(json, Settings);
	}
}