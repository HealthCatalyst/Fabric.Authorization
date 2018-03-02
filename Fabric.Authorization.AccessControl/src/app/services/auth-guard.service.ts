import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { AuthService } from './auth.service';

@Injectable()
export class AuthGuardService {

    constructor(private router: Router, private authService: AuthService) { }
    canActivate() {
        return this.authService.isUserAuthenticated().then((result) => {
            if (result) {
                return true;
            } else {               
                this.authService.login();
                return false;
            }
        });

    }

}
