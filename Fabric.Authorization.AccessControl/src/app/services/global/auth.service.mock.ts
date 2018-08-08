import { IAuthService } from '../global/auth.service';
import { UserManager, User } from 'oidc-client';

export class MockAuthService implements IAuthService {
    userManager: UserManager;
    identityClientSettings: any;
    clientId: string;
    authority: string;

    initialize(): Promise<any>{
      return new Promise((resolve) =>{
        resolve(true);
      })
    }

    login(){
      return;
    }

    logout(){
      return
    }

    handleSigninRedirectCallback(){
      return;
    }

    getUser(): Promise<User>{
      const user = <User>{};
      return new Promise((resolve) =>{
        resolve(user);
      });
    }

    isUserAuthenticated(): Promise<boolean> {
      return new Promise((resolve) =>{
        resolve(true);
      })
    }
}