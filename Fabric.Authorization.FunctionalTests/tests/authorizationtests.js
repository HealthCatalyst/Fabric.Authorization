var chakram = require("chakram");
var expect = require("chakram").expect;

describe("authorization tests", function(){
    var baseAuthUrl = "http://localhost:5004";
    var baseIdentityUrl = "http://localhost:5001";

    var installerSecret = "";
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
        "allowedScopes": ["fabric/identity.manageresources"]    
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
        "SecurableItem": "rolesprincipal",
        "Name": "roleBar"
    }    
   
    function registerClientsWithIdentity(){
        var self = this;  

        var registrationApi = {
            "name": "registration-api", 
            "userClaims": ["name","email", "role", "groups"], 
            "scopes": [{ "name": "fabric/identity.manageresources"}]
        }

        return chakram.post(baseIdentityUrl + "/api/apiresource", registrationApi, requestOptions)
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
                    "fabric/authorization.manageclients",
                    "func-test"]
            }
        
            return chakram.post(baseIdentityUrl + "/api/client",installerClient, requestOptions);             
        })
        .then(function(postResponse){            
            self.installerSecret = postResponse.body.clientSecret;            
            return self.installerSecret;
        });
    }

    function getAccessTokenForInstaller(){
        var self = this;
        var postData = {
            form: {
                "client_id": "fabric-installer",
                "client_secret": self.installerSecret,
                "grant_type": "client_credentials",
                "scope": "fabric/identity.manageresources fabric/authorization.read fabric/authorization.write fabric/authorization.manageclients"
            }
        };      

        return chakram.post(baseIdentityUrl + "/connect/token", undefined, postData)
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

        return chakram.post("http://localhost:5001/api/apiresource", authApiResource, authRequestOptions);
    }

    before("running before", function(){
        this.timeout(5000);            

        return registerClientsWithIdentity()
        .then(registerAuthorizationApi())
        .then(function(installerSecret){
            console.log("installer client secret: " + installerSecret);
            return getAccessTokenForInstaller();
        })
        .then(function(retrievedAccessToken){                       
            console.log("access token: " + retrievedAccessToken);
            authRequestOptions.headers.Authorization = retrievedAccessToken;            
        })
        
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
            var identityRegisterResponse = chakram.post(baseIdentityUrl + "/api/client", newIdentityClient, authRequestOptions);           
            expect(identityRegisterResponse).to.have.status(201);
            
            var authRegisterResponse = chakram.post(baseAuthUrl + "/clients", newAuthClient, authRequestOptions);            
            expect(authRegisterResponse).to.have.status(201);    
            
            return chakram.wait();        
        });
    });

    describe("register groups", function(){
        it("should register groups", function(){
            var registerGroupFooResponse = chakram.post(baseAuthUrl + "/groups", newGroupFoo, authRequestOptions);
            expect(registerGroupFooResponse).to.have.status(201);            

            var registerGroupBarResponse = chakram.post(baseAuthUrl + "/groups", newGroupBar, authRequestOptions);
            expect(registerGroupBarResponse).to.have.status(201);           

            return chakram.wait();
        });
    });

    describe("register roles", function(){
        it("should register roles", function(){
            chakram.startDebug();
            var registerRoleFooResponse = chakram.post(baseAuthUrl + "/roles", newRoleFoo, authRequestOptions);
            chakram.stopDebug();
            return expect(registerRoleFooResponse).to.have.status(201);
        });
    });
});