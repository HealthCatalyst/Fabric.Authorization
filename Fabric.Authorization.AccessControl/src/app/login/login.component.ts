import { Component, OnInit } from '@angular/core';
import { AuthService } from '../services/global/auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html'
})
export class LoginComponent implements OnInit {
  public status = '';
  public loggingIn = true;
  constructor(private authService: AuthService) {
  }

  ngOnInit() {
    this.authService.getUser().then((userCheck) => {
      if (userCheck) {
        this.loggingIn = false;
        this.status = `${userCheck.profile.name} logged in`;
      } else {
        sessionStorage.setItem('redirect', window.location.href);
        this.authService.login().then((user) => {
          this.status = 'Redirecting...';
          this.loggingIn = false;
        }, (error) => {
          this.status = `Unable to login: ${error}`;
          this.loggingIn = false;
        });
      }
    });
  }
}
