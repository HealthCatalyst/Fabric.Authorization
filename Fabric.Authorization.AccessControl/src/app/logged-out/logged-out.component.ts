import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-logged-out',
  templateUrl: './logged-out.component.html',
  styleUrls: ['./logged-out.component.scss']
})
export class LoggedOutComponent implements OnInit {

  constructor(private router: Router) { }

  ngOnInit() {
  }

  goToHomepage() {
    this.router.navigateByUrl('/access-control');
}

}
