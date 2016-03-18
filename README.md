Couchbase ASP.NET Integration
================

[![Join the chat at https://gitter.im/couchbaselabs/couchbase-aspnet](https://badges.gitter.im/couchbaselabs/couchbase-aspnet.svg)](https://gitter.im/couchbaselabs/couchbase-aspnet?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

This library provides infrastructure support for using [Couchbase Server](http://couchbase.com) and ASP.NET.

- To request a feature or report a bug use [Jira](https://issues.couchbase.com/projects/CBASP).
- Gitter home is here ^^^
- Couchbase Forums for help is [here](https://forums.couchbase.com/c/net-sdk).
- Current release on [NuGet](https://www.nuget.org/packages/CouchbaseAspNet/2.0.0-beta3).

## Features:

ASP.NET SessionState Provider

* Updated to Couchbase .NET SDK 2.2!
* Port of the [Enyim Memcached Provider](https://github.com/enyim/memcached-providers) to Couchbase Server

## Requirements

* You'll need .NET Framework 4.5 or later to use the precompiled binaries. 
* To build the client, you'll need Visual Studio > 2012 with MVC 4 to compile.
* The Nuget package for [Couchbase.NetClient 2.2.X](http://nuget.org/packages/CouchbaseNetClient) is referenced by Couchbase.AspNet
* Couchbase Server 2.5 or greater

## Application Startup

***Note: its no longer required to initialize the ClusterHelper in global.asax or setup.cs to use the session or caching providers***

## Configuring the SessionState provider

Configure the Couchbase Client as you normally would:

	<configSections>
      <section name="couchbase-session" type="Couchbase.Configuration.Client.Providers.CouchbaseClientSection, Couchbase.NetClient" />
    </configSections>

	...

	<couchbase-session>
		<servers>
			<!-- changes in appSettings section should also be reflected here -->
			<add uri="http://localhost:8091/"></add>
		</servers>
		<buckets>
			<add name="my-memcached-bucket"></add>
		</buckets>
	</couchbase-session>

Update the sessionState section in Web.config as follows:

    <sessionState customProvider="couchbase-session" mode="Custom">
      <providers>
        <add name="couchbase-session" type="Couchbase.AspNet.SessionState.CouchbaseSessionStateProvider, Couchbase.AspNet"  bucket="my-memcached-bucket" 
		maxRetryCount="6"  />
      </providers>
    </sessionState>
		
**Important #1:** note that the name of the session provider ("couchbase-session") must match the name of the `CouchbaseClientSection` you defined earlier. The name can be anything you like but it must match so that the provider can lookup the couchbase configuration.

**Important #2:** the name of the bucket in the `sessionState` section ("my-memcached-bucket") must match the bucket name defined in the `CouchbaseClientSection `as well so during initialization the correct bucket is created.
    
If you would like to use a different bucket than the default one, you may do so by specifying a value for the "bucket" attribute of the provider entry (see below).

    <sessionState customProvider="Couchbase" mode="Custom">
      <providers>
        <add name="Couchbase" type="Couchbase.AspNet.SessionState.CouchbaseSessionStateProvider, Couchbase.AspNet" bucket="my-bucket" />
      </providers>
    </sessionState>

If you would like to control the prefixes used to store data in the Couchbase bucket, you can change the default values (which are based on the application name and virtual path) with your own custom values. This will allow you to share session data between applications if you so desire.

    <sessionState customProvider="couchbase-session" mode="Custom">
      <providers>
        <add name="couchbase-session" type="Couchbase.AspNet.SessionState.CouchbaseSessionStateProvider, Couchbase.AspNet" headerPrefix="header-" dataPrefix ="data-" />
      </providers>
    </sessionState>

This session handler also supports the ability to disable exclusive session access for ASP.NET sessions if desired. You can set the value using the "exclusiveAccess" attribute of the provider entry.

    <sessionState customProvider="couchbase-session" mode="Custom">
      <providers>
        <add name="couchbase-session" type="Couchbase.AspNet.SessionState.CouchbaseSessionStateProvider, Couchbase.AspNet" exclusiveAccess="false" />
      </providers>
    </sessionState>
	
In code, simply use the Session object as you normally would.

	Session["Message"] = "Couchbase is awesome!";

Be sure to mark any user defined types as Serializable.

	[Serializable]
	public class SessionUser 
	{
		public string Username { get; set; }

		public string Email { get; set; }
	}

## Configuring the OutputCache provider

Configure the Couchbase Client as you normally would:

    <configSections>
    	<section name="couchbase-caching" type="Couchbase.Configuration.Client.Providers.CouchbaseClientSection, Couchbase.NetClient" />
	</configSections>

	...

    <couchbase-caching>
        <servers>
          <add uri="http://localhost:8091"></add>
        </servers>
        <buckets>
           <add name="my-couchbase-bucket"></add>
        </buckets>
    </couchbase-caching>

Update the outputCache section in Web.config as follows:

      <outputCache defaultProvider="couchbase-caching">
        <providers>
          <add name="couchbase-caching" type="Couchbase.AspNet.OutputCache.CouchbaseOutputCacheProvider, Couchbase.AspNet" bucket="my-couchbase-bucket"/>
        </providers>
      </outputCache>

**Important #1:** note that the name of the caching provider ("couchbase-caching") must match the name of the `CouchbaseClientSection` you defined earlier. The name can be anything you like but it must match so that the provider can lookup the couchbase configuration.

**Important #2:** the name of the bucket in the `outputCache` section ("my-couchbase-bucket") must match the bucket name defined in the `CouchbaseClientSection `as well so during initialization the correct bucket is created.

If you would like to use a different bucket than the default one, you may do so by specifying a value for the "bucket" attribute of the provider entry (see below).

      <outputCache defaultProvider="couchbase-caching">
        <providers>
          <add name="couchbase-caching" type="Couchbase.AspNet.OutputCache.CouchbaseOutputCacheProvider, Couchbase.AspNet" bucket="my-bucket" />
        </providers>
      </outputCache>

If you would like to control the prefix used to store data in the Couchbase bucket, you can change the default values (which are based on the application name and virtual path) with your own custom value. This will allow you to share cache data between applications if you so desire.

      <outputCache defaultProvider="couchbase-caching">
        <providers>
          <add name="couchbase-caching" type="Couchbase.AspNet.OutputCache.CouchbaseOutputCacheProvider, Couchbase.AspNet" prefix="cache-" />
        </providers>
      </outputCache>

Once configured, simply enable output cache as you already do with ASP.NET MVC

    [OutputCache(Duration = 60, VaryByParam="foo")]
    public ActionResult Time(string foo)
    {
    	return Content(DateTime.Now.ToString());
    }

or with ASP.NET WebForms

    <%@ OutputCache Duration="60" VaryByParam="foo" %>

## Packaging Notes
From the Couchbase.AspNet directory, run nuget pack as follows:
`nuget pack .\Couchbase.AspNet.csproj`


##Change Log and Notes##
- `ICouchbaseBucketFactory` and `CouchbaseBucketFactory` have been made obsolete and are no longer used. Instead the provider will use the `CouchbaseConfigSection` as a factory to create the correct cluster and bucket objects.
- `ClusterHelper` is no longer used internally; the provider will create static `Cluster` and `CouchbaseBucket/MemcachedBucket` objects.
- A new parameter has been added to limit the  number of retries that will occur if an `CouchbaseSessionStateProvider` item is locked with CAS. It's called `maxRetryCount` and defaults to `5`:


    <add name="couchbase-session" type="Couchbase.AspNet.SessionState.CouchbaseSessionStateProvider, Couchbase.AspNet" maxRetryCount="10" />

##Contributing##
The Couchbase Caching and Session Providers is an open source software project which is depends upon community contributions and feedback. We welcome all forms of contribution good, bad or in-different!	
