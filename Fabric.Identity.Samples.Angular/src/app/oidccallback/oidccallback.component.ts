import { Component, OnInit } from '@angular/core';
import { AuthService } from '../shared/services/auth.service'
import { Router } from '@angular/router';

@Component({
  selector: 'app-oidccallback',
  templateUrl: './oidccallback.component.html',
  styleUrls: ['./oidccallback.component.css']
})
export class OidccallbackComponent implements OnInit {

  constructor(private authService: AuthService, private router: Router) {
      authService.handleSigninRedirectCallback();
      router.navigate(['home']);
  }

  ngOnInit() {
  }

}
