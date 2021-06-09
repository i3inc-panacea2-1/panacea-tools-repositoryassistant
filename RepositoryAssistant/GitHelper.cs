using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Microsoft.Win32;
using Octokit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RepositoryAssistant
{
    static class GitHelper
    {
        public static void SetupRepositories()
        {

            var settings = GetGitSettings();

            Console.WriteLine("Doing work. Don't look at me like that. The work should have finished by now. But you are still reading. Whatev.");

            var git = new GitHubClient(new ProductHeaderValue("MyAmazingApp"), new Uri(settings.ServerUrl));
            git.Credentials = new Octokit.Credentials(settings.Username, settings.Password.ToString());

            var org = git.Organization.Get(settings.Organization).Result;
            var repos = git.Repository.GetAllForOrg(settings.Organization).Result.ToList();


            var apis = repos.Where(r => r.Name.StartsWith("panacea-modularity-"))
                            .ToList();
            repos.RemoveAll(r => apis.Contains(r));

            var modules = repos
                            .Where(r => r.Name.StartsWith("panacea-modules-"))
                            .ToList();

            repos.RemoveAll(r => modules.Contains(r));

            var tools = repos
                           .Where(r => r.Name.StartsWith("panacea-tools-"))
                           .ToList();

            repos.RemoveAll(r => tools.Contains(r));

            var apps = repos.Where(r => r.Name == "panacea" || r.Name.StartsWith("panacea-applications")).ToList();
            repos.RemoveAll(r => apps.Contains(r));

            var libs = repos;
            var path = settings.RootDir;

            CloneRepos(apis.Select(r => r.Name), Path.Combine(path, "Libraries"));
            CloneRepos(libs.Select(r => r.Name), Path.Combine(path, "Libraries"));
            CloneRepos(modules.Select(r => r.Name), Path.Combine(path, "Modules"));
            CloneRepos(apps.Select(r => r.Name), Path.Combine(path, "Applications"));
            CloneRepos(tools.Select(r => r.Name), Path.Combine(path, "Tools"));

            Console.WriteLine("Press any key");
            Console.ReadKey();
        }

        static void CloneRepos(IEnumerable<string> repos, string path)
        {
            var settings = GetGitSettings();
            foreach (var repo in repos)
            {
                var url = new Uri(new Uri(settings.ServerUrl), settings.Organization + "/" + repo + ".git");
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(repo);
                Console.ForegroundColor = color;
                Clone(url.ToString(), Path.Combine(path, repo));
            }
        }

        static void Clone(string url, string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            var settings = GetGitSettings();
            var co = new CloneOptions()
            {
                OnTransferProgress = OnCloneProgress,
                CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials { Username = settings.Username, Password = settings.Password.ToString() }
            };
            Task.Run(() =>
            {
                try
                {
                    LibGit2Sharp.Repository.Clone(url, path, co);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }).Wait();
            Console.WriteLine();
        }

        private static bool OnCloneProgress(TransferProgress progress)
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
            Console.Write(progress.ReceivedObjects + "/" + progress.TotalObjects);
            return true;
        }

        public static void PullAll()
        {
            var settings = GetGitSettings();
            var dirs = Directory.EnumerateDirectories(settings.RootDir, ".git", SearchOption.AllDirectories);
            foreach (var dir in dirs.Select(d => new DirectoryInfo(d).Parent.FullName))
            {

                using (var repo = new LibGit2Sharp.Repository(dir, new RepositoryOptions()))
                {
                    var color = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(Path.GetFileName(dir));
                    Console.ForegroundColor = color;


                    PullOptions pullOptions = new PullOptions()
                    {
                        MergeOptions = new MergeOptions()
                        {
                            FastForwardStrategy = FastForwardStrategy.Default
                        },
                        FetchOptions = new FetchOptions()
                        {
                            CredentialsProvider = new CredentialsHandler((url, usernameFromUrl, types) =>
                            new UsernamePasswordCredentials()
                            {
                                Username = settings.Username,
                                Password = settings.Password
                            }),
                            OnTransferProgress = OnCloneProgress
                        }
                    };
                    Task.Run(() =>
                    {
                        try
                        {
                            MergeResult mergeResult = Commands.Pull(
                                repo,
                                new LibGit2Sharp.Signature(settings.Username, settings.Username, DateTimeOffset.Now),
                                pullOptions
                            );
                            if (mergeResult.Commit != null)
                            {
                                Console.WriteLine();
                                Console.WriteLine(mergeResult.Status);
                                Console.WriteLine(mergeResult.Commit.ToString());
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(ex.Message);
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                    }).Wait();

                }
            }
        }

        public static void PrintRepositoriesWithLocalChanges()
        {
            foreach (var dir in GetRepositoriesWithLocalChanges())
            {
                Console.WriteLine(dir);
            }
            Console.ReadKey();
        }

        public static IEnumerable<string> GetRepositoriesWithLocalChanges()
        {
            var settings = GetGitSettings();
            var dirs = Directory.EnumerateDirectories(settings.RootDir, ".git", SearchOption.AllDirectories);
            foreach (var dir in dirs.Select(d => new DirectoryInfo(d).Parent.FullName))
            {
                using (var repo = new LibGit2Sharp.Repository(dir, new RepositoryOptions()))
                {
                    var options = new FetchOptions();
                    options.CredentialsProvider = new CredentialsHandler((url, usernameFromUrl, types) =>
                        new UsernamePasswordCredentials()
                        {
                            Username = settings.Username,
                            Password = RegistrySettings.DecryptData(settings.Password)
                        });
                    if (repo.RetrieveStatus().IsDirty) yield return dir;
                }
            }
        }

        public static Settings GetGitSettings()
        {
            var settings = new Settings();

            settings.ServerUrl = RegistrySettings.ReadValue("Github server url", "GithubUrl");
            settings.Organization = RegistrySettings.ReadValue("Github organization", "Organization");
            settings.Username = RegistrySettings.ReadValue("Github username", "Username");
            settings.Password = RegistrySettings.ReadValue("Github password", "Password", true);
            settings.RootDir = RegistrySettings.ReadValue("Root directory", "RootDir");

            return settings;
        }
    }
}
