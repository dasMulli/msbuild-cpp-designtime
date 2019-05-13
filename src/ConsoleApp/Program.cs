
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ConsoleApp
{
    /// <summary>
    /// Inspired by:
    /// https://daveaglick.com/posts/running-a-design-time-build-with-msbuild-apis
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            var logBuilder = new StringBuilder();
            var logger = new ConsoleLogger(LoggerVerbosity.Normal, x => logBuilder.Append(x), null, null);

            // TODO: Fix this
            var projectPath = @"C:\Users\Joao\source\repos\MsBuildCpp\src\LockTools\LockTools.vcxproj";
            var project = GetProject(projectPath, logger);

            var projectInstance = project.CreateProjectInstance();

            var requestData = new BuildRequestData(projectInstance, new[] { "Build" });

            var par = new BuildParameters()
            {
                DetailedSummary = true,
                Loggers = new[] { logger },
            };

            var buildResult = BuildManager.DefaultBuildManager.Build(par, requestData);

            if (buildResult.OverallResult != BuildResultCode.Success)
            {
                Console.WriteLine(logBuilder);
                throw new Exception($"Overall build result: {buildResult.OverallResult}");
            }

            // Why is this empty on 15.0? - Investigate
            var targetResult = buildResult.ResultsByTarget["Build"];
            var artifact = targetResult.Items.Single().ToString();
            var artifactPdb = Path.ChangeExtension(artifact, ".pdb");
        }

        public static Project GetProject(string projectPath, ConsoleLogger logger)
        {
            string toolsPath = GetToolsPath();
            Dictionary<string, string> globalProperties = GetGlobalProperties(projectPath, toolsPath);
            ProjectCollection projectCollection = new ProjectCollection(globalProperties);
            projectCollection.AddToolset(new Toolset(ToolLocationHelper.CurrentToolsVersion, toolsPath, projectCollection, string.Empty));
            projectCollection.RegisterLogger(logger);
            return projectCollection.LoadProject(projectPath);
        }

        public static string GetToolsPath()
        {
            string toolsPath = ToolLocationHelper.GetPathToBuildToolsFile("msbuild.exe", ToolLocationHelper.CurrentToolsVersion);
            if (string.IsNullOrEmpty(toolsPath))
            {
                toolsPath = PollForToolsPath().FirstOrDefault();
            }
            if (string.IsNullOrEmpty(toolsPath))
            {
                throw new Exception("Could not locate the tools (MSBuild) path.");
            }
            return Path.GetDirectoryName(toolsPath);
        }

        public static string[] PollForToolsPath()
        {
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

            return new[]
            {
                Path.Combine(programFilesX86, @"Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe"),
                Path.Combine(programFilesX86, @"Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe"),
                Path.Combine(programFilesX86, @"Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"),
                Path.Combine(programFilesX86, @"MSBuild\14.0\Bin\MSBuild.exe"),
                Path.Combine(programFilesX86, @"MSBuild\12.0\Bin\MSBuild.exe")
            }.Where(File.Exists).ToArray();
        }

        public static Dictionary<string, string> GetGlobalProperties(string projectPath, string toolsPath)
        {
            string solutionDir = Path.GetFullPath(Path.Combine(projectPath, @"..\..\..\"));

            string extensionsPath = Path.GetFullPath(Path.Combine(toolsPath, @"..\..\"));

            // TODO: Fix this!
            var vcTools = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\VC\VCTargets\";

            string sdksPath = Path.Combine(extensionsPath, "Sdks");
            string roslynTargetsPath = Path.Combine(toolsPath, "Roslyn");

            return new Dictionary<string, string>
            {
                { "SolutionDir", solutionDir },
                { "MSBuildExtensionsPath", extensionsPath },
                { "MSBuildSDKsPath", sdksPath },
                { "VCTargetsPath", vcTools }
            };
        }
    }
}
