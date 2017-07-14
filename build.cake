//#tool "nuget:?package=GitReleaseNotes"
#tool nuget:?package=GitVersion.CommandLine

GitVersion versionInfo = null;
var target = Argument("target", "Default");
var outputDir = "./artifacts/";
var configuration   = Argument("configuration", "Release");

Task("Clean")
    .Does(() => {
        if (DirectoryExists(outputDir))
        {
            DeleteDirectory(outputDir, recursive:true);
        }
        CreateDirectory(outputDir);
    });

Task("Version")
    .Does(() => {
        GitVersion(new GitVersionSettings{
            UpdateAssemblyInfo = true,
            OutputType = GitVersionOutput.BuildServer
        });
        versionInfo = GitVersion(new GitVersionSettings{ OutputType = GitVersionOutput.Json });
    });

Task("Restore")
    .IsDependentOn("Version")
    .Does(() => {        
        DotNetCoreRestore(new DotNetCoreRestoreSettings
        {
            ArgumentCustomization = args => args.Append("-p:VersionPrefix=" + versionInfo.MajorMinorPatch)
        });
    });

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .Does(() => {
        // DotNetCoreBuild(".", new DotNetCoreBuildSettings
        // {
        //     Configuration = configuration,
        //     VersionSuffix = versionInfo.PreReleaseTag,
        //     ArgumentCustomization = args => args.Append("-p:VersionPrefix=" + versionInfo.MajorMinorPatch),
        // });

        MSBuild("./TwentyTwenty.MessageBus.Providers.sln", new MSBuildSettings {
            //Verbosity = Verbosity.Minimal,
            //ToolVersion = MSBuildToolVersion.VS2015,
            Configuration = configuration,
        });
    });

Task("Package")
    .IsDependentOn("Build")
    .Does(() => {
        var settings = new DotNetCorePackSettings
        {
            OutputDirectory = outputDir,
            NoBuild = true,
            Configuration = configuration,
            VersionSuffix = versionInfo.PreReleaseTag,
            ArgumentCustomization = args => args.Append("-p:VersionPrefix=" + versionInfo.MajorMinorPatch),
        };

        DotNetCorePack("src/TwentyTwenty.MessageBus.Providers/", settings);
        DotNetCorePack("src/TwentyTwenty.MessageBus.Providers.MassTransit/", settings);

        if (AppVeyor.IsRunningOnAppVeyor)
        {
            foreach (var file in GetFiles(outputDir + "**/*"))
                AppVeyor.UploadArtifact(file.FullPath);
        }
    });

Task("Default")
    .IsDependentOn("Package");

RunTarget(target);