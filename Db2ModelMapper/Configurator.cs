using System;
using System.Collections.Specialized;
using System.Configuration;

namespace Db2ModelMapper
{
    public class Configurator
    {
        public string ConnectionString { get; set; }

        public string Library { get; set; }

        public bool TraceQuery { get; set; }

        public static Configurator GetConfiguration()
        {
            var nameValueColection = ConfigurationManager.GetSection("Db2ModelMapper") as NameValueCollection;

            if (nameValueColection != null)
            {
                return new Configurator
                {
                    ConnectionString = nameValueColection["ConnectionString"],
                    Library = nameValueColection["Library"],
                    TraceQuery = bool.Parse(nameValueColection["TraceQuery"])
                };
            }

            throw new Exception("Config section Db2ModelMapper not exists");
        }
    }
}
