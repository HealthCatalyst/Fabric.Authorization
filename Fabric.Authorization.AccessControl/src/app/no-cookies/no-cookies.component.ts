import { Component, OnInit, TemplateRef } from '@angular/core';
import { ModalService, ModalOptions } from '@healthcatalyst/cashmere';
import { BrowserRequirementsService } from '../services/browser-requirements.service';
import { AboutAppService } from '../services/about-app.service';

@Component({
  selector: 'app-root',
  templateUrl: './no-cookies.component.html',
  styleUrls: ['./no-cookies.component.scss']
})
export class NoCookiesComponent implements OnInit {
  currentYear: number;
  surveyURL: string;

  constructor(
    private browserRequirements: BrowserRequirementsService,
    private modalService: ModalService,
    private appInfo: AboutAppService
  ) {
    this.surveyURL = appInfo.surveyURL + "?app_version=" + this.appInfo.dosVersion;
  }

  ngOnInit() {
    let today = new Date();
    this.currentYear = today.getFullYear();

    setInterval(() => {
      if (this.browserRequirements.cookiesEnabled()) {
        this.ngOnInit();
      }
    }, 1000);
  }

  reload() {
    window.location.reload();
  }

  aboutApp(content: TemplateRef<any>) {
    let options: ModalOptions = {
      size: 'md'
    };
    this.modalService.open(content, options);
  }
}
