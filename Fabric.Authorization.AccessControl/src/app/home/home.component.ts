import { Component, OnInit } from '@angular/core';
import { AuthService } from '../services/global/auth.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  profile = {}; 

  constructor(private authService: AuthService) { }

  ngOnInit() {
    this.authService.getUser().then(result => {
      if (result) {
          this.profile = result.profile;
      }
    });
  }
}
