using BeSwarm.CoreWebApi.Services.ConfigLoader;
using BeSwarm.CoreWebApi.Services.Errors;

using CommunityToolkit.Mvvm.Messaging;

using Confluent.Kafka;

using Elasticsearch.Net;
using Newtonsoft.Json.Linq;

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

  public  record InternalMessage(string  message);


    public class InternalMessageEvent : IMessageEvent
    {

        IDispatchError dispatch_error;
      
        ILogger<InternalMessageEvent> _logger;
        public InternalMessageEvent(IDispatchError _dispatch_error,ILogger<InternalMessageEvent> logger)
        {
            dispatch_error = _dispatch_error;
            _logger= logger;

        }
        public async Task ConsumeMessage(string topic, string consumergroupid, CancellationToken cts, Func<string, Task<ResultAction>> messagehandler)
        {
            WeakReferenceMessenger.Default.Register<InternalMessage, string>(this, topic, async (r, m) =>
            {
                _logger.LogInformation($"Consume Internal message from topic {topic}");
                await messagehandler(m.message);
            });
            while(true)
            {
                await Task.Delay(500);
            }

        }
        public async Task<ResultAction> PushMessage(string topic, string message)
        {
            ResultAction res = new();
            InternalMessage msg = new(message);
            WeakReferenceMessenger.Default.Send(msg, topic);
            return res;
        }
    }
}
