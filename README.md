# VRAtlas.API
API for the VRAtlas - The open source hub to find new events occurring in virtual reality.

## Live

The API is live at [api.vratlas.io/swagger](https://api.vratlas.io/swagger)

## Requesting Features

If you're wanting to request a feature for just the website or a feature that requires updating **both** the website and the API, go to [VRAtlas.Web](https://github.com/Auros/VRAtlas.Web) and create an issue there.

If you're just exclusively requesting a feature or making an issue related to the **API**, make it here.

## Setup Guide

Quick prelude: if you are trying to set up the API or website and are encountering issues or experiencing confusion at any step, make an issue on this repository to let me know. I will gladly assist and update the documentation if needed.

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
    "Audience": "<YOUR AUDIENCE>", // The Auth0 API Identifier, or audience.
    "ClientId": "<YOUR AUTH0 APPLICATION CLIENT ID>",
    "ClientSecret": "<YOUR AUTH0 APPLICATION CLIENT SECRET>"
  },
  "Cloudflare": {
    "ApiUrl": "https://api.cloudflare.com/client/v4/accounts/<YOUR ACCOUNT ID>", // VRAtlas.API uses v4 of the Cloudflare API
    "ApiKey": "<YOUR API KEY>"
  }
}
```

Finally, there are some development tools that are highly recommended.
* [Caddy](https://caddyserver.com/) (For setting up a local web reverse proxy)

### Setting Up the Databases
The VR Atlas requires two databases to work. One is for storing data and the other is for the [job scheduler](https://www.quartz-scheduler.net/) used internally (responsible for updating events and dispatching notifications at the right time.

1. Setup a PostgreSQL >=14 instance.
2. You do not need to create a database for the main storage. It will be generated automatically when you run the API for the first time.
3. Create a new database for the scheduler. Run [the following SQL](https://github.com/Auros/VRAtlas.API/blob/main/.docker/scheduler/init.sql) to initialize it.
4. Provide the connection strings to the API's configuration file.

### Setting Up Third Party Services

#### Cloudflare Images

Note: Cloudflare Images is a paid service. At the time of writing it costs $5.00 USD per month. The API will soon add an alternative image uploading implementation which saves and serves files locally.

1. Login / Register for Cloudflare and Cloudflare Images.
2. Navigate to your [Cloudflare Profile's API Tokens](https://dash.cloudflare.com/profile/api-tokens)
3. Create a new token using the template for R/W Access to Cloudflare Images or create a custom token with an Account permission for Cloudflare Images with Read and Edit enabled. Record the value of the token. This is your API Key.
4. Navigate to the Cloudflare Images dashboard, and record the values of `Account ID` and `Account Hash` for later.

Add the `Account ID` and API Key to your configuration file.

#### Auth0

Auth0 is an authorization and authentication as a service platform with multiple pricing tiers. It also has a free tier which will be fine for most use cases.

1. Login / Register for Auth0
2. Create a new tenant (recommended) or use the default tenant
3. Navigate to the `Applications -> APIs` Section
    1. Create a `New API`
        * The name and identifier are up to you. Record the value of your identifier for later.
        * Ensure the Signing Algorithm is set to `RS256`
        * This process will create a new Machine to Machine Application within Auth0
    2. Go to the `Permissions` tab of your API
    3. Add the following permissions:
        * `create:groups`
        * `update:groups`
        * `create:upload_url`
        * `create:events`
        * `update:events`
4. Navigate to the `Applications -> Applications` Section
    1. Find the recently created application. It should contain the name of the API you created.
    2. Go to the `Settings` tab of your Application
    3. Record the values of `Domain`, `Client ID`, and `Client Secret` for later.
    4. In Allowed Callback URLs, add the endpoint to what you want to redirect to after auth. If you're just working on the VR Atlas API, you can set this to a random localhost url like http://localhost:15000. If you're setting it up for the VR Atlas website, you will want to set it to `WEBSITE_INSTANCE_URL/login/callback`.
    5. In Advanced Settings...
        * Under the OAuth tab, set the `JSON Web Token (JWT) Signature Algorithm` to **HS256**.
        * Under the Grant Types tab, ensure `Authorization Code`, `Refresh Token`, and `Client Credentials` are selected.
    6. Make sure to save
    7. Go to the `APIs` tab of your Application
    8. Enable and expand `Auth0 Management API` 
        * Ensure the `read:users` and `update:users` permissions are selected.
5. Customize the login types under the `Authentication` section. The VR Atlas currently **requires** the logged in user have a profile picture, so ensure the login options all come from third party providers that have profile pictures.
6. Navigate to the `User Management -> Roles` Section
    1. Create a new role called `Standard`. This will be the default role given to everyone who logs in.
    2. Go to the `Permissions` tab of the role
    3. Click `Add Permissions`
    4. Select the API that you created earlier
    5. Select all the permissions you created earlier.
    6. Record the `Role ID` for the next step.
7. Navigate to `Auth Pipeline -> Rules`
    1. Create a new empty rule.
    2. Replace the basic script with the following:
        ```js
        function (user, context, callback) {
            const count = context.stats && context.stats.loginsCount ? context.stats.loginsCount : 0;
            if (count > 1) {
                return callback(null, user, context);
            }

            const ManagementClient = require('auth0@2.27.0').ManagementClient;
            const management = new ManagementClient({
                token: auth0.accessToken,
                domain: auth0.domain
            });

            const params =  { id : user.user_id};
            const data = { "roles" : ["<default role ID>"]};

            management.users.assignRoles(params, data, function (err, user) {
                if (err) {
                    // Handle error.
                    console.log(err);
                }
                callback(null, user, context);
            });
        }
        ```
    3. In this script, replace `<default role ID>` with the role ID you recorded earlier.
    4. This script assigns the user the Standard role when they first log in.
8. Add the values of your `Domain`, `Audience`, `Client ID`, and `Client Secret` to the VR Atlas API configuration file.
9. Build Your Auth Url
    * Combine all the values you've collected into this template:
    * `https://<DOMAIN>/authorize?response_type=code&client_id=<CLIENT_ID>&redirect_uri=<ONE OF YOUR CALLBACK URLS>&scope=openid%20profile&audience=<AUDIENCE>`
    * Navigating to this in the browser will put you through the OAuth flow and callback to the `redirect_uri` provided with a `code` parameter.
