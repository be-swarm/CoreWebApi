using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BeSwarm.CoreWebApi.Services.Errors
{
    public interface IDispatchCriticalInternalError
    {

        public Task<string> Dispatch(Exception ex, string additionnalinfo = "", [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0);

        public Task<string> DispatchCritical(Exception ex, string additionnalinfo = "", [CallerMemberName] string memberName = "",
         [CallerFilePath] string sourceFilePath = "",
         [CallerLineNumber] int sourceLineNumber = 0);

    }
}
