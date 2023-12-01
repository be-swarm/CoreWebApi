using BeSwarm.CoreWebApi.Services.ConfigLoader;

using CoreWebApi;

using Microsoft.AspNetCore.Hosting;

using Serilog.Events;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using System.Runtime.InteropServices;
using BeSwarm.CoreWebApi.Services.Errors;
using BeSwarm.WebApi.Core.DBStorage;
using BeSwarm.CoreWebApi.Services.DataBase;
using System.Reflection;
using BeSwarm.CoreWebApi.Services.Swagger;
using MediatR;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;
using BeSwarm.CoreWebApi.Services.Tokens;
using BeSwarm.CoreWebApi.Services.MessageEvents;
using BeSwarm.CoreWebApi.Services.Mails;
using Confluent.Kafka;
using Serilog.Sinks.Kafka;
using Org.BouncyCastle.Asn1.IsisMtt.Ocsp;
using static Org.BouncyCastle.Math.EC.ECCurve;
using Org.BouncyCastle.Asn1.Cmp;
using Serilog.Formatting.Elasticsearch;
using BeSwarm.CoreWebApi.MiddleWare;

namespace BeSwarm.CoreWebApi;

public static class Builder
{
    public class SwaggerInfo : OpenApiInfo
    {
        public SwaggerInfo(string version, string title, string description)
        {
            Version = version;
            Title = title;
            Description = description;
            Contact = new OpenApiContact
            {
                Email = CoreEnvironment.env.swagger.contactemail,
                Url = new Uri(CoreEnvironment.env.swagger.websiteurl),
            };
        }
    }

