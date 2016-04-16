using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;


namespace GitMind.Utils
{
	public class JsonSerializer
	{
		public T Deserialize<T>(string text)
		{
			DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));

			using (Stream stream = StringStream(text))
			{
				return (T)serializer.ReadObject(stream);
			}
		}


		private Stream StringStream(string text)
		{
			return new MemoryStream(Encoding.UTF8.GetBytes(text ?? ""));
		}
	}
}