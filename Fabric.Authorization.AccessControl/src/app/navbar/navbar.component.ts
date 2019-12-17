import { Component, OnInit, Inject, TemplateRef } from '@angular/core';
import { ModalService, ModalOptions } from '@healthcatalyst/cashmere';
import { AboutAppService } from '../services/about-app.service';
import { User } from 'oidc-client';

import { IAuthService } from '../services/global/auth.service';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss']
})
export class NavbarComponent implements OnInit {
  userDisplayName: string;
  userIsAuthenticated: boolean;
  identityProvider: string;
  organization: string;
  currentYear: number;

  constructor(
    @Inject('IAuthService')private authService: IAuthService,
    private modalService: ModalService,
    private appInfo: AboutAppService
  ) {}

  ngOnInit() {
    let today = new Date();
    this.currentYear = today.getFullYear();

    this.authService.getUser().then(user => {
      this.userDisplayName = this.getUserDisplayName(user);
    });

    this.authService.isUserAuthenticated().then(result => {
      this.userIsAuthenticated = result;
    });
  }

  logout(): void {
    this.authService.logout();
  }

  getUserDisplayName(user: User): string {
    if (user && user.profile) {
      this.identityProvider = user.profile.idp;
      if( user.profile.email ) {
        let email = user.profile.email;
        let domain = email.substring(email.lastIndexOf("@")+1);
        let org = domain.split(".");
        this.organization = org[0];
      }
      if (user.profile.family_name && user.profile.given_name) {
        return (user.profile.given_name + ' ' + user.profile.family_name);
      } else if (user.profile.idp === 'AzureActiveDirectory') {
        return this.parseAzureADProfileName(user.profile);
      } else {
        return user.profile.name;
      }
    }
    return '';
  }

  aboutApp(content: TemplateRef<any>) {
    let options: ModalOptions = {
      size: 'md'
    };
    this.modalService.open(content, options);
  }

  get surveyURL() {
    let tempURL = this.appInfo.surveyURL;
    tempURL += "?app_version=" + this.appInfo.dosVersion;
    if( this.userDisplayName ) {
      tempURL += "&user=" + this.userDisplayName;
    }
    if( this.organization ) {
      tempURL += "&org=" + this.organization;
    }
    return tempURL;
  }

  /*
  Function: parseAzureADProfileName
    This is complicated code.  Meant to parse the Profile object of a user from
    Azure AD. It will look to see if it is an array and decided if it is able
    to find a non-email name.  If it cant find that non-email name then it
    will use the User Principal name,  i.e. email@domain
  Parameters: profile, the profile of the user from OIDC
  Returns: string, the name to use in the nav bar for Azure AD
  */
  parseAzureADProfileName(profile: any): string {
    // 0) if the profile does not have the name property,
    // then it should return no name detected.  This should
    // not be the case.
    if (!profile.hasOwnProperty('name') || profile.name === null) {
      return 'no name detected';
    }

    // 1) if the profile does not have an array,
    // then return the name completely
    if (!Array.isArray(profile.name)) {
      return profile.name;
    }

    // 2) if the profile has 1 item in the array,
    // then return the first item.
    if (profile.name.length === 1) {
      return profile.name[0];
    } else {
      // if the profile has multiple items, look for a non-email domain name
      // 3) if you find it, immediately return it as the name to use.
      let indexOfUserPrincipal = -1;
      for (let i = 0; i < profile.name.length; i++ ) {
        if (profile.name[i].indexOf('@') > 0) {
          indexOfUserPrincipal = i;
        } else {
          return profile.name[i];
        }
      }

      // 4) If you dont find the non-email domain name, but you find the
      // domain name, i.e. name@domain, then use that.
      // 5) If you cant find anything, then just return the profile name.
      if (indexOfUserPrincipal > -1) {
        return profile.name[indexOfUserPrincipal];
      } else {
        return profile.name[0];
      }
    }
  }
}
