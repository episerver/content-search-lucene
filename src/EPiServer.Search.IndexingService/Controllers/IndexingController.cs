﻿using EPiServer.Search.IndexingService.Helpers;
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
        private readonly IIndexingServiceHandler _indexingServiceHandler;
        private readonly IIndexingServiceSettings _indexingServiceSettings;
        private readonly IResponseExceptionHelper _responseExceptionHelper;

        public IndexingController(SecurityHandler securityHandler, 
            IIndexingServiceHandler indexingServiceHandler,
            IIndexingServiceSettings indexingServiceSettings,
            IResponseExceptionHelper responseExceptionHelper)
        {
            _securityHandler = securityHandler;
            _indexingServiceHandler = indexingServiceHandler;
            _indexingServiceSettings = indexingServiceSettings;
            _responseExceptionHelper = responseExceptionHelper;
        }

        //POST: reset?namedIndex={namedIndex}&accessKey={accessKey}
        [HttpPost]
        [Route("reset")]
        public IActionResult ResetIndex(string namedIndex, string accessKey)
        {
            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Reset of index: {0} requested", namedIndex));

            if (!_securityHandler.IsAuthenticated(accessKey, AccessLevel.Modify))
            {
                _responseExceptionHelper.HandleServiceUnauthorized("Unauthorized");
            }

            _indexingServiceHandler.ResetNamedIndex(namedIndex);
            return Ok();
        }

        //POST: update?accessKey={accessKey}
        [HttpPost]
        [Route("update")]        
        public IActionResult UpdateIndex(string accessKey, [FromBody] FeedModel model)
        {
            if (!_securityHandler.IsAuthenticated(accessKey, AccessLevel.Modify))
            {
                _responseExceptionHelper.HandleServiceUnauthorized("Unauthorized");
            }

            _indexingServiceHandler.UpdateIndex(model);
            return Ok();
        }

        //GET: search?q={q}&namedIndexes={namedIndexes}&offset={offset}&limit={limit}&accessKey={accessKey}
        [HttpGet]
        [Route("search/json")]
        public IActionResult GetSearchResultsJson(string q, string namedIndexes, string offset, string limit, string accessKey)
        {
            if (!_securityHandler.IsAuthenticated(accessKey, AccessLevel.Read))
            {
                _responseExceptionHelper.HandleServiceUnauthorized("Unauthorized");
            }

            return Ok(_indexingServiceHandler.GetSearchResults(q, namedIndexes, Int32.Parse(offset, CultureInfo.InvariantCulture), Int32.Parse(limit, CultureInfo.InvariantCulture)));
        }

        //GET: namedindexes?accesskey={accesskey}
        [HttpGet]
        [Route("namedindexes")]
        public IActionResult GetNamedIndexes(string accessKey)
        {
            if (!_securityHandler.IsAuthenticated(accessKey, AccessLevel.Read))
            {
                _responseExceptionHelper.HandleServiceUnauthorized("Unauthorized");
            }

            return Ok(_indexingServiceHandler.GetNamedIndexes());
        }

        #region Private

        
        #endregion

        #region Events

        public static event EventHandler DocumentAdding;
        public static event EventHandler DocumentAdded;
        public static event EventHandler DocumentRemoving;
        public static event EventHandler DocumentRemoved;
        public static event EventHandler IndexOptimized;
        public static event EventHandler InternalServerError;

        internal static void OnDocumentAdding(object sender, AddUpdateEventArgs e)
        {
            if (DocumentAdding != null)
            {
                DocumentAdding(sender, e);
            }
        }

        internal static void OnDocumentAdded(object sender, AddUpdateEventArgs e)
        {
            if (DocumentAdded != null)
            {
                DocumentAdded(sender, e);
            }
        }

        internal static void OnDocumentRemoving(object sender, RemoveEventArgs e)
        {
            if (DocumentRemoving != null)
            {
                DocumentRemoving(sender, e);
            }
        }

        internal static void OnDocumentRemoved(object sender, RemoveEventArgs e)
        {
            if (DocumentRemoved != null)
            {
                DocumentRemoved(sender, e);
            }
        }

        internal static void OnIndexedOptimized(object sender, OptimizedEventArgs e)
        {
            if (IndexOptimized != null)
            {
                IndexOptimized(sender, e);
            }
        }



        internal static void OnInternalServerError(object sender, InternalServerErrorEventArgs e)
        {
            if (InternalServerError != null)
            {
                InternalServerError(sender, e);
            }
        }


        #endregion
    }
    
}