﻿using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Codelyzer.Analysis;
using CTA.FeatureDetection.Common.Models;
using CTA.FeatureDetection.Tests.Utils;
using CTA.Rules.Config;
using NUnit.Framework;

namespace CTA.FeatureDetection.Tests
{
    [SetUpFixture]
    public class TestProjectsSetupFixture
    {
        private string _tempDirName;
        private string _tempBaseDir;
        private static string _targetDir;
        private static readonly string _coreMvcDir = "CoreMVC";
        private static readonly string _coreWebApiDir = "CoreWebApi";
        private static readonly string _efDir = "Ef";
        private static readonly string _mvcDir = "Mvc";
        private static readonly string _webApiDir = "WebApi";
        private static readonly string _webClassLibraryDir = "WebClassLibrary";
        private static readonly string TestProjectDirectory = TestUtils.GetTestAssemblySourceDirectory(typeof(TestUtils));
        public static string ConfigFile => Path.Combine(TestProjectDirectory, "Examples", "Input", "feature_config.json");

        public static string CoreMvcSolutionName => "CoreMVC.sln";
        public static string CoreWebApiSolutionName => "CoreWebApi.sln";
        public static string EfSolutionName => "EF6_Test.sln";
        public static string MvcSolutionName => "ASP.NET-MVC-Framework.sln";
        public static string WebApiSolutionName => "WebApi-Framework.sln";
        public static string WebClassLibrarySolutionName => "WebClassLibrary.sln";
        public static string CoreMvcSolutionPath => Path.Combine(_targetDir, _coreMvcDir, CoreMvcSolutionName);
        public static string CoreWebApiSolutionPath => Path.Combine(_targetDir, _coreWebApiDir, CoreWebApiSolutionName);
        public static string EfSolutionPath => Path.Combine(_targetDir, _efDir, EfSolutionName);
        public static string MvcSolutionPath => Path.Combine(_targetDir, _mvcDir, MvcSolutionName);
        public static string WebApiSolutionPath => Path.Combine(_targetDir, _webApiDir, WebApiSolutionName);
        public static string WebClassLibrarySolutionPath => Path.Combine(_targetDir, _webClassLibraryDir, WebClassLibrarySolutionName);
        public static string CoreMvcProjectPath => Path.Combine(_targetDir, _coreMvcDir, "CoreMVC", "CoreMVC.csproj");
        public static string CoreWebApiProjectPath => Path.Combine(_targetDir, _coreWebApiDir, "CoreWebApi", "CoreWebApi.csproj");
        public static string Ef6ProjectPath => Path.Combine(_targetDir, _efDir, "EF6_Test", "EF6_Test.csproj");
        public static string MvcProjectPath => Path.Combine(_targetDir, _mvcDir, "ASP.NET-MVC-Framework", "ASP.NET-MVC-Framework.csproj");
        public static string WebApiProjectPath => Path.Combine(_targetDir, _webApiDir, "WebApi-Framework", "WebApi-Framework.csproj");
        public static string WebClassLibraryProjectPath => Path.Combine(_targetDir, _webClassLibraryDir, "WebClassLibrary", "WebClassLibrary.csproj");
        public static IEnumerable<AnalyzerResult> CoreMvcAnalyzerResults { get; private set; }
        public static IEnumerable<AnalyzerResult> CoreWebApiAnalyzerResults { get; private set; }
        public static IEnumerable<AnalyzerResult> EfAnalyzerResults { get; private set; }
        public static IEnumerable<AnalyzerResult> MvcAnalyzerResults { get; private set; }
        public static IEnumerable<AnalyzerResult> WebApiAnalyzerResults { get; private set; }
        public static IEnumerable<AnalyzerResult> WebClassLibraryAnalyzerResults { get; private set; }
        public static FeatureDetector FeatureDetector { get; private set; }
        public static FeatureDetectionResult CoreMvcFeatureDetectionResult { get; private set; }
        public static FeatureDetectionResult CoreWebApiFeatureDetectionResult { get; private set; }
        public static FeatureDetectionResult Ef6FeatureDetectionResult { get; private set; }
        public static FeatureDetectionResult MvcFeatureDetectionResult { get; private set; }
        public static FeatureDetectionResult WebApiFeatureDetectionResult { get; private set; }
        public static FeatureDetectionResult WebClassLibraryFeatureDetectionResult { get; private set; }

