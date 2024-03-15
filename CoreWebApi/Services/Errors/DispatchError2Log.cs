
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BeSwarm.CoreWebApi.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using Serilog.Context;

namespace BeSwarm.CoreWebApi.Services.Errors
{
    public class DispatchError2Log: IDispatchError
    {
        ILogger<DispatchError2Log> logger;
      
        public DispatchError2Log(ILogger<DispatchError2Log> _logger)
        {
            logger = _logger;
        }
        public async Task<string> DispatchError(InternalError err, string additionnalinfo = "",[CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        {
            string uidinternalerror= "";
            try
            {
                uidinternalerror = Guid.NewGuid().ToString();
                using (LogContext.PushProperty("Type", "internal error"))
                using (LogContext.PushProperty("ErrorID", uidinternalerror))
                using (LogContext.PushProperty("Code", err.ErrorCode))
                using (LogContext.PushProperty("Source file", sourceFilePath))
                using (LogContext.PushProperty("MemberName", memberName))
                using (LogContext.PushProperty("Line", sourceLineNumber))
                using (LogContext.PushProperty("AdditionnalInfo", additionnalinfo))
                logger.LogError(err.Description);
                
            }
            catch(Exception e)
            {

            }
            return uidinternalerror;
        }

    


        public async Task<string> DispatchCritical(Exception ex, string additionnalinfo = "", [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            string uidinternalerror = "";
            try
            {
               
                uidinternalerror = Guid.NewGuid().ToString();
                using (LogContext.PushProperty("Type", "exception"))
                using (LogContext.PushProperty("ErrorID", uidinternalerror))
                using (LogContext.PushProperty("Code", ex.HResult))
                using (LogContext.PushProperty("Source file", sourceFilePath))
                using (LogContext.PushProperty("MemberName", memberName))
                using (LogContext.PushProperty("Line", sourceLineNumber))
                using (LogContext.PushProperty("AdditionnalInfo", additionnalinfo))
                logger.LogCritical(ex.Message);             
            }
            catch (Exception e)
            {

            }
            return uidinternalerror;
        }
    }
}
