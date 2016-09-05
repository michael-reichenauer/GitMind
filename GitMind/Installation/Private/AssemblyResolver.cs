using System;
using System.IO;
using System.Reflection;
using GitMind.Utils;


namespace GitMind.Installation.Private
{
	internal class AssemblyResolver
	{
		private static readonly string GitBinaryDll = "git2-785d8c4.dll";


		public static void Activate()
		{
			AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
		}


		private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
		{
			try
			{
				Assembly executingAssembly = Assembly.GetExecutingAssembly();
				string name = executingAssembly.FullName.Split(',')[0];
				string resolveName = args.Name.Split(',')[0];
				string resourceName = $"{name}.Dependencies.{resolveName}.dll";
				
				if (resolveName == "LibGit2Sharp")
				{				
					HandleLibGit2SharpDependency(executingAssembly, name);
				}

				// Load the requested assembly from the resources
				using (Stream stream = executingAssembly.GetManifestResourceStream(resourceName))
				{
					if (stream == null)
					{
						if (resolveName != "GitMind.resources")
						{
							Log.Warn($"Failed to resolve assembly {resolveName}");
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
				Log.Error($"Failed to load, {e}");
				throw;
			}
		}


		private static void HandleLibGit2SharpDependency(Assembly executingAssembly, string name)
		{
			string directoryName = Path.GetDirectoryName(executingAssembly.Location);
			string targetPath = Path.Combine(directoryName, GitBinaryDll);
			if (!File.Exists(targetPath))
			{
				string gitResourceName = $"{name}.Dependencies.{GitBinaryDll}";
				Log.Debug($"Trying to extract {gitResourceName} and write {targetPath}");
				using (Stream stream = executingAssembly.GetManifestResourceStream(gitResourceName))
				{
					if (stream == null)
					{
						Log.Error($"Failed to read {gitResourceName}");
						throw new InvalidOperationException("Failed to extract dll" + gitResourceName);
					}

					long bytestreamMaxLength = stream.Length;
					byte[] buffer = new byte[bytestreamMaxLength];
					stream.Read(buffer, 0, (int)bytestreamMaxLength);
					File.WriteAllBytes(targetPath, buffer);
					Log.Debug($"Extracted {targetPath}");
				}
			}
		}
	}
}