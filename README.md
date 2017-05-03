# Overview

The purpose of the authorization API is to allow client applications (aka relying party applications) to easily store and retrieve application level permissions. Client applications will submit all requests using the access token provided by the Identity Service. That access token will contain the sub (openid user identification) and groups claims along with the id of the client application. If the Access token is not present the Authorization service will respond with a 403 forbidden response. Below are details of the Authorization Service API:

For additional information please refer to our [wiki](https://github.com/HealthCatalyst/Fabric.Authorization/wiki).