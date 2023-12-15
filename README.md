# content-search-lucene

## Introduction

<p>The Search service is deployed through the Docker image content-search-lucene/indexingservice. You can download the Docker image as the following command below:</p>

```shell
$ docker pull ghcr.io/episerver/content-search-lucene/indexingservice:sha-ec860ab
```

<p>The search integration with CMS is deployed through the NuGet package EPiServer.Search.Cms and should always be installed in an Optimizely website; it contains the required funcionality to query the search service and to integrate with the user interface in Optimizely.</p>

## Configuration

<h3>Client configuration (EPiServer.Search.Cms package)</h3>
<p>Configure the search client on the website to use the service. Set the attribute baseUri attribute to the endpoint for the service. The following example shows a default configuration:</p>

```html

"EPiServer": {
    "episerver.search": {
      "active": true,
      "namedIndexingServices": {
        "defaultService": "local",
        "services": [
          {
            "name": "local",
            "baseUri": "https://<yoursearchsite>/api/indexing",
            "accessKey": "<accessKey>"
          }
        ]
      },
      "searchResultFilter": {
        "defaultInclude": "true",
        "providers": [

        ]
      }
  }    
}

```
<h3>Service configuration (content-search-lucene/indexingservice)</h3>
<p>The default configuration allows local connections; that is, requests originated from the same server are allowed. If you deploy the Search service to another machine than the site, then the specify the ipAddress and ip6Address attributes  as follows:</p>

```html

"EPiServer": {
    "episerver.search.indexingservice": {
      "Clients": [
        {
          "Name": "<accessKey>",
          "Description": "local",
          "AllowLocal": true,
          "IpAddress": "<ipaddress v4 here>",
          "Ip6Address": "<ipaddress v6 here>",
          "ReadOnly": false
        }
      ],
      "NamedIndexes": {
        "DefaultIndex": "default",
        "Indexes": [
          {
            "Name": "default",
            "DirectoryPath": "[appDataPath]\\Index",
            "ReadOnly": false
          }
        ]
      }
    }

```

<h3>Deployment</h3>
<p>When you deploy the site to production, update the baseUri to the production search service. When you deploy the Search service to production, you might need to update the allowed IP addresses if that configuration is used.</p>

<h3>Local debug</h3>
<ul>
<li>On properties of Solution, select Multiple startup projects.</li>
<li>Choose 'Start' option for AlloyTemplates and IndexingService</li>
<li>Note: Index files will generate in folder ./src/EPiServer.Search.IndexingService/App_Data/Index</li>
</ul>