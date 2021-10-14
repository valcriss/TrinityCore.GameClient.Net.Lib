using Newtonsoft.Json;
using System;

namespace TrinityCore.GameClient.Net.Lib.Sample
{
    public class Configuration
    {
        [JsonProperty("host")]
        public string Host { get; set; }

        [JsonProperty("port")]
        public int Port { get; set; }

        [JsonProperty("login")]
        public string Login { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("dataPath")]
        public string DataPath { get; set; }

        [JsonProperty("logLevel")]
        public string LogLevel { get; set; }

        [JsonIgnore]
        public bool IsValid => !string.IsNullOrEmpty(Host) && !string.IsNullOrEmpty(Login) && !string.IsNullOrEmpty(Password)&& !string.IsNullOrEmpty(DataPath) && Port > 0;


        private string Directory { get; set; }
        private string Filename { get; set; }

        public Configuration()
        {
            Directory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Filename = System.IO.Path.Combine(Directory, "configuration.json");
        }

        public void Save()
        {
            string objContent = JsonConvert.SerializeObject(this);
            System.IO.File.WriteAllText(Filename, objContent);
        }

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
    }
}
