var chakram = require("chakram");
var expect = require("chakram").expect;

describe("authorization tests", function(){
    var baseAuthUrl = "http://localhost:5004";
    var baseIdentityUrl = "http://localhost:5001";

    var newAuthClientAccessToken = "";
    var authRequestOptions = {
        headers: {
                "content-type": "application/json",
                "Authorization": ""
            }   
    }
    var requestOptions = {
        headers:{
            "content-type": "application/json"
        }
    }   

    var newIdentityClient = {
        "clientId": "func-test",
        "clientName": "Functional Test Client",
        "requireConsent": "false",
        "allowedGrantTypes": ["client_credentials"], 
        "allowedScopes": [
            "fabric/identity.manageresources", 
            "fabric/authorization.read",
            "fabric/authorization.write"
        ]    
    }

    var newAuthClient = {
        "id": "func-test",
        "name": "Functional Test Client",
        "topLevelSecurableItem": { "name": "func-test" }
    }

    var newGroupFoo = {
        "id": "roleFoo",
        "groupName": "roleFoo"
    }

    var newGroupBar = {
        "id": "roleBar",
        "groupName": "roleBar"
    }

    var newRoleFoo = {
        "Grain": "app",
        "SecurableItem": "func-test",
        "Name": "roleFoo"
    }

    var newRoleBar = {
        "Grain": "app",
        "SecurableItem": "func-test",
        "Name": "roleBar"
    }    

    var newPermissionUserCanView = {
        "Grain": "app",
        "SecurableItem": "func-test",
        "Name": "userCanView"
    }

    var newPermissionUserCanEdit = {
        "Grain": "app",
        "SecurableItem": "func-test",
        "Name": "userCanEdit"
    }
   
    function registerClientsWithIdentity(){
        var registrationApi = {
            "name": "registration-api", 
            "userClaims": ["name","email", "role", "groups"], 
            "scopes": [{ "name": "fabric/identity.manageresources"}]
        }

        return chakram.post(baseIdentityUrl + "/api/apiresource", registrationApi, requestOptions)
        .then(registerAuthorizationApi())
        .then(function(){
            var installerClient = { 
                "clientId": "fabric-installer",
                "clientName": "Fabric Installer",
                "requireConsent": "false",
                "allowedGrantTypes": ["client_credentials"], 
                "allowedScopes": [
                    "fabric/identity.manageresources", 
                    "fabric/authorization.read", 
                    "fabric/authorization.write", 
                    "fabric/authorization.manageclients"]
            }
        
            return chakram.post(baseIdentityUrl + "/api/client", installerClient, requestOptions);             
        })
        .then(function(postResponse){            
            return postResponse.body.clientSecret;                        
        });
    }

    function getAccessTokenForInstaller(installerSecret){        
        var postData = {
            form: {
                "client_id": "fabric-installer",
                "client_secret": installerSecret,
                "grant_type": "client_credentials",
                "scope": "fabric/identity.manageresources fabric/authorization.read fabric/authorization.write fabric/authorization.manageclients"
            }
        };      

        return getAccessToken(postData);
    }

    function getAccessTokenForAuthClient(newAuthClientSecret){        
        var clientData = {
            form:{
                "client_id": "func-test",
                "client_secret": newAuthClientSecret,
                "grant_type": "client_credentials",
                "scope":"fabric/authorization.read fabric/authorization.write"
            }
        }

        return getAccessToken(clientData);
    }

    function getAccessToken(clientData){
        return chakram.post(baseIdentityUrl + "/connect/token", undefined, clientData)
            .then(function(postResponse){  
                console.log("response for access token: " + JSON.stringify(postResponse.body));
                var accessToken = "Bearer " + postResponse.body.access_token;                                             
                return accessToken;
            });
    }

    function registerAuthorizationApi(){
        var authApiResource = { 
            "name": "authorization-api", 
            "userClaims": ["name", "email", "role", "groups"], 
            "scopes": [
                {"name": "fabric/authorization.read"}, 
                {"name": "fabric/authorization.write"}, 
                {"name":"fabric/authorization.manageclients"}
            ]
        }

        return chakram.post(baseIdentityUrl + "/api/apiresource", authApiResource, authRequestOptions);
    }

    before("running before", function(){
        this.timeout(5000);            

        return registerClientsWithIdentity()        
        .then(function(installerSecret){            
            return getAccessTokenForInstaller(installerSecret);
        })
        .then(function(retrievedAccessToken){                                   
            authRequestOptions.headers.Authorization = retrievedAccessToken;            
        });
        
    });

    describe("validate security", function(){
        it("should return a 403 when no access token provided", function(){
            var response = chakram.get(baseAuthUrl + "/clients", requestOptions);

            expect(response).to.have.status(403);
            return chakram.wait();
        });       
    });

    describe("register client", function(){      
        
        it("should register a client", function(){        
           return chakram.post(baseIdentityUrl + "/api/client", newIdentityClient, authRequestOptions)
            .then(function(clientResponse){
                expect(clientResponse).to.have.status(201);                                    
                return getAccessTokenForAuthClient(clientResponse.body.clientSecret);
            })
            .then(function(authClientAccessToken){                
                newAuthClientAccessToken = authClientAccessToken;
            })    
            .then(function(){
                return chakram.post(baseAuthUrl + "/clients", newAuthClient, authRequestOptions);           
            }) 
            .then(function(clientResponse){                
                expect(clientResponse).to.have.status(201);    
            });
            
        }); 
    });

    describe("register groups", function(){
        it("should register group foo", function(){
            var registerGroupFooResponse = chakram.post(baseAuthUrl + "/groups", newGroupFoo, authRequestOptions);
            return expect(registerGroupFooResponse).to.have.status(201);            
        });

        it("should register group bar", function(){
            var registerGroupBarResponse = chakram.post(baseAuthUrl + "/groups", newGroupBar, authRequestOptions);
            return expect(registerGroupBarResponse).to.have.status(201);           
        });
    });

    describe("register roles", function(){
        it("should register role foo", function(){
            authRequestOptions.headers.Authorization = newAuthClientAccessToken;
            
            var registerRoleFooResponse = chakram.post(baseAuthUrl + "/roles", newRoleFoo, authRequestOptions);            
            return expect(registerRoleFooResponse).to.have.status(201);
        });

        it("should register role bar", function(){
            authRequestOptions.headers.Authorization = newAuthClientAccessToken;
            
            var registerRoleBarResponse = chakram.post(baseAuthUrl + "/roles", newRoleBar, authRequestOptions);            
            return expect(registerRoleBarResponse).to.have.status(201);
        });
    });

    describe("register permissions", function(){
        it("should register permission userCanView", function(){
            authRequestOptions.headers.Authorization = newAuthClientAccessToken;

            var registerPermissionResponse = chakram.post(baseAuthUrl + "/Permissions", newPermissionUserCanView, authRequestOptions);
            return expect(registerPermissionResponse).to.have.status(201);
        });

         it("should register permission userCanEdit", function(){
            authRequestOptions.headers.Authorization = newAuthClientAccessToken;

            var registerPermissionResponse = chakram.post(baseAuthUrl + "/Permissions", newPermissionUserCanEdit, authRequestOptions);
            return expect(registerPermissionResponse).to.have.status(201);
        });
    });
});