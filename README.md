couchbase-aspnet 2.0
================

This library provides infrastructure support for using [Couchbase Server](http://couchbase.com) and ASP.NET.

## Features:

ASP.NET SessionState Provider

* Updated to Couchbase .NET SDK 2.1!
* Port of the [Enyim Memcached Provider](https://github.com/enyim/memcached-providers) to Couchbase Server

## Requirements

* You'll need .NET Framework 4.5 or later to use the precompiled binaries. 
* To build the client, you'll need Visual Studio > 2012 with MVC 4 to compile.
* The Nuget package for [Couchbase.NetClient 2.1.X](http://nuget.org/packages/CouchbaseNetClient) is referenced by Couchbase.AspNet
* Couchbase Server 2.5 or greater

## Application Startup

The first thing you will need to do is make sure you initialize the Couchbase Cluster using the ClusterHelper class in your Global.asax file:

    protected void Application_Start()
    {
    	ClusterHelper.Initialize("couchbase-caching");
    	
    	...
    }

## Configuring the SessionState provider

Update the sessionState section in Web.config as follows:

    <sessionState customProvider="Couchbase" mode="Custom">
      <providers>
        <add name="Couchbase" type="Couchbase.AspNet.SessionState.CouchbaseSessionStateProvider, Couchbase.AspNet" />
      </providers>
    </sessionState>
		
Configure the Couchbase Client as you normally would:

    <section name="couchbase-caching" type="Couchbase.Configuration.Client.Providers.CouchbaseClientSection, Couchbase.NetClient" />
    <couchbase-caching>
        <servers>
          <add uri="http://localhost:8091"></add>
        </servers>
        <buckets>
           <add name="default"></add>
        </buckets>
    </couchbase-caching>
    
If you would like to use a different bucket than the default one, you may do so by specifying a value for the "bucket" attribute of the provider entry (see below).

    <sessionState customProvider="Couchbase" mode="Custom">
      <providers>
        <add name="Couchbase" type="Couchbase.AspNet.SessionState.CouchbaseSessionStateProvider, Couchbase.AspNet" bucket="my-bucket" />
      </providers>
    </sessionState>

If you would like to control the prefixes used to store data in the Couchbase bucket, you can change the default values (which are based on the application name and virtual path) with your own custom values. This will allow you to share session data between applications if you so desire.

    <sessionState customProvider="Couchbase" mode="Custom">
      <providers>
        <add name="Couchbase" type="Couchbase.AspNet.SessionState.CouchbaseSessionStateProvider, Couchbase.AspNet" headerPrefix="header-" dataPrefix ="data-" />
      </providers>
    </sessionState>

If you would like to use a custom bucket factory, you may do so by specifying a value in the "factory" attribute of the provider entry. The example below sets it to the default factory, but you can replace this with your own factory class to have full control over the creation and lifecycle of the Couchbase client.

    <sessionState customProvider="Couchbase" mode="Custom">
      <providers>
        <add name="Couchbase" type="Couchbase.AspNet.SessionState.CouchbaseSessionStateProvider, Couchbase.AspNet" factory="Couchbase.AspNet.CouchbaseBucketFactory" />
      </providers>
    </sessionState>

This session handler also supports the ability to disable exclusive session access for ASP.NET sessions if desired. You can set the value using the "exclusiveAccess" attribute of the provider entry.

    <sessionState customProvider="Couchbase" mode="Custom">
      <providers>
        <add name="Couchbase" type="Couchbase.AspNet.SessionState.CouchbaseSessionStateProvider, Couchbase.AspNet" exclusiveAccess="false" />
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

Update the outputCache section in Web.config as follows:

      <outputCache defaultProvider="CouchbaseCache">
        <providers>
          <add name="CouchbaseCache" type="Couchbase.AspNet.OutputCache.CouchbaseOutputCacheProvider, Couchbase.AspNet" />
        </providers>
      </outputCache>

Configure the Couchbase Client as you normally would:

    <section name="couchbase-caching" type="Couchbase.Configuration.Client.Providers.CouchbaseClientSection, Couchbase.NetClient" />
    <couchbase-caching>
        <servers>
          <add uri="http://localhost:8091"></add>
        </servers>
        <buckets>
           <add name="default"></add>
        </buckets>
    </couchbase-caching>

If you would like to use a different bucket than the default one, you may do so by specifying a value for the "bucket" attribute of the provider entry (see below).

      <outputCache defaultProvider="CouchbaseCache">
        <providers>
          <add name="CouchbaseCache" type="Couchbase.AspNet.OutputCache.CouchbaseOutputCacheProvider, Couchbase.AspNet" bucket="my-bucket" />
        </providers>
      </outputCache>

If you would like to control the prefix used to store data in the Couchbase bucket, you can change the default values (which are based on the application name and virtual path) with your own custom value. This will allow you to share cache data between applications if you so desire.

      <outputCache defaultProvider="CouchbaseCache">
        <providers>
          <add name="CouchbaseCache" type="Couchbase.AspNet.OutputCache.CouchbaseOutputCacheProvider, Couchbase.AspNet" prefix="cache-" />
        </providers>
      </outputCache>

If you would like to use a custom bucket factory, you may do so by specifying a value in the "factory" attribute of the provider entry. The example below sets it to the default factory, but you can replace this with your own factory class to have full control over the creation and lifecycle of the Couchbase client.

      <outputCache defaultProvider="CouchbaseCache">
        <providers>
          <add name="CouchbaseCache" type="Couchbase.AspNet.OutputCache.CouchbaseOutputCacheProvider, Couchbase.AspNet" factory="Couchbase.AspNet.CouchbaseBucketFactory" />
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
