import { Injectable, Inject } from '@angular/core';
import { IAuthService } from './auth.service';
import { ServicesService } from './services.service';

@Injectable()
export class InitializerService {

  constructor(@Inject('IAuthService')private authService: IAuthService, private servicesServce: ServicesService) { }

  initialize() {
    return this.authService.initialize()
      .then(() => this.servicesServce.initialize());
  }
}
