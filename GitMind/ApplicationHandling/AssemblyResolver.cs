using System;
using System.IO;
using System.Reflection;
using GitMind.Utils;


namespace GitMind.ApplicationHandling
{
	internal class AssemblyResolver
	{
		public static void Activate() => AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;


		private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
		{
			try
			{
				Assembly executingAssembly = Assembly.GetExecutingAssembly();
				string name = executingAssembly.FullName.Split(',')[0];
				string resolveName = args.Name.Split(',')[0];
				string resourceName = $"{name}.Dependencies.{resolveName}.dll";

				// Load the requested assembly from the resources
				using (Stream stream = executingAssembly.GetManifestResourceStream(resourceName))
				{
					if (stream == null)
					{
						if (resolveName != "GitMind.resources")
						{
							Log.Error($"Failed to resolve assembly {resolveName}");
						}

						return null;
					}

					long bytestreamMaxLength = stream.Length;
					byte[] buffer = new byte[bytestreamMaxLength];
					stream.Read(buffer, 0, (int)bytestreamMaxLength);
					Log.Debug($"Resolved {resolveName}");
					return Assembly.Load(buffer);
				}
			}
			catch (Exception e)
			{
				Log.Exception(e, "Failed to load");
				throw;
			}
		}
	}
}