import { Component, OnInit } from '@angular/core';

import { AuthService } from '../services/global/auth.service';
import { AccessControlConfigService } from '../services/access-control-config.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  profile = {}; 
  clientId: string;

  constructor(private authService: AuthService, private configService: AccessControlConfigService) { }

  ngOnInit() {
    this.authService.getUser().then(result => {
      if (result) {
          this.profile = result.profile;
      }
    });
    this.clientId = this.configService.clientId;
  }
}
