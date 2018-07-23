import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { Observable } from 'rxjs/Observable';
import { AuthService } from '../global/auth.service';

@Injectable()
export class AuthenticationGuard implements CanActivate {
  constructor(private authService: AuthService) {}

  canActivate(
    next: ActivatedRouteSnapshot,
    state: RouterStateSnapshot): Observable<boolean> | Promise<boolean> | boolean {
    
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
