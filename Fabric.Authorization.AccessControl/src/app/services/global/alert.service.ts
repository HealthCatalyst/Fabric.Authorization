import { Injectable } from '@angular/core';
import { HcToasterService, HcToastOptions } from '@healthcatalyst/cashmere';

@Injectable({
  providedIn: 'root'
})
export class AlertService {

  constructor(private toasterService: HcToasterService) { }

  public showSyncWarning(errorMessage: string) {
    const options: HcToastOptions = {
      header: 'Failed to Sync',
      body: 'Changes have been saved, however we were not able to ' +
      'sync the changes to EDW Console. If the user(s) need admin access to EDW Console ' +
      'contact your administrator to manually grant access. The error is: ' + errorMessage,
      position: 'top-right',
      type: 'warning',
      timeout: 10000,
      clickDismiss: true
    };
    this.toasterService.addToast(options);
  }

  public showError(errorMessage: string) {
    const options: HcToastOptions = {
      header: 'System Error',
      body: 'We apologize for the inconvenience, but we encountered an error. The error is: ' + errorMessage,
      position: 'top-right',
      type: 'alert',
      timeout: 8000,
      clickDismiss: true
    };
    this.toasterService.addToast(options);
  }
}
