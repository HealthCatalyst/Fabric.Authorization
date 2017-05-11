[![Build status](https://healthcatalyst.visualstudio.com/_apis/public/build/definitions/eaeb1198-1e3e-4938-88f1-918e8bf769af/315/badge)](https://healthcatalyst.visualstudio.com/_apis/public/build/definitions/eaeb1198-1e3e-4938-88f1-918e8bf769af/315/badge)

# Overview

The purpose of the authorization API is to allow client applications (aka relying party applications) to easily store and retrieve application level permissions. Client applications will submit all requests using the access token provided by the Identity Service. That access token will contain the sub (openid user identification) and groups claims along with the id of the client application. If the Access token is not present the Authorization service will respond with a 403 forbidden response. Below are details of the Authorization Service API:

# Resources
[User](https://github.com/HealthCatalyst/Fabric.Authorization/wiki/User)  
[Groups](https://github.com/HealthCatalyst/Fabric.Authorization/wiki/Groups)  
[Roles](https://github.com/HealthCatalyst/Fabric.Authorization/wiki/Roles)  
[Permissions](https://github.com/HealthCatalyst/Fabric.Authorization/wiki/Permissions)  

# Key Design Considerations
Below are some key design considerations we are thinking through as we build out the service.

## Leverage Third Party Identity Provider Groups
We intend to leverage third party identity providers to provide the groups that a user is in.
This will allow relying party applications to map roles to groups maintained by the client's IT staff.
This way a client can manage their own groups and relying parties do not have to be involved with managing individual users directly.

## Hierarchical Permission Model
Some relying party applications that we will support have more complex needs. 
To that end we have come up with a hierarchical model for permissions.

A permission will consist of three parts:

`{grain}/{resource}.{permission}`

We will have three top level reserved grains:
```
patient 
user
app
```

A relying party application will store its permissions under the `app` grain.
For example, lets say we had an app with a clientid `myclientapp1`. 
The permissions for that application would look like:

```
app/myclientapp1.manageusers
app/myclientapp1.createalerts
app/myclientapp1.createdocument
```

Now lets say that relying party application needed to secure additional resources within the application,
for example a document that a user was able to create based on the `app\myclientapp1.createdocument`
permission. Those permissions might look like:

```
myclientapp1/mynewdocument.edit
myclientapp1/mynewdocument.delete
```

They can be created in the Authorization service via the API, and associated to the appropriate role.

The `patient` and `user` grains will be used to manage permissions to FHIR 
based data services and are based on FHIR scopes and resource which you can read 
more about [here](http://docs.smarthealthit.org/authorization/scopes-and-launch-context/).

The `app` grain is where all custom application permissions will live.

This is how we will conceptually separate data permissions from app permissions.

## Prefer Fast Reads
We expect that the bulk of the traffic to the Authorization service to be for reading permissions related to a user via their groups.
As a result, we shaped the /User/Permissions api to return a condensed format for permissions using the above string based model istead of returning a verbos json representation for each permission.
In additiona by default, we plan to only return the top level permissions for an application based on the clientid. So a request to:
`GET /user/permissions` from the relying party application with a client id of `myclientapp1` would return only permissions in the `app/myclientapp1` grain/resource combination.
The relying party application can make a subsequent request to get the nested permissions by supplying the grain and resource query string parameters.


