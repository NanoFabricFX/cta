using CTA.Rules.Config;
using CTA.Rules.Models;
using CTA.Rules.RuleFiles;
using CTA.Rules.Update;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CTA.Rules.Test
{
    public class CTATests : AwsRulesBaseTest
    {
        public string tempDir = "";
        public string downloadLocation;

        [SetUp]
        public void Setup()
        {
            Setup(this.GetType());
            tempDir = GetTstPath(Path.Combine(new string[] { "Projects", "Temp" }));
            DownloadTestProjects();
        }

        private void DownloadTestProjects()
        {
            var tempDirectory = Directory.CreateDirectory(tempDir);
            downloadLocation = Path.Combine(tempDirectory.FullName, "d");

            var fileName = Path.Combine(tempDirectory.Parent.FullName, @"TestProjects.zip");
            Utils.SaveFileFromGitHub(fileName, GithubInfo.TestGithubOwner, GithubInfo.TestGithubRepo, GithubInfo.TestGithubTag);
            ZipFile.ExtractToDirectory(fileName, downloadLocation, true);
        }

        [Test]
        public void TestCli()
        {
            string mvcMusicStorePath = Directory.EnumerateFiles(tempDir, "MvcMusicStore.sln", SearchOption.AllDirectories).FirstOrDefault();
            string mvcMusicStoreProjectPath = Directory.EnumerateFiles(tempDir, "MvcMusicStore.csproj", SearchOption.AllDirectories).FirstOrDefault();
            string[] args = { "-p", mvcMusicStoreProjectPath, "-s", mvcMusicStorePath, "-r", Path.Combine(tempDir, "Input"), "-a", "", "-m", "true" };
            var cli = new RulesCli();
            cli.HandleCommand(args);
            Assert.NotNull(cli);
            Assert.NotNull(cli.Project);
            Assert.NotNull(cli.FilePath);
            Assert.NotNull(cli.AssembliesDir);
            Assert.NotNull(cli.IsMockRun);            

        }

        [Test]
        public void TestSampleWebApiSolution()
        {
            //We don't care about version for CTA-only rules:
            string version = "net5.0";

            var sourceDir = Directory.GetParent(Directory.EnumerateFiles(downloadLocation, "WebApiWithReferences.sln", SearchOption.AllDirectories).FirstOrDefault());
            var solutionDir = Path.Combine(tempDir, version);

            CopyDirectory(sourceDir, new DirectoryInfo(solutionDir));
            string solutionPath = Directory.EnumerateFiles(solutionDir, "WebApiWithReferences.sln", SearchOption.AllDirectories).FirstOrDefault();

            FileAssert.Exists(solutionPath);

            //Sample Web API has only one project:
            string projectFile = Directory.EnumerateFiles(solutionDir, "WebApiWithReferences.csproj", SearchOption.AllDirectories).FirstOrDefault();
            FileAssert.Exists(projectFile);

            ProjectConfiguration projectConfiguration = new ProjectConfiguration()
            {
                ProjectPath = projectFile,
                TargetVersions = new List<string> { version },
                RulesPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CTAFiles"))
            };

            List<ProjectConfiguration> solutionConfiguration = new List<ProjectConfiguration>();
            solutionConfiguration.Add(projectConfiguration);

            SolutionRewriter solutionRewriter = new SolutionRewriter(solutionPath, solutionConfiguration);
            var analysisRunResult = solutionRewriter.AnalysisRun();
            StringBuilder str = new StringBuilder();
            foreach (var k in analysisRunResult.Keys)
            {
                str.AppendLine(k);
                str.AppendLine(analysisRunResult[k].ToString());
            }


            var analysisResult = str.ToString();
            StringAssert.Contains("IActionResult", analysisResult);


            solutionRewriter.Run(analysisRunResult);

            string projectDir = Directory.GetParent(projectFile).FullName;

            var homeControllerText = File.ReadAllText(Path.Combine(projectDir, "Controllers", "HouseController.cs"));
            var iHouseRepositoryText = File.ReadAllText(Path.Combine(projectDir, "Repositories", "IHouseRepository.cs"));
            //Check that namespace has been added
            StringAssert.Contains(@"Microsoft.AspNetCore.Mvc", homeControllerText);

            //Check that attribute has been added to class, and base class changed:
            StringAssert.Contains(@"[ApiController]", homeControllerText);
            StringAssert.Contains(@"ControllerBase", homeControllerText);

            //Check that invocation expression was replaced:
            StringAssert.Contains(@"this.Response.Headers.Add", homeControllerText);

            //Check that identifier as replaced:
            StringAssert.Contains(@"IActionResult", homeControllerText);

            //Interface Tests
            StringAssert.Contains(@"AnyMethod", iHouseRepositoryText);
            StringAssert.Contains(@"using System;", iHouseRepositoryText);
            StringAssert.Contains(@"[Authorize]", iHouseRepositoryText);
            StringAssert.DoesNotContain(@"Delete", iHouseRepositoryText);
            StringAssert.Contains(@"IChangedHouseRepository", iHouseRepositoryText);
            StringAssert.Contains(@"This is a commented interface", iHouseRepositoryText);
            StringAssert.DoesNotContain(@"[Description(", iHouseRepositoryText);

        }

        [Test]
        public void TestMvcMusicStore()
        {
            //We don't care about version for CTA-only rules:
            string version = "net5.0";

            var sourceDir = Directory.GetParent(Directory.EnumerateFiles(downloadLocation, "MvcMusicStore.sln", SearchOption.AllDirectories).FirstOrDefault());
            var solutionDir = Path.Combine(tempDir, version);

            CopyDirectory(sourceDir, new DirectoryInfo(solutionDir));
            string solutionPath = Directory.EnumerateFiles(solutionDir, "MvcMusicStore.sln", SearchOption.AllDirectories).FirstOrDefault();
            FileAssert.Exists(solutionPath);


            //Sample Web API has only one project:
            string projectFile = Directory.EnumerateFiles(solutionDir, "*.csproj", SearchOption.AllDirectories).FirstOrDefault();
            FileAssert.Exists(projectFile);

            ProjectConfiguration projectConfiguration = new ProjectConfiguration()
            {
                ProjectPath = projectFile,
                TargetVersions = new List<string> { version },
                RulesPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CTAFiles"))
            };

            List<ProjectConfiguration> solutionConfiguration = new List<ProjectConfiguration>();
            solutionConfiguration.Add(projectConfiguration);

            SolutionRewriter solutionRewriter = new SolutionRewriter(solutionPath, solutionConfiguration);
            var analysisRunResult = solutionRewriter.AnalysisRun();
            StringBuilder str = new StringBuilder();
            foreach (var k in analysisRunResult.Keys)
            {
                str.AppendLine(k);
                str.AppendLine(analysisRunResult[k].ToString());
            }

            var analysisResult = str.ToString();
            StringAssert.Contains("HtmlEncoder", analysisResult);

            solutionRewriter.Run(analysisRunResult);

            string projectDir = Directory.GetParent(projectFile).FullName;

            var accountControllerText = File.ReadAllText(Path.Combine(projectDir, "Controllers", "AccountController.cs"));
            var checkoutControllerText = File.ReadAllText(Path.Combine(projectDir, "Controllers", "CheckoutController.cs"));
            var shoppingCartControllerText = File.ReadAllText(Path.Combine(projectDir, "Controllers", "ShoppingCartController.cs"));
            var storeManagerControllerText = File.ReadAllText(Path.Combine(projectDir, "Controllers", "StoreManagerController.cs"));
            var musicStoreEntitiesText = File.ReadAllText(Path.Combine(projectDir, "Models", "MusicStoreEntities.cs"));
            var shoppingCartText = File.ReadAllText(Path.Combine(projectDir, "Models", "ShoppingCart.cs"));

            var shoppingCartRemoveViewModel = File.ReadAllText(Path.Combine(projectDir, "ViewModels", "ShoppingCartRemoveViewModel.cs"));
            var shoppingCartViewModel = File.ReadAllText(Path.Combine(projectDir, "ViewModels", "ShoppingCartViewModel.cs"));


            //Check that namespace was added
            StringAssert.Contains(@"Microsoft.AspNetCore.Mvc", accountControllerText);

            //Check that namespace was added
            StringAssert.Contains(@"Microsoft.AspNetCore.Mvc", checkoutControllerText);

            //Check that method was replaced
            StringAssert.Contains(@"HtmlEncoder.Default.Encode", shoppingCartControllerText);

            //Check that namespaces were added
            StringAssert.Contains(@"Microsoft.AspNetCore.Mvc", storeManagerControllerText);
            StringAssert.Contains(@"Microsoft.EntityFrameworkCore", storeManagerControllerText);

            //Check that namespace was added
            StringAssert.Contains(@"OnConfiguring", musicStoreEntitiesText);
            StringAssert.Contains(@"Microsoft.EntityFrameworkCore", musicStoreEntitiesText);

            //Check that httpcontext is added (with a space) to make sure it's not httpcontextbase
            StringAssert.Contains(@"HttpContext ", shoppingCartText);

            //Test change attribute name
            StringAssert.Contains(@"ChangedChildActionOnly", shoppingCartControllerText);

            //Test change namespace name
            StringAssert.Contains(@"MvcMusicStore.ChangedViewModel", shoppingCartRemoveViewModel);
            StringAssert.Contains(@"MvcMusicStore.ChangedViewModel", shoppingCartViewModel);

            StringAssert.Contains(@"using System;", shoppingCartControllerText);
            StringAssert.Contains(@"int PickRandomNumberRange", shoppingCartControllerText);
            StringAssert.Contains(@"Something.Something()", shoppingCartControllerText);
            StringAssert.DoesNotContain(@"new MusicStoreEntities()", shoppingCartControllerText);
            StringAssert.Contains(@"SomethingElse storeDB", shoppingCartControllerText);
        }

        [Test]
        public void ConvertHierarchicalToNamespaceFile()
        {
            var dir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CTAFiles");
            var consolidatedFile = Directory.EnumerateFiles(dir, "consolidated.json").FirstOrDefault();
            FileAssert.Exists(consolidatedFile);

            RulesFileExport rulesFileExport = new RulesFileExport(consolidatedFile);
            rulesFileExport.Run();

            FileAssert.Exists(Path.Combine(dir, "microsoft.sqlserver.types.json"));
            FileAssert.Exists(Path.Combine(dir, "system.data.entity.json"));
            FileAssert.Exists(Path.Combine(dir, "system.data.sqlclient.json"));
            FileAssert.Exists(Path.Combine(dir, "system.runtime.caching.json"));
            FileAssert.Exists(Path.Combine(dir, "system.web.http.json"));
            FileAssert.Exists(Path.Combine(dir, "system.web.http.odata.json"));
            FileAssert.Exists(Path.Combine(dir, "system.web.json"));
            FileAssert.Exists(Path.Combine(dir, "system.web.mvc.json"));
        }

        [Test]
        public void LoggerTest()
        {
            LogHelper.LogError(new Exception("Test message 1"));
            LogHelper.LogError("Test message 1");
            LogHelper.LogDebug("Test message 1");
            LogHelper.LogWarning("Test message 1");
            LogHelper.LogInformation("Test message 1");
            LogHelper.Logger = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Debug)).CreateLogger(Constants.Translator);
        }

        [TearDown]
        public void Cleanup()
        {
            Directory.Delete(GetTstPath(Path.Combine(new string[] { "Projects", "Temp" })), true);
        }
    }
}