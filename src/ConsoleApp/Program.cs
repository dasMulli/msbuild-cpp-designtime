using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Locator;
using Microsoft.Build.Logging;
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
            MSBuildLocator.RegisterDefaults();

            var baseDir = Path.GetFullPath(@"..\..\..\..\");

            var logBuilder = new StringBuilder();
            var logger = new ConsoleLogger(LoggerVerbosity.Normal, x => logBuilder.Append(x), null, null);

            var cppProjectPath =  Path.Combine(baseDir, @"LockTools\LockTools.vcxproj");

            var globalProps = new Dictionary<string, string>()
            {
                { "BuildProjectReferences", "false" },
            };

            var projectInstance = new ProjectInstance(cppProjectPath, globalProps, null);
            var requestData = new BuildRequestData(projectInstance, new[] { "Clean", "Build" });

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

            var targetResult = buildResult.ResultsByTarget["Build"];
            var artifact = targetResult.Items.Single().ToString();
            var artifactPdb = Path.ChangeExtension(artifact, ".pdb");
        }
    }
}