        [OneTimeSetUp]
        public void DownloadTestProjects()
        {
            _tempDirName = Path.Combine("Projects", "Temp");
            _tempBaseDir = Path.Combine(TestUtils.GetTstPath(), _tempDirName);
            var tempDownloadDir = Path.Combine(_tempBaseDir, "d");

            // Download test solutions
            DownloadTestProjects(tempDownloadDir);

            // Get directory of each solution
            var tempCoreMvcSolutionDir = GetSolutionDir(tempDownloadDir, CoreMvcSolutionName);
            var tempCoreWebApiSolutionDir = GetSolutionDir(tempDownloadDir, CoreWebApiSolutionName);
            var tempEfSolutionDir = GetSolutionDir(tempDownloadDir, EfSolutionName);
            var tempMvcSolutionDir = GetSolutionDir(tempDownloadDir, MvcSolutionName);
            var tempWebApiSolutionDir = GetSolutionDir(tempDownloadDir, WebApiSolutionName);
            var tempWebClassLibrarySolutionDir = GetSolutionDir(tempDownloadDir, WebClassLibrarySolutionName);
            
            // Copy solutions to a directory with a shorter path
            var destDir = "dest";
            _targetDir = Path.Combine(_tempBaseDir, destDir);
            TestUtils.CopyDirectory(tempCoreMvcSolutionDir, new DirectoryInfo(Path.Combine(_targetDir, _coreMvcDir)));
            TestUtils.CopyDirectory(tempCoreWebApiSolutionDir, new DirectoryInfo(Path.Combine(_targetDir, _coreWebApiDir)));
            TestUtils.CopyDirectory(tempEfSolutionDir, new DirectoryInfo(Path.Combine(_targetDir, _efDir)));
            TestUtils.CopyDirectory(tempMvcSolutionDir, new DirectoryInfo(Path.Combine(_targetDir, _mvcDir)));
            TestUtils.CopyDirectory(tempWebApiSolutionDir, new DirectoryInfo(Path.Combine(_targetDir, _webApiDir)));
            TestUtils.CopyDirectory(tempWebClassLibrarySolutionDir, new DirectoryInfo(Path.Combine(_targetDir, _webClassLibraryDir)));

            // Run source code analysis
            CoreMvcAnalyzerResults = AnalyzerResultsFactory.GetAnalyzerResults(CoreMvcSolutionPath);
            CoreWebApiAnalyzerResults = AnalyzerResultsFactory.GetAnalyzerResults(CoreWebApiSolutionPath);
            EfAnalyzerResults = AnalyzerResultsFactory.GetAnalyzerResults(EfSolutionPath);
            MvcAnalyzerResults = AnalyzerResultsFactory.GetAnalyzerResults(MvcSolutionPath);
            WebApiAnalyzerResults = AnalyzerResultsFactory.GetAnalyzerResults(WebApiSolutionPath);
            WebClassLibraryAnalyzerResults = AnalyzerResultsFactory.GetAnalyzerResults(WebClassLibrarySolutionPath);

            // Detect features in each solution
            FeatureDetector = new FeatureDetector(ConfigFile);
            CoreMvcFeatureDetectionResult = FeatureDetector.DetectFeaturesInProjects(CoreMvcAnalyzerResults)[CoreMvcProjectPath];
            CoreWebApiFeatureDetectionResult = FeatureDetector.DetectFeaturesInProjects(CoreWebApiAnalyzerResults)[CoreWebApiProjectPath];
            Ef6FeatureDetectionResult = FeatureDetector.DetectFeaturesInProjects(EfAnalyzerResults)[Ef6ProjectPath];
            MvcFeatureDetectionResult = FeatureDetector.DetectFeaturesInProjects(MvcAnalyzerResults)[MvcProjectPath];
            WebApiFeatureDetectionResult = FeatureDetector.DetectFeaturesInProjects(WebApiAnalyzerResults)[WebApiProjectPath];
            WebClassLibraryFeatureDetectionResult = FeatureDetector.DetectFeaturesInProjects(WebClassLibraryAnalyzerResults)[WebClassLibraryProjectPath];
        }

        [OneTimeTearDown]
        public void DeleteDownloadedProjects()
        {
            Directory.Delete(_tempBaseDir, true);
        }

        private DirectoryInfo GetSolutionDir(string startDirectory, string solutionName)
        {
            return Directory.GetParent(Directory.EnumerateFiles(startDirectory, solutionName, SearchOption.AllDirectories).FirstOrDefault());
        }

        private void DownloadTestProjects(string tempDir)
        {
            var tempDirectory = Directory.CreateDirectory(tempDir);
            var downloadLocation = Path.Combine(tempDirectory.FullName, "d");

            var fileName = Path.Combine(tempDirectory.Parent.FullName, @"TestProjects.zip");
            Rules.Config.Utils.SaveFileFromGitHub(fileName, GithubInfo.TestGithubOwner, GithubInfo.TestGithubRepo, GithubInfo.TestGithubTag);
            ZipFile.ExtractToDirectory(fileName, downloadLocation, true);
        }
    }
}