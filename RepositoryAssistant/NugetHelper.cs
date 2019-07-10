using Microsoft.Build.Evaluation;
using NuGet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RepositoryAssistant
{
    class NugetHelper
    {
        public static void CheckAllProjectDependencies()
        {
            var rootDir = RegistrySettings.ReadValue("Root directory", "RootDir");
            var csprojFiles = Directory.EnumerateFiles(rootDir, "*.csproj", SearchOption.AllDirectories).ToList();
            var tasks = new List<Task>();
            foreach (var csprojFile in csprojFiles)
            {
                tasks.Add(
                    Task.Run(() => { CheckProjectDependencies(csprojFile); })
                );
            }
            Task.WaitAll(tasks.ToArray());
            Console.WriteLine("Press any key to return to menu.");
            Console.ReadKey();
        }

        static readonly object _object = new object();
        private static void PrintProject(string proj, List<string> outdated)
        {
            lock (_object)
            {
                if (outdated.Count != 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(proj);
                    Console.ForegroundColor = ConsoleColor.White;
                    foreach (string outdatedLib in outdated)
                    {
                        Console.WriteLine(outdatedLib);
                    }
                    Console.WriteLine();
                }
            }
        }

        public static void CheckProjectDependencies(string csprojFile)
        {
            Project project = new Project(csprojFile);
            var packageReferences = project.GetItems("PackageReference");
            List<string> outdated = new List<string>();
            foreach (var pr in packageReferences)
            {
                string packageName = pr.EvaluatedInclude;
                string version = pr.GetMetadataValue("Version");
                string latest = GetLatestVersion(packageName, version);
                if (version != latest)
                {
                    outdated.Add(packageName + "\n\tcurrent version: " + version + "\n\tlatest version:  " + latest);
                }
            }
            ProjectCollection.GlobalProjectCollection.UnloadProject(project);
            PrintProject(csprojFile, outdated);
            
            return;
        }
        public static string GetLatestVersion(string packageName, string version)
        {
            try
            {
                var _repo = PackageRepositoryFactory.Default.CreateRepository("http://hq.dotbydot.gr:7070/nuget/Panacea/");
                SemanticVersion semVer;
                SemanticVersion.TryParse(version , out semVer);
                var package = _repo.FindPackage(packageName, semVer);
                if (package.IsLatestVersion)
                {
                    return semVer.ToFullString();
                }
                else
                {
                    var packages = _repo.FindPackagesById(packageName);
                    var latest = packages.Max(p => p.Version);
                    return latest.ToFullString();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static void NugetUpdateAllProjects()
        {
            var rootDir = RegistrySettings.ReadValue("Root directory", "RootDir");
            var csprojFiles = Directory.EnumerateFiles(rootDir, "*.csproj", SearchOption.AllDirectories).ToList();
            var tasks = new List<Task>();
            List<string> changedProjects = new List<string>();
            foreach (var csprojFile in csprojFiles)
            {
                tasks.Add(
                    Task.Run(() => { if(UpdateNuget(csprojFile, "Panacea.")) changedProjects.Add(csprojFile); })
                );
            }
            Task.WaitAll(tasks.ToArray());
            Console.WriteLine(string.Join(Environment.NewLine, changedProjects));
            Console.WriteLine("Press any key to return to menu.");
            Console.ReadKey();
        }

        public static bool UpdateNuget(string csprojFile, string prefix)
        {
            Project project = new Project(csprojFile);
            var packageReferences = project.GetItems("PackageReference");
            List<string> outdated = new List<string>();
            var changed = false;
            foreach (var pr in packageReferences)
            {
                string packageName = pr.EvaluatedInclude;
                if (!packageName.StartsWith(prefix)) continue;
                string version = pr.GetMetadataValue("Version");
                string latest = GetLatestVersion(packageName, version);
                if (version != latest)
                {
                    pr.SetMetadataValue("Version", latest);
                    changed = true;
                }
            }

            project.Save();
            ProjectCollection.GlobalProjectCollection.UnloadProject(project);
            return changed;
        }
    }
}
