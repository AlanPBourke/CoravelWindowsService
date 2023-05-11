using CoravelWindowsService.Models;
using Newtonsoft.Json;

namespace IntegrationTests
{
    public class IntegrationTests
    {
        [Fact]
        public void Test_Deserialise_Config_Passing()
        {
            ServiceConfiguration? config = new ServiceConfiguration();
            string configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "service-config.json");
            if (File.Exists(configFile))
            {
                config = JsonConvert.DeserializeObject<ServiceConfiguration>(File.ReadAllText(configFile));
            }

            Assert.Equal(2, config!.EverySecondsJobDefinitions.Count);
        }
    }
}