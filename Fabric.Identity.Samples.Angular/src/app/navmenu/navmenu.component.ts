import { Component, OnInit } from '@angular/core';
import { AuthService } from '../shared/services/auth.service';

@Component({
  selector: 'app-navmenu',
  templateUrl: './navmenu.component.html',
  styleUrls: ['./navmenu.component.css']
})
export class NavmenuComponent {
    public isUserAuthenticated = false;
    public profile = {};
    constructor(private authService: AuthService) {
        authService.isUserAuthenticated().then(result => {
            this.isUserAuthenticated = result;
        });
        authService.getUser().then(result => {
            if (result) {
                this.profile = result.profile;
            }
        });
    }

  
}
