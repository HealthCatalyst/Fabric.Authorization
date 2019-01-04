var chakram = require("chakram");
var expect = require("chakram").expect;

var webdriver = require("selenium-webdriver"),
    By = webdriver.By,
    until = webdriver.until;

describe("authorization tests", function () {
    var baseAuthUrl = process.env.BASE_AUTH_URL;
    var baseIdentityUrl = process.env.BASE_IDENTITY_URL;
    var fabricInstallerSecret = process.env.FABRIC_INSTALLER_SECRET;    

    if (!baseAuthUrl) {
        baseAuthUrl = "http://localhost:5004";
    }

    if (!baseIdentityUrl) {
        baseIdentityUrl = "http://localhost:5001";
    }

    var newClientSecret = "";
    var funcTestAuthClientAccessToken = "";

    var authRequestOptions = {
        headers: {
            "Content-Type": "application/json",
            "Accept": "application/json",
            "Authorization": ""
        }
    }
    var requestOptions = {
        headers: {
            "Content-Type": "application/json"
        }
    }

    var identityClientFuncTest = {
        "clientId": "func-test",
        "clientName": "Functional Test Client",
        "requireConsent": "false",
        "allowedGrantTypes": ["client_credentials", "password"],
        "allowedScopes": [
            "fabric/identity.manageresources",
            "fabric/authorization.read",
            "fabric/authorization.write",
            "openid",
            "profile"
        ]
    }

    var authClientFuncTest = {
        "id": "func-test",
        "name": "Functional Test Client",
        "topLevelSecurableItem": { "name": "func-test" }
    }

    var groupNonCustom = {
        "groupName": "FABRIC\\\Health Catalyst Non-Custom",
        "groupSource": "Directory"
    }

    var roleNonCustom = {
        "Grain": "app",
        "SecurableItem": "func-test",
        "Name": "FABRIC\\\Health Catalyst Non-Custom"
    }

    // all of the groups below are set with Source="Custom" in the setup-samples.sh script
    var groupHcViewer = {
        "groupName": "FABRIC\\\Health Catalyst Viewer",
        "groupSource": "custom"
    }

    var groupHcEditor = {
        "groupName": "FABRIC\\\Health Catalyst Editor",
        "groupSource": "custom"
    }

    var groupHcAdmin = {
        "groupName": "FABRIC\\\Health Catalyst Admin",
        "groupSource": "custom"
    }

    var userBob = {
        "subjectId": "88421113",
        "identityProvider": "test"
    }

    var roleHcViewer = {
        "Grain": "app",
        "SecurableItem": "func-test",
        "Name": "FABRIC\\\Health Catalyst Viewer"
    }

    var roleHcEditor = {
        "Grain": "app",
        "SecurableItem": "func-test",
        "Name": "FABRIC\\\Health Catalyst Editor"
    }

    var roleHcAdmin = {
        "Grain": "app",
        "SecurableItem": "func-test",
        "Name": "FABRIC\\\Health Catalyst Admin"
    }

    var userCanViewPermission = {
        "Grain": "app",
        "SecurableItem": "func-test",
        "Name": "userCanView"
    }

    var userCanEditPermission = {
        "Grain": "app",
        "SecurableItem": "func-test",
        "Name": "userCanEdit"
    }

    function getAccessToken(clientData) {
        return chakram.post(baseIdentityUrl + "/connect/token", undefined, clientData)
            .then(function (postResponse) {
                var accessToken = "Bearer " + postResponse.body.access_token;
                return accessToken;
            });
    }

    function getAccessTokenForInstaller(installerClientSecret) {
        var postData = {
            form: {
                "client_id": "fabric-installer",
                "client_secret": installerClientSecret,
                "grant_type": "client_credentials",
                "scope": "fabric/identity.manageresources fabric/authorization.read fabric/authorization.write fabric/authorization.manageclients"
            }
        };        

        return getAccessToken(postData);
    }

    function getAccessTokenForAuthClient(secret) {
        var clientData = {
            form: {
                "client_id": "func-test",
                "client_secret": secret,
                "grant_type": "client_credentials",
                "scope": "fabric/authorization.read fabric/authorization.write"
            }
        }

        return getAccessToken(clientData);
    }

    function bootstrapIdentityServer() {        
        return getAccessTokenForInstaller(fabricInstallerSecret)
            .then(function (retrievedAccessToken) {
                authRequestOptions.headers.Authorization = retrievedAccessToken;
            });
    }

    before("running before", function () {
        this.timeout(5000);
        return bootstrapIdentityServer();
    });

    describe("register client", function () {

        it("should register a client", function () {
            this.timeout(4000);
            return chakram.post(baseIdentityUrl + "/api/client", identityClientFuncTest, authRequestOptions)
                .then(function (clientResponse) {
                    expect(clientResponse).to.have.status(201);
                    newClientSecret = clientResponse.body.clientSecret;
                    return getAccessTokenForAuthClient(clientResponse.body.clientSecret);
                })
                .then(function (authClientAccessToken) {
                    funcTestAuthClientAccessToken = authClientAccessToken;
                })
                .then(function () {
                    return chakram.post(baseAuthUrl + "/clients", authClientFuncTest, authRequestOptions);
                })
                .then(function (clientResponse) {
                    expect(clientResponse).to.have.status(201);
                });
        });
    });

    describe("register groups", function () {
        it("should return 409 for group HC Editor (already exists)", function () {
            var registerGroupHcEditorResponse = chakram.post(baseAuthUrl + "/groups", groupHcEditor, authRequestOptions);
            return expect(registerGroupHcEditorResponse).to.have.status(409);
        });

        it("should return 409 for group HC Viewer (already exists)", function () {
            var registerGroupHcViewerResponse = chakram.post(baseAuthUrl + "/groups", groupHcViewer, authRequestOptions);
            return expect(registerGroupHcViewerResponse).to.have.status(409);
        });

        it("should register group HC Admin", function () {
            var registerGroupHcAdminResponse = chakram.post(baseAuthUrl + "/groups", groupHcAdmin, authRequestOptions);
            return expect(registerGroupHcAdminResponse).to.have.status(201);
        });

        it("should register group Non-Custom", function () {
            var registerGroupNonCustomResponse = chakram.post(baseAuthUrl + "/groups", groupNonCustom, authRequestOptions);
            return expect(registerGroupNonCustomResponse).to.have.status(201);
        });
    });

    describe("register roles", function () {
        it("should register role HC Viewer", function () {
            authRequestOptions.headers.Authorization = funcTestAuthClientAccessToken;

            var registerRoleHcViewerResponse = chakram.post(baseAuthUrl + "/roles", roleHcViewer, authRequestOptions);
            return expect(registerRoleHcViewerResponse).to.have.status(201);
        });

        it("should register role HC Editor", function () {
            authRequestOptions.headers.Authorization = funcTestAuthClientAccessToken;

            var registerRoleHcEditorResponse = chakram.post(baseAuthUrl + "/roles", roleHcEditor, authRequestOptions);
            return expect(registerRoleHcEditorResponse).to.have.status(201);
        });

        it("should register role HC Admin", function () {
            authRequestOptions.headers.Authorization = funcTestAuthClientAccessToken;

            var registerRoleHcAdminResponse = chakram.post(baseAuthUrl + "/roles", roleHcAdmin, authRequestOptions);
            return expect(registerRoleHcAdminResponse).to.have.status(201);
        });

        it("should register role Non-Custom", function () {
            authRequestOptions.headers.Authorization = funcTestAuthClientAccessToken;

            var registerRoleNonCustomResponse = chakram.post(baseAuthUrl + "/roles", roleNonCustom, authRequestOptions);
            return expect(registerRoleNonCustomResponse).to.have.status(201);
        });
    });

    describe("register permissions", function () {
        it("should register permission userCanView", function () {
            authRequestOptions.headers.Authorization = funcTestAuthClientAccessToken;

            var registerPermissionResponse = chakram.post(baseAuthUrl + "/Permissions", userCanViewPermission, authRequestOptions);
            return expect(registerPermissionResponse).to.have.status(201);
        });

        it("should register permission userCanEdit", function () {
            authRequestOptions.headers.Authorization = funcTestAuthClientAccessToken;

            var registerPermissionResponse = chakram.post(baseAuthUrl + "/Permissions", userCanEditPermission, authRequestOptions);
            return expect(registerPermissionResponse).to.have.status(201);
        });
    });

    describe("associate groups to roles", function () {
        it("should associate group HC Viewer with role HC Viewer", function () {
            authRequestOptions.headers.Authorization = funcTestAuthClientAccessToken;

            return chakram.get(baseAuthUrl + "/roles/" + roleHcViewer.Grain + "/" + roleHcViewer.SecurableItem + "/" + encodeURIComponent(roleHcViewer.Name), authRequestOptions)
                .then(function (getResponse) {
                    expect(getResponse).to.have.status(200);
                    expect(getResponse).to.comprise.of.json([{ name: "FABRIC\\Health Catalyst Viewer" }]);

                    return getResponse.body;
                })
                .then(function (role) {
                    return chakram.post(baseAuthUrl + "/groups/" + encodeURIComponent(groupHcViewer.groupName) + "/roles", [role[0]], authRequestOptions);
                })
                .then(function (postResponse) {
                    expect(postResponse).to.have.status(200);
                });
        });

        it("should associate group HC Editor with role HC Editor", function () {
            authRequestOptions.headers.Authorization = funcTestAuthClientAccessToken;

            return chakram.get(baseAuthUrl + "/roles/" + roleHcEditor.Grain + "/" + roleHcEditor.SecurableItem + "/" + encodeURIComponent(roleHcEditor.Name), authRequestOptions)
                .then(function (getResponse) {
                    expect(getResponse).to.have.status(200);
                    expect(getResponse).to.comprise.of.json([{ name: "FABRIC\\Health Catalyst Editor" }]);
                    return getResponse.body;
                })
                .then(function (role) {
                    return chakram.post(baseAuthUrl + "/groups/" + encodeURIComponent(groupHcEditor.groupName) + "/roles", [role[0]], authRequestOptions);
                })
                .then(function (postResponse) {
                    expect(postResponse).to.have.status(200);
                });
        });

        it("should associate group HC Admin with role HC Admin", function () {
            authRequestOptions.headers.Authorization = funcTestAuthClientAccessToken;

            return chakram.get(baseAuthUrl + "/roles/" + roleHcAdmin.Grain + "/" + roleHcAdmin.SecurableItem + "/" + encodeURIComponent(roleHcAdmin.Name), authRequestOptions)
                .then(function (getResponse) {
                    expect(getResponse).to.have.status(200);
                    expect(getResponse).to.comprise.of.json([{ name: "FABRIC\\Health Catalyst Admin" }]);
                    return getResponse.body;
                })
                .then(function (role) {
                    return chakram.post(baseAuthUrl + "/groups/" + encodeURIComponent(groupHcAdmin.groupName) + "/roles", [role[0]], authRequestOptions);
                })
                .then(function (postResponse) {
                    expect(postResponse).to.have.status(200);
                });
        });

        it("should associate group Non-Custom with role Non-Custom", function () {
            authRequestOptions.headers.Authorization = funcTestAuthClientAccessToken;

            return chakram.get(baseAuthUrl + "/roles/" + roleNonCustom.Grain + "/" + roleNonCustom.SecurableItem + "/" + encodeURIComponent(roleNonCustom.Name), authRequestOptions)
                .then(function (getResponse) {
                    expect(getResponse).to.have.status(200);
                    expect(getResponse).to.comprise.of.json([{ name: "FABRIC\\Health Catalyst Non-Custom" }]);
                    return getResponse.body;
                })
                .then(function (role) {
                    return chakram.post(baseAuthUrl + "/groups/" + encodeURIComponent(groupNonCustom.groupName) + "/roles", [role[0]], authRequestOptions);
                })
                .then(function (postResponse) {
                    expect(postResponse).to.have.status(200);
                });
        });
    });

    describe("associate users to groups", function () {
        it("should return 400 when no subjectId provided", function () {
            authRequestOptions.headers.Authorization = funcTestAuthClientAccessToken;

            return chakram.post(baseAuthUrl + "/groups/" + encodeURIComponent(groupHcEditor.groupName) + "/users", [{ "identityProvider": "Windows" }], authRequestOptions)
                .then(function (postResponse) {
                    expect(postResponse).to.have.status(400);
                });
        });

        it("should return 400 when no identityProvider provided", function () {
            authRequestOptions.headers.Authorization = funcTestAuthClientAccessToken;

            return chakram.post(baseAuthUrl + "/groups/" + encodeURIComponent(groupHcEditor.groupName) + "/users", [{ "subjectId": "first.last@gmail.com" }], authRequestOptions)
                .then(function (postResponse) {
                    expect(postResponse).to.have.status(400);
                });
        });

        it("should return 400 when associating user with non-custom group", function () {
            authRequestOptions.headers.Authorization = funcTestAuthClientAccessToken;

            return chakram.post(baseAuthUrl + "/groups/" + encodeURIComponent(groupNonCustom.groupName) + "/users", [userBob], authRequestOptions)
                .then(function (postResponse) {
                    expect(postResponse).to.have.status(400);
                });
        });

        it("should associate user with group HC Admin", function () {
            authRequestOptions.headers.Authorization = funcTestAuthClientAccessToken;

            return chakram.post(baseAuthUrl + "/groups/" + encodeURIComponent(groupHcAdmin.groupName) + "/users", [userBob], authRequestOptions)
                .then(function (postResponse) {
                    expect(postResponse).to.have.status(200);
                });
        });
    });

    describe("search identities", function () {
        it("should return a 404 when client_id does not exist", function () {
            var options = {
                headers: {
                    "Accept": "application/json",
                    "Authorization": funcTestAuthClientAccessToken
                }
            }

            return chakram.get(baseAuthUrl + "/members?client_id=blah", options)
                .then(function (getResponse) {
                    expect(getResponse).to.have.status(404);
                });
        });

        it("should return 200 and results with valid request", function () {
            this.timeout(20000);

            function loginUser() {
                //setup custom phantomJS capability
                var phantomjsExe = require("phantomjs-prebuilt").path;
                var customPhantom = webdriver.Capabilities.phantomjs();
                customPhantom.set("phantomjs.binary.path", phantomjsExe);

                //build custom phantomJS driver
                var driver = new webdriver.Builder().withCapabilities(customPhantom).build();

                driver.manage().window().setSize(1024, 768);
                var encodedRedirectUri = encodeURIComponent(baseIdentityUrl);
                return driver.get(baseIdentityUrl +
                        "/account/login?returnUrl=%2Fconnect%2Fauthorize%2Flogin%3Fclient_id%3Dfunc-test%26redirect_uri%3D" +
                        encodedRedirectUri +
                        "%26response_type%3Did_token%2520token%26scope%3Dopenid%2520profile%2520fabric%252Fauthorization.read%2520fabric%252Ffabric%252Fauthorization.write%26nonce%3Dd9bfc7af239b4e99b18cb08f69f77377")
                    .then(function () {

                        var timeout = 2000;

                        return driver.wait(function () {
                            return driver.findElement(By.id("Username")).isDisplayed();
                        }, timeout)
                        .then(function () {
                            return driver.findElement(By.id("Username")).sendKeys("bob");
                        })
                        .then(function() {
                            return driver.wait(function () {
                                return driver.findElement(By.id("Password")).isDisplayed();
                            }, timeout)
                        })
                        .then(function () {
                            return driver.findElement(By.id("Password")).sendKeys("bob");
                        })
                        .then(function () {
                            return driver.wait(function () {
                                return driver.findElement(By.id("login_but")).isDisplayed();
                            }, timeout)
                        })
                        .then(function() {
                            return driver.findElement(By.id("login_but")).click();
                        });
                    })
                    .then(function() {
                        return driver.getCurrentUrl();
                    });
            }

            var options = {
                headers: {
                    "Accept": "application/json",
                    "Authorization": funcTestAuthClientAccessToken
                }
            }

            return loginUser()
                .then(function () {
                    return chakram.get(baseAuthUrl + "/members?client_id=" + authClientFuncTest.id + "&sort_key=name&page_size=2",
                        options);
                })
                .then(function (getResponse) {
                    expect(getResponse).to.have.status(200);
                    var searchResult = getResponse.body;
                    var results = searchResult.results;                    
                    expect(results).to.be.an("array").that.is.not.empty;
                    expect(results.length).to.equal(2);                              
                    expect(searchResult.totalCount).to.equal(4);

                    var groupHcAdminResult = results[0];
                    expect(groupHcAdminResult.groupName).to.equal(groupHcAdmin.groupName);
                    expect(groupHcAdminResult.entityType).to.equal("CustomGroup");
                    expect(groupHcAdminResult.roles).to.be.an("array").that.is.not.empty;
                    expect(groupHcAdminResult.roles.length).to.equal(1);
                    expect(groupHcAdminResult.roles[0].name).to.equal(roleHcAdmin.Name);

                    var groupHcEditorResult = results[1];
                    expect(groupHcEditorResult.groupName).to.equal(groupHcEditor.groupName);
                    expect(groupHcEditorResult.entityType).to.equal("CustomGroup");
                    expect(groupHcEditorResult.roles).to.be.an("array").that.is.not.empty;
                    expect(groupHcEditorResult.roles.length).to.equal(1);
                    expect(groupHcEditorResult.roles[0].name).to.equal(roleHcEditor.Name);
                });
        });
    });

    describe("associate roles to permissions", function () {
        it("should associate roleHcViewer with userCanViewPermission and userCanEditPermission", function () {
            authRequestOptions.headers.Authorization = funcTestAuthClientAccessToken;
            var permissions = [];

            return chakram.get(baseAuthUrl + "/permissions/" + userCanViewPermission.Grain + "/" + userCanViewPermission.SecurableItem, authRequestOptions)
                .then(function (getResponse) {
                    expect(getResponse).to.have.status(200);
                    permissions = getResponse.body;
                    return chakram.get(baseAuthUrl + "/roles/" + roleHcViewer.Grain + "/" + roleHcViewer.SecurableItem + "/" + encodeURIComponent(roleHcViewer.Name), authRequestOptions);
                })
                .then(function (getResponse) {
                    expect(getResponse).to.have.status(200);
                    expect(getResponse).to.comprise.of.json([{ name: "FABRIC\\Health Catalyst Viewer" }]);
                    return getResponse.body;
                })
                .then(function (role) {
                    var roleId = role[0].id;
                    return chakram.post(baseAuthUrl + "/roles/" + roleId + "/permissions", permissions, authRequestOptions);
                })
                .then(function (postResponse) {
                    expect(postResponse).to.comprise.of.json({ name: "FABRIC\\Health Catalyst Viewer" });
                    expect(postResponse).to.have.status(201);
                });
        });
    });

    describe("get user permissions", function () {
        it("can get the users permissions", function () {
            //hit the token endpoint for identity with the username and password of the user
            var stringToEncode = "func-test:" + newClientSecret;
            var encodedData = new Buffer(stringToEncode).toString("base64");

            var postData = {
                form: {
                    "grant_type": "password",
                    "username": "bob",
                    "password": "bob"
                },
                headers: {
                    "content-type": "application/x-www-form-urlencoded",
                    "Authorization": "Basic " + encodedData
                }
            };

            return getAccessToken(postData)
                .then(function (accessToken) {
                    expect(accessToken).to.not.be.null;
                    var headers = {
                        headers: {
                            "Accept": "application/json",
                            "Authorization": accessToken
                        }
                    };
                    return chakram.get(baseAuthUrl + "/user/permissions?grain=app&securableItem=func-test", headers);
                })
                .then(function (getResponse) {
                    expect(getResponse).to.have.status(200);

                    var permissions = getResponse.body.permissions;
                    expect(permissions).to.be.an("array").that.is.not.empty;
                    expect(permissions).to.be.an("array").that.includes("app/func-test.userCanEdit");
                    expect(permissions).to.be.an("array").that.includes("app/func-test.userCanView");
                });
        });
    });

    describe("validate security", function () {
        it("should return a 403 when no access token provided", function () {
            return chakram.get(baseAuthUrl + "/clients", requestOptions)
                .then(function (getResponse) {
                    expect(getResponse).to.have.status(403);
                });
        });

        it("should return a 403 when an access token with invalid scope is used", function () {
            return getAccessTokenForAuthClient(newClientSecret)
                .then(function (accessToken) {
                    var options = {
                        headers: {
                            "content-type": "application/json",
                            "Authorization": accessToken
                        }
                    }
                    return chakram.get(baseAuthUrl + "/clients", options);
                })
                .then(function (getResponse) {
                    expect(getResponse).to.have.status(403);
                });
        });
    });
});