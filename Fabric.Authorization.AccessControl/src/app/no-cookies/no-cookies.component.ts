import { Component, OnInit } from '@angular/core';
import { BrowserRequirementsService } from '../services/browser-requirements.service';

@Component({
  selector: 'app-root',
  templateUrl: './no-cookies.component.html',
  styleUrls: ['./no-cookies.component.scss']
})
export class NoCookiesComponent implements OnInit {

  constructor(
    private browserRequirements: BrowserRequirementsService
  ) { }

  ngOnInit() {
    setInterval(() => {
      if (this.browserRequirements.cookiesEnabled()) {
        this.ngOnInit();
      }
    }, 1000);
  }

  reload() {
    window.location.reload();
  }
}