10. Testing Auth
    * You can pass the `code` and `redirect_uri` to the `/auth/token` endpoint of the VR Atlas API like so:
    * `/auth/token?code=<YOUR CODE>&redirectUri=<CALLBACK URL USED TO GET THE CODE>`
    * If everything went well, the endpoint should return a JSON response consisting of an access and id token as well as an expiration date.

#### Caddy
Caddy is highly recommended when working with both the API and the website, as it'll make working with URLs easier and bypass any annoyances with CORS locally.

1. [Install Caddy](https://caddyserver.com/docs/install). If you're developing on Windows, you'll probably want the static binary, docker setup, or Chocolatey.
2. Setup your Caddyfile to look as follows:
```
vratlas.localhost {
    handle_path /api/* {
        reverse_proxy https://localhost:7098 { # Change the port to what your API is running on.
            header_up X-Forwarded-Prefix /api 
        }
    }
    handle {
        reverse_proxy http://localhost:5173 # Change the port to what your dev website is running on. Refer to the VRAtlas.Web repository to see how to set up the website for development.
    }
}
```
3. Reload/re-run Caddy.
4. You should now be able to access the API at [vratlas.localhost/api/swagger](https://vratlas.localhost/api/swagger) and the website at [vratlas.localhost](https://vratlas.localhost)

## Contribution Guide

These instructions are subject to change.

1. Fork off this repository and **create a new branch** to work on your changes.
2. Create a PR into `main` and describe the changes.

* **DO** Try to follow the same naming conventions as the rest of the project
* **DO** Leave comments on code you're writing that isn't easily explainable
* **DO** Follow [Never-Nesting](https://www.youtube.com/watch?v=CFRhGnuXG-4)
* **DON'T** Leave unformatted lines
* **DON'T** Include unused commented code  
