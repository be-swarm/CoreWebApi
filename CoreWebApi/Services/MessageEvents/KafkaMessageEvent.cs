using BeSwarm.CoreWebApi.Services.ConfigLoader;
using BeSwarm.CoreWebApi.Services.Errors;

using Confluent.Kafka;

using Org.BouncyCastle.Asn1.IsisMtt.Ocsp;

using Polly;
using Polly.Retry;

using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using static Confluent.Kafka.ConfigPropertyNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BeSwarm.CoreWebApi.Services.MessageEvents
{

    public class ConfigKafka
    {
        [Len(1, -1)] public string BootstrapServers { get; set; }
        public int MessageTimeoutMs { get; set; } = 5000;
        public string certsource { get; set; } = "";
        [Hidden][Len(1, -1)] public string Username { get; set; }
        [Hidden][Len(1, -1)] public string Password { get; set; }

        public string GetCertificate()
        {
            string certpath = "";
           

            if (!string.IsNullOrEmpty(certsource))
            {
                string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string strWorkPath = System.IO.Path.GetDirectoryName(strExeFilePath);
                certpath = System.IO.Path.Combine(strWorkPath, "kafka.pem");
                try
                {
                    File.Delete(certpath);
                }
                catch (Exception e)
                {
                }

                var certcontent = ConfigFactory.GetConfiguration(certsource);
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
            return certpath;
        }

    }

    public class KafkaMessageEvent : IMessageEvent
    {

        ConfigKafka config;
        IDispatchError dispatch_error;
        string ErrorMessage = "";
        string certpath = "";
        AsyncRetryPolicy policy;
        ILogger<KafkaMessageEvent> _logger;
        public KafkaMessageEvent(ConfigKafka _config, IDispatchError _dispatch_error,ILogger<KafkaMessageEvent> logger)
        {
            config = _config;
            dispatch_error = _dispatch_error;
            certpath=config.GetCertificate();
            _logger= logger;
            policy = Policy.Handle<Exception>().WaitAndRetryAsync(new[] { TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(500) });


        }
        public async Task ConsumeMessage(string topic, string consumergroupid, CancellationToken cts, Func<string, Task<ResultAction>> messagehandler)
        {
               var cconf = new ConsumerConfig
                {
                    ClientId = Dns.GetHostName(),
                    BootstrapServers = config.BootstrapServers,
                    SecurityProtocol = SecurityProtocol.SaslSsl,
                    SaslMechanism = SaslMechanism.ScramSha256,
                    SaslUsername = config.Username,
                    SaslPassword = config.Password,
                    GroupId = consumergroupid,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    EnableAutoCommit = false,

                };
            if(certpath!="") cconf.SslCaLocation = certpath;

            using (var consumer = new ConsumerBuilder<Ignore, string>(cconf).SetErrorHandler((consumer, error) =>
            {
                Exception ex = new(error.Reason);
                dispatch_error.DispatchCritical(ex);
                ErrorMessage = error.Reason;
            }).Build())
            {
                consumer.Subscribe(topic);
                while (!cts.IsCancellationRequested)
                {

                    try
                    {
                        await policy.ExecuteAsync(async () =>
                        {
                            await Task.Run(async () =>
                            {
                                var cr = consumer.Consume(cts);
                                _logger.LogInformation($"Consume kafka message from topic {topic} consumer group {consumergroupid} ");
                                var ret = await messagehandler(cr.Message.Value);
                                consumer.Commit(cr);
                            });
                        });
                    }
                    catch (ConsumeException e)
                    {
                        await dispatch_error.DispatchCritical(e);
                    }
                    catch (OperationCanceledException)
                    {
                        consumer.Close();
                    }


                }
            }

        }
        public async Task<ResultAction> PushMessage(string topic, string message)
        {
            ErrorMessage = "";
            ResultAction res = new();
            var pconf = new ProducerConfig
            {
                BootstrapServers = config.BootstrapServers,
                ClientId = Dns.GetHostName(),
                MessageTimeoutMs = config.MessageTimeoutMs,
                SecurityProtocol = SecurityProtocol.SaslSsl,
                SaslMechanism = SaslMechanism.ScramSha256,
                SaslUsername = config.Username,
                SaslPassword = config.Password
            };
            if (certpath != "") pconf.SslCaLocation = certpath;
            using (var producer = new ProducerBuilder<Null, string>(pconf).SetErrorHandler((consumer, error) =>
            {
                Exception ex = new(error.Reason);
                dispatch_error.DispatchCritical(ex);
                ErrorMessage = error.Reason;
            }).Build())
            {
                try
                {
                    await policy.ExecuteAsync(async () =>
                    {
                        var produced = await producer.ProduceAsync(topic, new Message<Null, string> { Value = message });
                        if (produced.Status == PersistenceStatus.NotPersisted)
                        {
                            throw new Exception();
                        }

                        if (ErrorMessage != "")
                        {
                            res.SetError(new(ErrorMessage), StatusAction.internalerror);
                        }
                    });
                }
                catch (Exception ex)
                {
                    await dispatch_error.DispatchCritical(ex);
                    res.SetError(new($"Unable to send message on topic {topic}: {ex.Message}"), StatusAction.internalerror);
                }
            }
            return res;

        }
    }
}
