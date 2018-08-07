import { Component, OnInit, Inject } from '@angular/core';
import { User } from 'oidc-client';

import { AuthService } from '../services/global/auth.service';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss']
})
export class NavbarComponent implements OnInit {
  userDisplayName: string;
  userIsAuthenticated: boolean;

  constructor(
    private authService: AuthService
  ) {}

  ngOnInit() {
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
      if (user.profile.family_name && user.profile.given_name) {
        return (user.profile.given_name + ' ' + user.profile.family_name);
      }else {
        return user.profile.name;
      }
    }
    return '';
  }
}
