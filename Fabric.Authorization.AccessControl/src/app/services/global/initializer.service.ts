import { Injectable, Inject } from '@angular/core';
import { IAuthService } from './auth.service';
import { ServicesService } from './services.service';

@Injectable()
export class InitializerService {

  constructor(@Inject('IAuthService')private authService: IAuthService, private servicesService: ServicesService) { }

  initialize() {
    return this.authService.initialize().then(() => {
      return this.servicesService.initialize();
    }).then(() => {
      console.log('Initializer service completed');
    });
  }
}
