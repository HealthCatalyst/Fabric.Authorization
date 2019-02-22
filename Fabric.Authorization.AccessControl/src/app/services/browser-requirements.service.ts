import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class BrowserRequirementsService {

  constructor() { }

  cookiesEnabled(): boolean {
    const userAgent = window.navigator.userAgent;
    if (!(userAgent.indexOf('MSIE ') > -1 || userAgent.indexOf('Trident') > -1) &&
        typeof navigator.cookieEnabled !== 'undefined' && navigator.cookieEnabled) {
      return true;
    }
    if (!document.cookie || document.cookie === '') {
      const originalCookieValue = document.cookie;
      document.cookie = 'testCookie';
      const testCookieMatched = (document.cookie.indexOf('testCookie') !== -1);
      document.cookie = originalCookieValue;
      return testCookieMatched;
    }
    return false;
  }
}