    public static ServiceProvider AddCoreWebApiServices(this IServiceCollection services, string? configsource)
    {
        CoreEnvironment.services = services;

        // base logger
        services.AddLogging(
            builder =>
            {
                builder.ClearProviders()
                    .Configure(options => { })
                    .AddSimpleConsole(options => { options.SingleLine = true; });
            });

        ServiceProvider prov = services.BuildServiceProvider();
        Microsoft.Extensions.Logging.ILogger<Program> logger =
            prov.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();
        if (configsource is null)
        {
            logger.LogError($"config source must be set");
            Environment.Exit(-1);
        }
        // load config
        var configcontent = ConfigFactory.GetConfiguration(configsource);
        if (configcontent is null)
        {
            logger.LogError($"unable to get config content from config specification");
            Environment.Exit(-1);
        }
        else
        {

            Console.WriteLine("Configuration is set...");
        }
        ResultAction<CoreConfiguration> conf =
            ConfigBuilder.BuildConfiguration<CoreConfiguration>("core", configcontent, logger);
        if (conf.IsOk == false)
        {
            Environment.Exit(-1);
        }
        //
        // Build final logger
        //
        CoreEnvironment.env = conf.datas;
        LoggerConfiguration logconf;
        logconf = new LoggerConfiguration();
        logconf.Enrich.FromLogContext();
        switch (CoreEnvironment.env.log.loglevel)
        {
            case "Warning":
                logconf.MinimumLevel.Warning();
                break;
            case "Error":
                logconf.MinimumLevel.Error();
                break;
            default:
                logconf.MinimumLevel.Information();
                break;
        }

        if (CoreEnvironment.env.log.syslog != "")
        {
            logconf.WriteTo.UdpSyslog(CoreEnvironment.env.log.syslog, CoreEnvironment.env.log.syslogport);
        }
        //
        // log to file ?
        // limit size. 
        // just used to log startup
        //
        if (CoreEnvironment.env.log.file != "")
        {
            logconf.WriteTo.File(CoreEnvironment.env.log.file, rollingInterval: RollingInterval.Infinite,
                fileSizeLimitBytes: 500000);
        }
        //
        // log to elastic ?
        //

        if (CoreEnvironment.env.log.elastic.host != null) // specified and ok
        {
            logconf.WriteTo.Elasticsearch(
                new ElasticsearchSinkOptions(new Uri(CoreEnvironment.env.log.elastic.getconnectionstring()))
                {
                    MinimumLogEventLevel = LogEventLevel.Verbose,
                    IndexFormat = $"{CoreEnvironment.env.servicename}-log-{{0:yyyy.MM.dd}}",
                    TypeName = null,
                    AutoRegisterTemplate = true
                });
        }
        //
        // log to kafka ?
        //

        if (CoreEnvironment.env.log.kafka.bootstrapservers != null)
        {
            string certpath = "";
            if (!string.IsNullOrEmpty(CoreEnvironment.env.log.kafka.certsource))
            {
                string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string strWorkPath = System.IO.Path.GetDirectoryName(strExeFilePath);
                certpath = System.IO.Path.Combine(strWorkPath, "kafka-log.pem");
                try
                {
                    File.Delete(certpath);
                }
                catch (Exception e)
                {
                }

                var certcontent = ConfigFactory.GetConfiguration(CoreEnvironment.env.log.kafka.certsource);
                if (certcontent is null)
                {
                    throw (new Exception($"kafka: unable to get certsource from config specification"));
                }
                // config is get.
                // store it in file
                try
                {
                    File.WriteAllText(certpath, certcontent);
                }
                catch (Exception e)
                {
                    throw (new Exception($"kafka config: unable to write certsource to {certpath}"));
                }
            }
            if (!string.IsNullOrEmpty(certpath))
            {
                logconf.WriteTo.Kafka(
                  batchSizeLimit: 50,
                  period: 5,
                  bootstrapServers: CoreEnvironment.env.log.kafka.bootstrapservers,
                  saslUsername: CoreEnvironment.env.log.kafka.username,
                  saslPassword: CoreEnvironment.env.log.kafka.password,
                  topic: CoreEnvironment.env.log.kafka.topic,
                  securityProtocol: SecurityProtocol.SaslSsl,
                  saslMechanism: SaslMechanism.ScramSha256,
                  formatter: new ElasticsearchJsonFormatter()
                  );
            }
            else
            {
                logconf.WriteTo.Kafka(
                  batchSizeLimit: 50,
                  period: 5,
                  bootstrapServers: CoreEnvironment.env.log.kafka.bootstrapservers,
                  saslUsername: CoreEnvironment.env.log.kafka.username,
                  saslPassword: CoreEnvironment.env.log.kafka.password,
                  topic: CoreEnvironment.env.log.kafka.topic,
                  sslCaLocation: certpath,
                  securityProtocol: SecurityProtocol.SaslSsl,
                  saslMechanism: SaslMechanism.ScramSha256,
                  formatter: new ElasticsearchJsonFormatter()
                  );

            }
        }

        Log.Logger = logconf.CreateLogger();
        int loglevel;
        switch (CoreEnvironment.env.log.loglevel)
        {
            case "Warning":
                loglevel = (int)Microsoft.Extensions.Logging.LogLevel.Warning;
                break;
            case "Error":
                loglevel = (int)Microsoft.Extensions.Logging.LogLevel.Error;
                break;
            default:
                loglevel = (int)Microsoft.Extensions.Logging.LogLevel.Information;
                break;
        }

        services.AddLogging(
            builder =>
            {
                builder.ClearProviders()
                    .AddSimpleConsole(options => { options.SingleLine = true; })
                    .SetMinimumLevel((Microsoft.Extensions.Logging.LogLevel)loglevel)
                    .AddSerilog(dispose: true);
            });

        //reload logger
        prov = services.BuildServiceProvider();
        logger = prov.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();

        logger.LogInformation("***********************platform informations*************************");
        logger.LogInformation($"Framework:{RuntimeInformation.FrameworkDescription}");
        logger.LogInformation($"Architecture:{RuntimeInformation.OSArchitecture}");
        logger.LogInformation($"OS:{RuntimeInformation.OSDescription}:{RuntimeInformation.RuntimeIdentifier}");
        //
        // configure swagger
        //
        services.AddApiVersioning(o =>
        {
            o.AssumeDefaultVersionWhenUnspecified = true;
            o.DefaultApiVersion = new ApiVersion(1, 0);
        });
        services.AddSwaggerGen(c =>
        {

            c.SchemaFilter<AddSwaggerAttributes>();
            c.OperationFilter<RemoveVersionParameterFilter>();
            c.DocumentFilter<ReplaceVersionWithExactValueInPathFilter>();
            c.EnableAnnotations(enableAnnotationsForInheritance: true, enableAnnotationsForPolymorphism: true);

            c.AddSecurityDefinition("bearerAuth", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme."
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "bearerAuth"
                        }
                    },
                    new string[] { }
                }
            });
            c.CustomOperationIds(apiDescription =>
            {
                return apiDescription.TryGetMethodInfo(out MethodInfo methodinfo) ? methodinfo.Name : null;
            });
        });
        //
        // load RSA token keys
        //
        logger.LogInformation("Configure RSA token keys....");
        TokenRSAKeys configtoken = new(CoreEnvironment.env.tokenkeyfile);
        services.AddSingleton(configtoken);
        services.AddSingleton<ITokenService, JWTTokenService>();
        //
        // Cors
        //
        services.AddCors(c => { c.AddPolicy("AllowOrigin", options => options.AllowAnyOrigin()); });



        //
        // Build databases sessions
        //

        logger.LogInformation("build database(s) session(s)");

        SessionsMongoDB sessionsmongodb = new();
        int i = 0;
        ResultAction<ConfigMongoDB> confmongodb;
        foreach (var item in CoreEnvironment.env.databases)
        {

            //
            // is a mongodb ?
            //
            confmongodb = ConfigBuilder.BuildConfiguration<ConfigMongoDB>($"core.databases[{i}].mongodb", configcontent, logger);
            if (confmongodb.IsError) // specified but in errror
            {
                Environment.Exit(-1);
            }

            if (confmongodb.IsOk) // specified and ok
            {
                ResultAction resaddsession = sessionsmongodb.AddSession(confmongodb.datas, item.id);
                if (!resaddsession.IsOk)
                {
                    logger.LogError(resaddsession.error.Description);
                    Environment.Exit(-1);
                }

                logger.LogInformation($"successfull mongodb connect database id:{item.id}");
            }

            i++;
        }
        //
        // broker kafka ?
        //
        var confkafka = ConfigBuilder.BuildConfiguration<ConfigKafka>($"core.kafka", configcontent, logger);
        if (confkafka.IsError) // specified but in errror
        {
            Environment.Exit(-1);
        }
        services.AddSingleton<ConfigKafka>(confkafka.datas);
        //
        // mail ?
        //
        var confmail = ConfigBuilder.BuildConfiguration<ConfigMail>($"core.mail", configcontent, logger);
        if (confmail.IsError) // specified but in errror
        {
            Environment.Exit(-1);
        }
        services.AddSingleton<ConfigMail>(confmail.datas);

        // Add services to the container.
        services.AddSingleton<IDispatchCriticalInternalError, DispatchCriticalInternalError>();
        services.AddMediatR(typeof(IDBStorageEngine).GetTypeInfo().Assembly);
        services.AddSingleton<SessionsMongoDB>(sessionsmongodb);
        services.AddScoped<IDBStorageEngine, DBStorageEngineMongoDB>();
        services.AddScoped<IDBStorageBridge, DBStorageBridge>();

        services.AddSingleton<IMessageEvent, KafkaMessageEvent>();
        services.AddSingleton<IMailSender, SmtpSender>();
        services.AddSingleton<IMailReader, ImapReader>();

        services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddEndpointsApiExplorer();
        //	services.AddSwaggerGen();
        services.AddMvc().AddApplicationPart(typeof(Program).GetTypeInfo().Assembly).AddControllersAsServices();
        return prov;

    }
    public static void ConfigureCoreWebApiApp(this WebApplication app)
    {
        // Configure the HTTP request pipeline.
        app.UseCors(
            options => options.AllowAnyOrigin().WithMethods("GET", "POST", "PUT", "DELETE").AllowAnyHeader());
        app.UseCatchExceptionMiddleWareHandler();
        app.UseSwagger(c =>
        {
            c.RouteTemplate = CoreEnvironment.env.swagger.prefixurl + "/swagger/{documentname}/swagger.json";
        });
        if (CoreEnvironment.env.swagger.swaggerui)
        {
            app.UseSwaggerUI(c =>
            {
                foreach (var item in CoreEnvironment.swaggerfiles)
                {
                    c.SwaggerEndpoint("/" + CoreEnvironment.env.swagger.prefixurl + item.url, item.name);

                }
                c.InjectStylesheet("/swagger-ui/css/custom.css");
                c.RoutePrefix = CoreEnvironment.env.swagger.prefixurl + "/swagger";
                c.DisplayOperationId();
            });
        }
        app.MapControllers();
    }

}
