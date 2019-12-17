import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class AboutAppService {

  surveyURL: string = "https://healthcatalyst.typeform.com/to/rhGG5U";

  /* UPDATE THESE STRINGS WITH EACH DOS RELEASE */
  dosVersion: string = "19.1";
  docsLink: string = "https://docs.healthcatalyst.com/19-1/articles/dos-access-control/navigate-access-control.html";
  releaseNotes: string = "https://community.healthcatalyst.com/dos/w/releases/4258/dos-19-1-what-changed";

  constructor() { }
}
