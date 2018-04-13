import { Component, OnInit, Inject } from '@angular/core';

import { AuthService } from '../services/global/auth.service';
import { IAccessControlConfigService } from '../services/access-control-config.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html'
})
export class HomeComponent implements OnInit {
  profile: any;
  clientId: string;

  constructor(
    private authService: AuthService,
    @Inject('IAccessControlConfigService') private configService: IAccessControlConfigService
  ) {}

  ngOnInit() {
    this.authService.getUser().then(result => {
      if (result) {
        this.profile = result.profile;
      }
    });
    this.clientId = this.configService.clientId;
  }
}
