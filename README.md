Couchbase ASP.NET Integration 3.0 
================

[![Join the chat at https://gitter.im/couchbaselabs/couchbase-aspnet](https://badges.gitter.im/couchbaselabs/couchbase-aspnet.svg)](https://gitter.im/couchbaselabs/couchbase-aspnet?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![Build status](https://ci.appveyor.com/api/projects/status/7owuw1ofqnp48bcb/branch/master?svg=true)](https://ci.appveyor.com/project/Couchbase/couchbase-aspnet)

This library provides infrastructure support for using [Couchbase Server](http://couchbase.com) and ASP.NET.

- To request a feature or report a bug use [Jira](https://issues.couchbase.com/projects/CBASP).
- Gitter home is here ^^^
- Couchbase Forums for help is [here](https://forums.couchbase.com/c/net-sdk).
- Current release on [NuGet](https://www.nuget.org/packages/CouchbaseAspNet/2.0.0-beta3).

## New Features for 3.0 ##

- Supports .NET 4.6.0 and 4.6.2 frameworks
- New provider for asynchronous output caching/session state: CouchbaseOutputCacheProviderAsync and CouchbaseSessionStateProviderAsync
- Bootstrapping strategies - inline, manual and section
- Suppress and log errors or throw them - your choice
- Provider level logging
- Role Based Access Control (RBAC) for Couchbase Server 5.0

**Note: this branch is new development for v3.0 ~~and unstable~~ is getting more stable atm - 2.0 branch is stable.**

## Caching and Session Providers for .NET Full Framework 4.5 and 4.6.2 ##

### Couchbase Output Cache ###
An output cache stores the output of pages, controls and HTTP responses. The default implementation in ASP.NET is to store in-memory on the server, which forces the front-end application (Web) servers to use more resources. The CouchbaseOutputCacheProvider is a distributed cache provider, which allows you to override the default ASP.NET Output Cache with a Couchbase-based implementation.

To use the `CouchbaseOutputCacheProvider` you will need to either build from source or use the NuGet package once it's available (a beta version for 3.0 will be released shortly). Once you have the dependency resolved, you will configure the custom OutputCacheProvider just like you do any other custom output cache provider in your Web.Config file:

	<caching>
      <outputCache defaultProvider="couchbase-cache">
        <providers>
          	<add name="couchbase-cache" 
				type="Couchbase.AspNet.Caching.CouchbaseCacheProviderAsync, Couchbase.AspNet, Version=1.0.0.0, Culture=neutral" 
				bucket="default" 
				bootstrapStrategy="section">
			</add>
        </providers>
      </outputCache>
    </caching>

In this example, we are using a `bootstrapStrategy` of `section`, which means we will bootstrap the Couchbase SDK that the Cache is using from a Web.Config section (note that there are three bootstrapping strategies in 3.0: `inline`, `section` and `manual` - more on that later):

	<configSections>
	      <section name="couchbase-cache" type="Couchbase.Configuration.Client.Providers.CouchbaseClientSection, Couchbase.NetClient" />
	</configSections>
   	<couchbase-cache>
      <servers>
        <add uri="http://localhost:8091/"></add>
      </servers>
      <buckets>
        <add name="default"></add>
      </buckets>
    </couchbase-cache>

Note that the `name` of the `defaultProvider` matches the `name` of the `section` (`couchbase-cache`), this is required so that the `caching` entry will map to that specific cluster and bucket (`default`). Once you have done this and assuming you have a Couchbase Server instance running locally, you'll just need to add the `OutputCache` attribute to the action method in your controller which you want to cache:

 	public class HomeController : Controller
    {
		...

        [OutputCache(Duration = 60, VaryByParam = "foo")]
        public ActionResult Time(string foo)
        {
            return Content(DateTime.Now.ToString());
        }
    }

This will Cache the current date for 60 seconds and will store a different copy for variations of `VaryByParam`, just like the default ASP.NET OutputCacheProvider. The difference being that the data will be stored in a distributed cache (Couchbase Server) off the front end Web Application server.

### Bootstrapping Strategies ###
The provider maintains a dependency on the Couchbase .NET SDK which it uses as a driver for storing and retrieving the cached items. During or before the provider initializes, the SDK must be "bootstrapped" or started as well; the SDK maintains the connections to the server and updates automatically as the cluster topology changes. 

There are three (3) different bootstrapping strategies that are supported: `inline`, `manual` or `section`.

#### Bootstrapping `inline` ####
A new feature for 3.0, is bootstrapping by adding your configuration "inline" with the custom Cache provider declaration in your Web.Config. When you do this, you do not need to provide a `CouchbaseClientSection` like we did in the introductory example above. Basically, you provide whatever config information you need within the `caching/providers/add` section in your Web.Config. For example:

	<system.web>
	    <caching>
	      <outputCache defaultProvider="couchbase-cache">
	        <providers>
	          <add name="couchbase-cache" 
	               type="Couchbase.AspNet.Caching.CouchbaseCacheProvider, Couchbase.AspNet, Version=1.0.0.0, Culture=neutral"
	               bootstrapStrategy="inline"
		       bucket="default"
	               servers="http://node1:8091; http://node2:8091"></add>
	        </providers>
	      </outputCache>
	    </caching>
	  </system.web>

When the provider initializes, it will create an internal `ClientConfiguration` for the Couchbase SDK and use the servers `"http://node1:8091"` and `"http://node2:8091"` as the bootstrapping servers. Note the `bootstrapStrategy` has the value `inline`, this will let the provider know how to handle the SDK bootstrapping.

All other configuration values will be defaulted to the `ClientConfiguration` settings, but there are several that can be overridden:

- `username`: Used for Couchbase Server 5.0 or greater RBAC authentication.
- `password`: The password for the password for the username or the bucket.
- `bucket`: The bucket where the data is stored. For pre-5.0 Couchbase servers this will be used to authenticate along with the password.
- `bootstrapStrategy`: The configuration strategy to use for SDK bootstrapping.
- `servers`: A semi-colon delimited list of bootstrapping server (A Couchbase Cluster node).
- `useSsl`: If true and certs are setup, TLS/SSL will be used for secure communication between the app server and the Couchbase cluster.
- `prefix`: An optional key-prefix for each item stored.
- `maxPoolSize`: The maximum size of the connection pool that the SDK will use.
- `minPoolSize`: The minimum size of the connection pool that the SDK will use (if supported).
- `operationLifespan`: The total length of time that the operation will live including retries.
- `sendTimeout`: The max amount of time that the client will wait for a Couchbase server response before timing out.
- `connectTimeout`: The max amount of time that the client will wait to connect to the server.
- `throwOnError`: if `true` if any error or exception is raised within the provider, it will be re-thrown or allowed to bubble up to the application. If `false`, it will only be logged and not thrown from the provider.

Here is an example with every configuration value set:

	<system.web>
	    <caching>
	      <outputCache defaultProvider="couchbase-cache">
	        <providers>
	          <add name="couchbase-cache" 
	               type="Couchbase.AspNet.Caching.CouchbaseCacheProvider, Couchbase.AspNet, Version=1.0.0.0, Culture=neutral"
	               username="Administrator"
	               password="password"
	               bucket="default"
	               bootstrapStrategy="inline"
	               servers="http://node1:8091; http://node2:8091"
	               useSsl="true"
	               prefix="app1"
	               maxPoolSize="10"
	               minPoolSize="1"
	               operationLifespan="2500"
	               sendTimeout="15000"
	               connectTimeout="1000"
	               throwOnError="false"></add>
	        </providers>
	      </outputCache>
	    </caching>
	  </system.web>

Note that in most cases, the default configuration is the best for `operationLifeSpan`, `sendTimeout`, `connectionTimeout`. For `maxPoolSize`, you should start with the default settings and then increase the value if needed. In general, smaller pool sizes are better than very large (50, 100, etc) pools.

#### Bootstrapping from `section` ####
Bootstrapping from Web.Config section is the way that version 2.0 of Couchbase providers worked. It uses the `CouchbaseClientSection` API that the .NET SDK exposes to allow the configuration to be done in a configuration section. This is illustrated in the first example above, in the "Couchbase Output Cache" section. You can read all about configuring the SDK on the main Couchbase site [here](https://developer.couchbase.com/documentation/server/5.0/sdk/dotnet/client-settings.html).

#### Programmatic or `manual` bootstrapping ####
This is a new feature for 3.0 which originally existed in the 1.0 version of the provider. In it's simplest terms, instead of supplying the configuration information inline or as a config section, you problematically configure the SDK somewhere in your application that will fire before the provider is initialized. To make it easier to use multiple clusters, there is a special `MultiCluster` class which holds the references to the `Cluster` and `IBucket` instances that the provider is going to use. You still need to use the Web.Config to declare the CouchbaseOutputCacheProvider and you will still need to add `bucket` and `bootstrapStrategy` variables. 

Here is an example, starting with the Web.Config:

	<system.web>
	    <caching>
	      <outputCache defaultProvider="couchbase-cache">
	        <providers>
	          <add name="couchbase-cache" 
	               type="Couchbase.AspNet.Caching.CouchbaseOutputCacheProvider, Couchbase.AspNet, Version=1.0.0.0, Culture=neutral"
	               bootstrapStrategy="inline"
				   bucket="default"
				   prefix="pfx"
				   throwOnError="true"
	        </providers>
	      </outputCache>
	    </caching>
	  </system.web>

Note that three configure variables are still supported in this scenario: 

- `throwOnError`: if `true` if any error or exception is raised within the provider, it will be re-thrown or allowed to bubble up to the application. If `false`, it will only be logged and not thrown from the provider.
- `prefix`: An optional key-prefix for each item stored.
- `bucket`: The bucket where the data is stored. For pre-5.0 Couchbase servers this will be used to authenticate along with the password.

All are optional, except `bucket` as it's required to map to the provider. 

Now, in your Global.asax you can initialize the client that the provider will use like this:

	protected void Application_Start()
    {
       ...

        //configure the couchbase cluster
        MultiCluster.Configure(new ClientConfiguration
        {
            Servers = new List<Uri>
            {
                new Uri("http://localhost:8091")
            }
        }, "couchbase-cache");
    }

It's important to note that you will still need to specify the name of the provider so that this cluster instance can be mapped to it. In this case, then name chosen is "couchbase-cache", but could be anything you wish.

### Using CouchbaseOutputCacheProvider in your application ###
Using the provider in your application is the same as you using the default ASP.NET caching provider. The `OutputCache` attribute is your main weapon here:

 	public class HomeController : Controller
    {
        ...

        [OutputCache(Duration = 60, VaryByParam = "foo")]
        public ActionResult Time(string foo)
        {
            return Content(DateTime.Now.ToString());
        }
    }


You can read all about using the OutputCache [here](https://docs.microsoft.com/en-us/aspnet/mvc/overview/older-versions-1/controllers-and-routing/improving-performance-with-output-caching-cs).

## NEW! Asynchronous Output Caching! ##
The .NET Framework Version 4.6.2 introduced a new asynchronous Output Cache provider class called `OutputCacheProviderAsync` which inherits from `OutputCacheProvider` and adds async methods. We have added a Couchbase specific implementation of this class in version 3.0!

There are a couple of steps to enable the asynchronous provider:

First, you will need to target .NET Framework 4.6.2 in your Web.Config:

	<system.web>
	  <compilation debug="true" targetFramework="4.6.2"/>
	  <httpRuntime targetFramework="4.6.2"/>
	</system.web>

Second, you will need to include the dependency on the [OutputCacheModuleAsync](https://www.nuget.org/packages/Microsoft.AspNet.OutputCache.OutputCacheModuleAsync/):

	PM> Install-Package Microsoft.AspNet.OutputCache.OutputCacheModuleAsync -Version 1.0.1

Finally, you'll need to change the `CouchbaseOutputCacheProvider` to `CouchbaseOutputProviderAsync`:

    <caching>
      <outputCache defaultProvider="couchbase-cache">
        <providers>
          <add name="couchbase-cache" 
			type="Couchbase.AspNet.Caching.CouchbaseCacheProviderAsync, Couchbase.AspNet, Version=1.0.0.0, Culture=neutral" 
			bucket="default" 
			bootstrapStrategy="section"></add>
        </providers>
      </outputCache>
    </caching>

Once you have done this, ASP.NET will use the asynchronous outout cache.
