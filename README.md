couchbase-aspnet
================

This library provides infrastructure support for using [Couchbase Server](http://couchbase.com) and ASP.NET.

## Features:

ASP.NET SessionState Provider

* Port of the [Enyim Memcached Provider](https://github.com/enyim/memcached-providers) to Couchbase Server

## Requirements

* You'll need .NET Framework 3.5 or later to use the precompiled binaries. 
* To build the client, you'll need Visual Studio 2010 with MVC 3 installed.
* The Nuget package for [CouchbaseNetClient 1.0.1](http://nuget.org/packages/CouchbaseNetClient) is referenced by Couchbase.AspNet
* Couchbase Server 1.8

## Configuring the SessionState provider

Update the sessionState section in Web.config as follows:

    <sessionState customProvider="Couchbase" mode="Custom">
      <providers>
        <add name="Couchbase" type="Couchbase.AspNet.SessionState.CouchbaseSessionStateProvider, Couchbase.AspNet" />
      </providers>
    </sessionState>
		
Configure the Couchbase Client as you normally would:

    <section name="couchbase" type="Couchbase.Configuration.CouchbaseClientSection, Couchbase"/>	

	<couchbase>
		<servers bucket="default" bucketPassword="">
		<add uri="http://127.0.0.1:8091/pools"/>      
		</servers>
	</couchbase>

If you would like to use a custom configuration section, you may do so by specifying a value for the "section" attribute of the provider entry (see below).

    <section name="couchbaseSession" type="Couchbase.Configuration.CouchbaseClientSection, Couchbase"/>    

	<couchbaseSession>
		<servers bucket="sessionState" bucketPassword="">
		<add uri="http://127.0.0.1:8091/pools"/>      
		</servers>
	</couchbaseSession>

    <sessionState customProvider="Couchbase" mode="Custom">
      <providers>
        <add name="Couchbase" type="Couchbase.AspNet.SessionState.CouchbaseSessionStateProvider, Couchbase.AspNet" section="couchbaseSession" />
      </providers>
    </sessionState>

Note that currently, code-based configuration of the CouchbaseClient is not supported.

In code, simply use the Session object as you normally would.

	Session["Message"] = "Couchbase is awesome!";

Be sure to mark any user defined types as Serializable.

	[Serializable]
	public class SessionUser 
	{
		public string Username { get; set; }

		public string Email { get; set; }
	}