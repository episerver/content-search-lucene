using EPiServer.Search.IndexingService.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;

namespace EPiServer.Search.IndexingService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IndexingController : ControllerBase
    {
        private readonly SecurityHandler _securityHandler;

        public IndexingController(SecurityHandler securityHandler)
        {
            _securityHandler = securityHandler;
        }

        [HttpPost]
        [Route("reset/?namedindex={namedindex}&accessKey={accessKey}")]
        public IActionResult ResetIndex(string namedIndex, string accessKey)
        {
            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Reset of index: {0} requested", namedIndex));

            if (!_securityHandler.IsAuthenticated(accessKey, AccessLevel.Modify))
            {
                return StatusCode(401);
            }

            return IndexingServiceHandler.Instance.ResetNamedIndex(namedIndex);
        }

        [HttpPost]
        [Route("update/?accessKey={accessKey}")]
        public IActionResult UpdateIndex(string accessKey, SyndicationFeedFormatter formatter)
        {
            if (!_securityHandler.IsAuthenticated(accessKey, AccessLevel.Modify))
            {
                return StatusCode(401);
            }

            return IndexingServiceHandler.Instance.UpdateIndex(formatter.Feed);
        }

        [HttpGet]
        [Route("search/?q={q}&namedindexes={namedindexes}&format=xml&offset={offset}&limit={limit}&accesskey={accesskey}")]
        public IActionResult GetSearchResultsXml(string q, string namedIndexes, string offset, string limit, string accessKey)
        {
            if (!_securityHandler.IsAuthenticated(accessKey, AccessLevel.Read))
            {
                return StatusCode(401);
            }

            return Ok(GetSearchResults(q, namedIndexes, Int32.Parse(offset, CultureInfo.InvariantCulture), Int32.Parse(limit, CultureInfo.InvariantCulture)));
        }

        [HttpGet]
        [Route("search/?q={q}&namedindexes={namedindexes}&format=json&offset={offset}&limit={limit}&accesskey={accesskey}")]
        public IActionResult GetSearchResultsJson(string q, string namedIndexes, string offset, string limit, string accessKey)
        {
            if (!_securityHandler.IsAuthenticated(accessKey, AccessLevel.Read))
            {
                return StatusCode(401);
            }

            return Ok(GetSearchResults(q, namedIndexes, Int32.Parse(offset, CultureInfo.InvariantCulture), Int32.Parse(limit, CultureInfo.InvariantCulture)));
        }

        [HttpGet]
        [Route("namedindexes/?accesskey={accesskey}")]
        public IActionResult GetNamedIndexes(string accessKey)
        {
            if (!_securityHandler.IsAuthenticated(accessKey, AccessLevel.Read))
            {
                return StatusCode(401);
            }

            return Ok(IndexingServiceHandler.Instance.GetNamedIndexes());
        }

        #region Private

        private SyndicationFeedFormatter GetSearchResults(string q, string namedIndexes, int offset, int limit)
        {
            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Request for search with query parser with expression: {0} in named indexes: {1}", q, namedIndexes));

            //Parse named indexes string from request
            string[] namedIndexesArr = null;
            if (!String.IsNullOrEmpty(namedIndexes))
            {
                char[] delimiter = { '|' };
                namedIndexesArr = namedIndexes.Split(delimiter);
            }

            return IndexingServiceHandler.Instance.GetSearchResults(q, namedIndexesArr, offset, limit);
        }
        #endregion
    }
}
