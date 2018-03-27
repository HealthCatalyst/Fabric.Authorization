import { Component, OnInit } from '@angular/core';
import { AuthService } from '../services/global/auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {
  constructor(private authService: AuthService) {
    this.authService.login();
  }

  ngOnInit() {}
}
