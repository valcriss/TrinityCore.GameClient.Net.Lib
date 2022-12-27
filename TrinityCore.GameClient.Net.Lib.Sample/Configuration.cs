using Newtonsoft.Json;
using System;

namespace TrinityCore.GameClient.Net.Lib.Sample
{
    public class Configuration
    {
        #region Public Properties

        [JsonProperty("dataPath")]
        public string DataPath { get; set; }

        [JsonProperty("host")]
        public string Host { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public bool IsValid => !string.IsNullOrEmpty(Host) && !string.IsNullOrEmpty(Login) && !string.IsNullOrEmpty(Password) && !string.IsNullOrEmpty(DataPath) && Port > 0;

        [JsonProperty("login")]
        public string Login { get; set; }

        [JsonProperty("logLevel")]
        public string LogLevel { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("port")]
        public int Port { get; set; }

        #endregion Public Properties

        #region Private Properties

        private string Directory { get; set; }
        private string Filename { get; set; }

        #endregion Private Properties

        #region Public Constructors

        public Configuration()
        {
            Directory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Filename = System.IO.Path.Combine(Directory, "configuration.json");
        }

        #endregion Public Constructors

        #region Public Methods

        public static Configuration Load()
        {
            Configuration configuration = new Configuration();
            try
            {
                if (System.IO.File.Exists(configuration.Filename))
                {
                    string content = System.IO.File.ReadAllText(configuration.Filename);
                    return JsonConvert.DeserializeObject<Configuration>(content);
                }
            }
            catch (Exception)
            {
            }
            string objContent = JsonConvert.SerializeObject(configuration);
            System.IO.File.WriteAllText(configuration.Filename, objContent);
            return configuration;
        }

        public void Save()
        {
            string objContent = JsonConvert.SerializeObject(this);
            System.IO.File.WriteAllText(Filename, objContent);
        }

        #endregion Public Methods
    }
}