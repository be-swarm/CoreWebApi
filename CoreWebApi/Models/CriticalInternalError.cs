using MediatR;
using Newtonsoft.Json;
using System;

namespace BeSwarm.CoreWebApi.Models
{
    public class CriticalInternalError : INotification
    {

    
        [JsonProperty("id")] public string id { get; set; }
        public string instanceid { get; set; }
        public DateTime timestamp { get; set; }
        public string additionnalinfo { get;  }
        public string error { get;  }
        public string classname { get; }
        public string function { get; }
        public CriticalInternalError()
        {

        }
        public CriticalInternalError(Exception e,string _class,string _function,string _additionalinfo)
        {
            if(e==null)
            { throw new System.ArgumentException("Exeption is required");
            }
            instanceid = CoreEnvironment.env.servicename;
            classname=_class;
            error = e.Message;
            function = _function;
            id = Guid.NewGuid().ToString();
            timestamp = DateTime.UtcNow;
            additionnalinfo = _additionalinfo;
          
           
        }
    }
}

