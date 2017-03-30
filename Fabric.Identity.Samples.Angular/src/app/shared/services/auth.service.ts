import { Injectable } from '@angular/core';
import { UserManager, Log, MetadataService, User } from 'oidc-client';

@Injectable()
export class AuthService {
    userManager: UserManager = new UserManager(clientSettings);

    constructor() { }

    login() {
        this.userManager.signinRedirect().then(() => {
            console.log("signin redirect done");
        }).catch(err => {
            console.log(err);
        });
    }

    logout() {
        this.userManager.signoutRedirect();
    }

    handleSigninRedirectCallback() {
        this.userManager.signinRedirectCallback().then(user => {
            if (user) {
                console.log("Logged in", user.profile);
            } else {
                console.log("could not log user in");
            }
        }).catch(e => {
            console.error(e);
        });
    }

    getUser(): Promise<User> {
        return this.userManager.getUser();
    }

    isUserAuthenticated() {
        return this.userManager.getUser().then(function(user) {
            if (user) {
                console.log("User logged in", user.profile);
                return true;
            } else {
                console.log("User is not logged in");
                return false;
            }
        });
    }

}

const clientSettings: any = {
    authority: 'http://localhost:5001/',
    client_id: 'fabric-angularsample',
    redirect_uri: 'http://localhost:4200/oidc-callback',
    post_logout_redirect_uri: 'http://localhost:4200/',
    response_type: 'id_token token',
    scope: 'openid profile fabric.profile patientapi',

    silent_redirect_uri: 'http://localhost:4200',
    automaticSilentRenew: true,
    //silentRequestTimeout:10000,

    filterProtocolClaims: true,
    loadUserInfo: true
};