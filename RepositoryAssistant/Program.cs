
using Microsoft.Win32;
using Newtonsoft.Json;
using Octokit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepositoryAssistant
{
    class Program
    {

        static List<KeyValuePair<string, Action>> _options = new List<KeyValuePair<string, Action>>()
        {
            new KeyValuePair<string, Action>("Setup repositories.", GitHelper.SetupRepositories),
            new KeyValuePair<string, Action>("Get repositories with local changes.", GitHelper.PrintRepositoriesWithLocalChanges),
            new KeyValuePair<string, Action>("Pull all.", GitHelper.PullAll),
            new KeyValuePair<string, Action>("Check all repo dependencies.", NugetHelper.CheckAllProjectDependencies),
            new KeyValuePair<string, Action>("Exit.", ()=> Environment.Exit(0)),
        };

        static void Main(string[] args)
        {

            MainMenu();
        }


        static void MainMenu()
        {
            while (true)
            {
                int choice;
                bool res;
                bool error = false;
                do
                {
                    Console.Clear();

                    for (var i = 0; i < _options.Count; i++)
                    {
                        Console.WriteLine($"{i + 1}. {_options[i].Key}");
                    }
                    if (error)
                    {
                        var color = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Wrong choice!");
                        Console.ForegroundColor = color;
                    }
                    error = true;
                    res = int.TryParse(Console.ReadLine(), out choice);
                } while (!res || choice > _options.Count || choice < 1);
                Console.Clear();
                error = false;
                _options[choice - 1].Value.Invoke();
            }
        }
    }
}

