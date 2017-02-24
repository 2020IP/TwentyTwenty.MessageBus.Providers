#tool "nuget:?package=GitReleaseNotes"
#tool "nuget:?package=GitVersion.CommandLine"

var target = Argument("target", "Default");
var outputDir = "./artifacts/";
var providersProjectJson = "./src/TwentyTwenty.MessageBus.Providers/project.json";
var providersMtProjectJson = "./src/TwentyTwenty.MessageBus.Providers.MassTransit/project.json";

Task("Clean")
    .Does(() => {
        if (DirectoryExists(outputDir))
        {
            DeleteDirectory(outputDir, recursive:true);
        }
        CreateDirectory(outputDir);
    });

Task("Restore")
    .Does(() => {
        DotNetCoreRestore("src");
    });

GitVersion versionInfo = null;
Task("Version")
    .Does(() => {
        GitVersion(new GitVersionSettings{
            UpdateAssemblyInfo = true,
            OutputType = GitVersionOutput.BuildServer
        });
        versionInfo = GitVersion(new GitVersionSettings{ OutputType = GitVersionOutput.Json });

        // Update project.json
        var updatedProvidersProjectJson = System.IO.File.ReadAllText(providersProjectJson)
            .Replace("1.0.0-*", versionInfo.NuGetVersion);
        var updatedProvidersMtProjectJson = System.IO.File.ReadAllText(providersMtProjectJson)
            .Replace("1.0.0-*", versionInfo.NuGetVersion);

        System.IO.File.WriteAllText(providersProjectJson, updatedProvidersProjectJson);
        System.IO.File.WriteAllText(providersMtProjectJson, updatedProvidersMtProjectJson);
    });

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Version")
    .IsDependentOn("Restore")
    .Does(() => {
        DotNetCoreBuild(providersMtProjectJson);
    });

Task("Package")
    .IsDependentOn("Build")
    .Does(() => {
        //GitLink("./", new GitLinkSettings { ArgumentCustomization = args => args.Append("-include Specify,Specify.Autofac") });

        //GenerateReleaseNotes();

        var settings = new DotNetCorePackSettings
        {
            OutputDirectory = outputDir,
            NoBuild = true
        };

        DotNetCorePack(providersMtProjectJson, settings);

        System.IO.File.WriteAllLines(outputDir + "artifacts", new[]{
            "nuget:TwentyTwenty.BaseLine." + versionInfo.NuGetVersion + ".nupkg",
            "nugetSymbols:TwentyTwenty.BaseLine." + versionInfo.NuGetVersion + ".symbols.nupkg",
        //    "releaseNotes:releasenotes.md"
        });

        if (AppVeyor.IsRunningOnAppVeyor)
        {
            foreach (var file in GetFiles(outputDir + "**/*"))
                AppVeyor.UploadArtifact(file.FullPath);
        }
    });

Task("Default")
    .IsDependentOn("Package");

private void GenerateReleaseNotes()
{
    var settings = new GitReleaseNotesSettings
    {
        WorkingDirectory = ".",        
    };

    GitReleaseNotes("./artifacts/releasenotes.md", settings);
}
    

RunTarget(target);