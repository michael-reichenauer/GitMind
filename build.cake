#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0
//#tool nuget:?package=Tools.InnoSetup&version=5.5.9
#tool nuget:?package=Microsoft.VSSDK.Vsixsigntool&version=15.7.27703
#addin nuget:?package=Cake.VersionReader&version=5.0.0
#addin nuget:?package=Cake.VsixSignTool&version=1.2.0

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");


//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define paths.
var name = "GitMind";

var solutionPath = $"./{name}.sln";
var outputPath = $"{name}/bin/{configuration}/{name}.exe";
var setupPath = $"{name}Setup.exe";

string signPassword = "";


//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectories($"./**/obj/{configuration}");
    CleanDirectories($"./**/bin/{configuration}");

    if (FileExists(setupPath))
    { 
        DeleteFile(setupPath);
    }
});


Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    NuGetRestore(solutionPath, new NuGetRestoreSettings {
       Verbosity = NuGetVerbosity.Quiet 
    });
});


Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    if(IsRunningOnWindows())
    {
      // Use MSBuild
      MSBuild(solutionPath, new MSBuildSettings {
        Configuration = configuration,
        Verbosity = Verbosity.Minimal,
        MSBuildPlatform = MSBuildPlatform.x86,
        ArgumentCustomization = args => args.Append("/nologo") 
        }  );
    }
    else
    {
      // Use XBuild
      XBuild(solutionPath, settings =>
        settings.SetConfiguration(configuration));
    }
});


Task("Build-Setup-File")
    .IsDependentOn("Build")
    .Does(() =>
{
    var version = GetFullVersionNumber(outputPath);
	string isSigning = string.IsNullOrWhiteSpace(signPassword) ? "False" : "True";

	CopyFile(outputPath, setupPath);	
});


Task("Build-Setup")
    .IsDependentOn("Clean")
	.IsDependentOn("Prompt-Sign-Password")
	.IsDependentOn("Build")
    .IsDependentOn("Build-Setup-File")
    .Does(() =>
{
	// Sign setup file
	var file = new FilePath(setupPath);
    Sign(file, new SignToolSignSettings {
            TimeStampUri = new Uri("http://timestamp.digicert.com"),
            CertPath = @"C:\Users\micha\OneDrive\CodeSigning\SignCert.pfx",
            Password = signPassword
    });
	
	var version = GetFullVersionNumber(outputPath);
    Version v = Version.Parse(version);
    string shortVersion = string.Format("{0}.{1}", v.Major, v.Minor);

    Information("v{0}", version); 
    Information("Version {0} beta", shortVersion); 

    Information("\n\n");  
})
.OnError(exception =>
{
	RunTarget("Clean");
	throw exception;
});;


Task("Build-Unsigned-Setup")
    .IsDependentOn("Build-Setup-File")
    .Does(() =>
{
	Warning("\nSetup file is not signed !!!");
	Error("----------------------------\n\n");
});


Task("Prompt-Sign-Password")
    .Does(() =>
{
	if(Environment.UserInteractive)
	{
		Console.WriteLine("Enter password for signing setup file:");
		signPassword = "";
		ConsoleKeyInfo key;
		do
		{
			key = Console.ReadKey(true);
			if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
			{
				signPassword += key.KeyChar;
				Console.Write("*");
			}
			else
			{
				if (key.Key == ConsoleKey.Backspace && signPassword.Length > 0)
				{
					signPassword = signPassword.Substring(0, (signPassword.Length - 1));
					Console.Write("\b \b");
				}
			}
		}
		while (key.Key != ConsoleKey.Enter);
        Information(" ");

		if (string.IsNullOrWhiteSpace(signPassword))
		{
			throw new Exception("Invalid sign password");
		}
	}
});


Task("Run-Unit-Tests")
    .IsDependentOn("Build")
    .Does(() =>
{
    NUnit3($"./**/bin/{configuration}/*Test.dll", new NUnit3Settings {
        NoResults = true,
        NoHeader = true,
        });
});


Task("Default")
    .IsDependentOn("Run-Unit-Tests");



//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
