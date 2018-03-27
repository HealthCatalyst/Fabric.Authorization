import { Component, OnInit } from '@angular/core';
import { AuthService } from '../services/global/auth.service';

@Component({
  selector: 'app-logout',
  templateUrl: './logout.component.html',
  styleUrls: ['./logout.component.css']
})
export class LogoutComponent implements OnInit {
  constructor(private authService: AuthService) {
    this.authService.logout();
  }

  ngOnInit() {}
}
