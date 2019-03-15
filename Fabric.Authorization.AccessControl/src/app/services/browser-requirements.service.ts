import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class BrowserRequirementsService {

  constructor() { }

  cookiesEnabled(): boolean {
    const userAgent = window.navigator.userAgent;
    const isIE = (userAgent.indexOf('MSIE') > -1 || userAgent.indexOf('Trident') > -1);
    if (isIE && document.cookie && document.cookie.length > 0) {
      return true;
    } else if (isIE === false && typeof navigator.cookieEnabled !== 'undefined' && navigator.cookieEnabled) {
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
