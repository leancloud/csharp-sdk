using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;

namespace LeanCloud.Engine {
    [ApiController]
    [Route("__engine/{1,1.1}")]
    [EnableCors(LCEngine.LCEngineCORS)]
    public class LCPingController : ControllerBase {
        [HttpGet("ping")]
        public object Get() {
            LCLogger.Debug("Ping ~~~");

            return new Dictionary<string, string> {
                { "runtime", $"dotnet-{Environment.Version}" },
                { "version", LCInternalApplication.SDKVersion }
            };
        }
    }
}
