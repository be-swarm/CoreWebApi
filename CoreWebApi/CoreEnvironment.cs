using BeSwarm.CoreWebApi.Services.Tokens;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace BeSwarm.CoreWebApi;
public class webapidlls
{
    public string path { get; set; } = "";
}


public class SwaggerFile
{
    public string url { get; set; }
    public string name { get; set; }
}

public class LogContext
{
    public string loglevel { get; set; } = "Information";
    public string syslog { get; set; } = "";
    public int syslogport { get; set; } = 514;
    public string file { get; set; } = "";
    public ElasticConfig elastic { get; set; } = new ElasticConfig();
    public KafkaConf kafka { get; set; } = new KafkaConf();

}
public class KafkaConf
{
    public string bootstrapservers { get; set; }
    public string certsource { get; set; } = "";
    public string topic { get; set; }
    [Hidden]public string username { get; set; }
    [Hidden]public string password { get; set; }
}

public class ElasticConfig
{
    public string host { get; set; }
    [Hidden]
    public string user { get; set; }
    [Hidden]
    public string password { get; set; }
    public string getconnectionstring()
    {

        if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(password))
        {
            string connectionstring = "";
            string[] ss = host.Split("//");
            if (ss.Length == 2)
            {
                connectionstring = $"{ss[0]}//{user}:{password}@{ss[1]}";
            }
            return connectionstring;
        }
        else return host;
    }
}




public class ConfigDataBase
{
    public string id { get; set; } = "";

}

public class ConfigSwagger
{
    public string contactemail { get; set; } = "";
    public string websiteurl { get; set; } = "";
    public bool swaggerui { get; set; } = true;
    public string prefixurl { get; set; } = "common";

}
public class CoreConfiguration
{

    [Len(1, -1)] public string servicename { get; set; } = "webapi";

    [Len(1, -1)] public string listen { get; set; } = "http://*:5000";

    [Len(1, -1)] public LogContext log { get; set; } = new();

    public List<ConfigDataBase> databases { get; set; } = new();
    [Len(1, -1)][Hidden] public string tokenkeyfile { get; set; } = "";

    public ConfigSwagger swagger { get; set; } = new();

}

public static class CoreEnvironment
{
    public static CoreConfiguration env { get; set; } = new();
    public static string dirseparator { get { if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "\\"; else return "/"; } }
    public static IServiceCollection services { get; set; }
    public static List<SwaggerFile> swaggerfiles { get; set; } = new();


}



