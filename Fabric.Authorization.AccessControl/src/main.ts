import { enableProdMode } from '@angular/core';
import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';

import { AppModule } from './app/app.module';
import { environment } from './environments/environment';

import { BrowserRequirementsService } from './app/services/browser-requirements.service';
import { bootstrapNoCookiesAppModule } from './app/no-cookies.app.module';

if (environment.production) {
  enableProdMode();
}

const browserRequirementsService = new BrowserRequirementsService();
if (browserRequirementsService.cookiesEnabled()) {
  platformBrowserDynamic()
    .bootstrapModule(AppModule)
    .catch(err => console.log(err));
} else {
  bootstrapNoCookiesAppModule();
}
