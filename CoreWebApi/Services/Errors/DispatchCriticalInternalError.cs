
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BeSwarm.CoreWebApi.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BeSwarm.CoreWebApi.Services.Errors
{
    public class DispatchCriticalInternalError: IDispatchCriticalInternalError
    {
        ILogger<DispatchCriticalInternalError> logger;
      
        public DispatchCriticalInternalError(ILogger<DispatchCriticalInternalError> _logger)
        {
            logger = _logger;
        }
        public async Task<string> Dispatch(Exception ex, string additionnalinfo = "",[CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        {
            string uidinternalerror= "";
            try
            {
                CriticalInternalError ci = new CriticalInternalError(ex, sourceFilePath, $"{memberName} at line {sourceLineNumber}", additionnalinfo);
                logger.LogError(JsonConvert.SerializeObject(ci));
                uidinternalerror = ci.id;
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
                CriticalInternalError ci = new CriticalInternalError(ex, sourceFilePath, $"{memberName} at line {sourceLineNumber}", additionnalinfo);
                logger.LogCritical(JsonConvert.SerializeObject(ci));
                uidinternalerror = ci.id;
            }
            catch (Exception e)
            {

            }
            return uidinternalerror;
        }
    }
}
