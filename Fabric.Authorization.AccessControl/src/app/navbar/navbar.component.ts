import { Component, OnInit, Inject } from '@angular/core';

import { AuthService } from '../services/global/auth.service';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss']
})
export class NavbarComponent implements OnInit {
  userDisplayName: string;

  constructor(
    private authService: AuthService
  ) {}

  ngOnInit() {
    this.authService.getUser().then(result => {
      if (result && result.profile) {
        debugger;
        if(result.profile.family_name && result.profile.given_name){
          this.userDisplayName = result.profile.given_name + ' ' + result.profile.family_name;
        }else{
          this.userDisplayName = result.profile.name;
        }
      }
    });
  }
}
