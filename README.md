# VRAtlas.API
API for the VRAtlas - The open source hub to find new events occurring in virtual reality.

## Live

The API is live at [api.vratlas.io/swagger](https://api.vratlas.io/swagger)

## Setup Guide

For development and deployment, you need a few third party services. In the future, additional alternatives that operate locally will be implemented.
* [Auth0](https://auth0.com) (For Authorization and Authentication)
* [Cloudflare Images](https://www.cloudflare.com/products/cloudflare-images/) (For Image Uploading)

You will also need these dependencies
* .NET 7 SDK
* PostgreSQL (14 or Later, Multiple Databases Required)

The configuration file `appsettings.json, secrets.json, etc.` is as follows:
```jsonc
{
  "ConnectionStrings": {
    "Main": "Server=dbhost;Port=dbport;User Id=userid;Password=password;Database=vratlas;",
    "Quartz": "Server=quartzdbhost;Port=quartzdbport;User Id=quartzuserid;Password=quartzpassword;Database=postgres;" // Relies on Quartz.NET for scheduling events.
  },
  "Auth0": {
    "Domain": "https://your-tenant.region.auth0.com/", // Make sure that it begins with the protocol ("https://") and ends with the forward slash ("/")
    "Audience: "<YOUR AUDIENCE>", // The Auth0 API Identifier, or audience.
    "ClientId": "<YOUR AUTH0 APPLICATION CLIENT ID>",
    "ClientSecret": "<YOUR AUTH0 APPLICATION CLIENT SECRET>"
  },
  "Cloudflare": {
    "ApiUrl": "https://api.cloudflare.com/client/v4/accounts/<YOUR ACCOUNT ID>", // VRAtlas.API uses v4 of the Cloudflare API
    "ApiKey": "<YOUR API KEY>"
  }
}
```
```
