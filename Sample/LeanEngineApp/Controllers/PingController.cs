using System;
using System.Threading;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LeanEngineApp.Controllers {
    [ApiController]
    [Route("__engine/{1,1.1}/ping")]
    public class PingController : ControllerBase {
        private readonly ILogger<PingController> logger;

        public PingController(ILogger<PingController> logger) {
            this.logger = logger;
        }

        [HttpGet]
        public Dictionary<string, string> Get() {
            Console.WriteLine("ping get to console");
            logger.LogDebug("ping get to logger");
            return new Dictionary<string, string> {
                { "runtime", "dotnet" },
                { "version", "1.0.0" }
            };
        }
    }
}
