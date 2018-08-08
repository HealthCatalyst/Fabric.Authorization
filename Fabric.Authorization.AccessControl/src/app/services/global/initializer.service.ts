import { Injectable, Inject } from '@angular/core';
import { IAuthService } from './auth.service';

@Injectable()
export class InitializerService {

  constructor(@Inject('IAuthService')private authService: IAuthService) { }

  initialize(){
    return this.authService.initialize();
  }

}
