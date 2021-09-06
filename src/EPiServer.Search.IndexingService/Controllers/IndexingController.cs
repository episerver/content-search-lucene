using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;

namespace EPiServer.Search.IndexingService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IndexingController : ControllerBase
    {
        [HttpPost]
        [Route("reset/?namedindex={namedindex}&accessKey={accessKey}")]
        public IActionResult ResetIndex(string namedIndex, string accessKey)
        {
            return Ok();
        }

        [HttpPost]
        [Route("update/?accessKey={accessKey}")]
        public IActionResult UpdateIndex(string accessKey, SyndicationFeedFormatter formatter)
        {
            return Ok();
        }

        [HttpGet]
        [Route("search/?q={q}&namedindexes={namedindexes}&format=xml&offset={offset}&limit={limit}&accesskey={accesskey}")]
        public IActionResult GetSearchResultsXml(string q, string namedIndexes, string offset, string limit, string accessKey)
        {
            return Ok();
        }

        [HttpGet]
        [Route("search/?q={q}&namedindexes={namedindexes}&format=json&offset={offset}&limit={limit}&accesskey={accesskey}")]
        public IActionResult GetSearchResultsJson(string q, string namedIndexes, string offset, string limit, string accessKey)
        {
            return Ok();
        }

        [HttpGet]
        [Route("namedindexes/?accesskey={accesskey}")]
        public IActionResult GetNamedIndexes(string accessKey)
        {
            return Ok();
        }
    }
}
