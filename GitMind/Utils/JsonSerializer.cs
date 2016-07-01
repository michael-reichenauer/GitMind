using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;


namespace GitMind.Utils
{
	public class JsonSerializerX
	{
		private static readonly DataContractJsonSerializerSettings
			Settings = new DataContractJsonSerializerSettings
			{
				DateTimeFormat = new DateTimeFormat("yyyy-MM-dd'T'HH:mm:ssZ")
			};


		public T Deserialize<T>(string text)
		{
			DataContractJsonSerializer serializer = GetSerializer<T>();

			using (Stream stream = StringStream(text))
			{
				return (T)serializer.ReadObject(stream);
			}
		}

		public T Deserialize<T>(Stream stream)
		{
			DataContractJsonSerializer serializer = GetSerializer<T>();

			return (T)serializer.ReadObject(stream);
		}


		public void Serialize<T>(T instance, Stream stream)
		{
			DataContractJsonSerializer serializer = GetSerializer<T>();
			serializer.WriteObject(stream, instance);
		}


		private static DataContractJsonSerializer GetSerializer<T>()
		{
			return new DataContractJsonSerializer(typeof(T), Settings);
		}


		private Stream StringStream(string text)
		{
			return new MemoryStream(Encoding.UTF8.GetBytes(text ?? ""));
		}
	}
}