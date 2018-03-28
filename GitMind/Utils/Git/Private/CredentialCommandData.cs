namespace GitMind.Utils.Git.Private
{
	internal class CredentialCommandData
	{
		public string Protocol { get; }
		public string Host { get; }
		public string Username { get; }
		public string Password { get; }

		public string Url => !string.IsNullOrEmpty(Protocol) && !string.IsNullOrEmpty(Host)
			? $"{Protocol}://{Host}"
			: null;

		private CredentialCommandData(string protocol, string host, string username, string password)
		{
			Protocol = protocol;
			Host = host;
			Username = username;
			Password = password;
		}

		public static CredentialCommandData Parse(string commandData)
		{
			string protocol = null;
			string host = null;
			string username = null;
			string password = null;

			foreach (string line in commandData.Split("\n".ToCharArray()))
			{
				string[] parts = line.Split("=".ToCharArray());

				switch (parts[0])
				{
					case "protocol":
						protocol = parts[1];
						break;
					case "host":
						host = parts[1];
						break;
					case "username":
						username = parts[1];
						break;
					case "password":
						password = parts[1];
						break;
				}
			}

			return new CredentialCommandData(protocol, host, username, password);
		}
	}
}