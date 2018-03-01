import { Component, OnInit } from '@angular/core';
import { AuthserviceService } from '../../../services/authservice.service';

@Component({
  selector: 'app-userlist',
  templateUrl: './userlist.component.html',
  styleUrls: ['./userlist.component.css']
})
export class UserlistComponent implements OnInit {

  message: string;

  constructor(private authService: AuthserviceService) { 
    this.message = this.authService.foo('user list reporting!');
  }

  ngOnInit() {
  }

}
