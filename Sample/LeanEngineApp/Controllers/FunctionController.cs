using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace LeanEngineApp.Controllers {
    [ApiController]
    [Route("{1,1.1}/functions/")]
    public class FunctionController : ControllerBase {
        public FunctionController() {
        }

        [HttpGet("_ops/metadatas")]
        public Dictionary<string, List<string>> Get() {
            List<string> functions = new List<string> {

            };
            return new Dictionary<string, List<string>> {
                { "result", functions }
            };
        }

        public async Task<object> Post() {
            return null;
        }
    }
}
