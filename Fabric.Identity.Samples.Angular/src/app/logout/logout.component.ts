import { Component, OnInit } from '@angular/core';
import { AuthService } from '../shared/services/auth.service'

@Component({
  selector: 'app-logout',
  templateUrl: './logout.component.html',
  styleUrls: ['./logout.component.css']
})
export class LogoutComponent{

  constructor(private authService: AuthService) {
      this.authService.logout();
  }
    
}
