import { Component, OnInit } from '@angular/core';
import { AuthserviceService } from '../../../services/authservice.service';

@Component({
  selector: 'app-useradd',
  templateUrl: './useradd.component.html',
  styleUrls: ['./useradd.component.css']
})
export class UseraddComponent implements OnInit {

  message: string;

  constructor(private authService: AuthserviceService) { 
    this.message = this.authService.foo('user add reporting!');
  }

  ngOnInit() {
  }

}
