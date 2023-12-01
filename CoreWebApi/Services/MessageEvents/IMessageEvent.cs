namespace BeSwarm.CoreWebApi.Services.MessageEvents
{
    public interface IMessageEvent
    {

        public Task<ResultAction> PushMessage(string topic, string message);
        public Task  ConsumeMessage(string topic,string consumergroupid,CancellationToken cts, Func<string, Task<ResultAction>> massagehandler);
    }
}
