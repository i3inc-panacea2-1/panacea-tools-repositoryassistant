using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace RepositoryAssistant
{

    public class Settings
    {
        public string Username { get; set; }

        public string Password { get; set; }

        public string ServerUrl { get; set; }

        public string RootDir { get; set; }

        public string Organization { get; set; }
    }
}
