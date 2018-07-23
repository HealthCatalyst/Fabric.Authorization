import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { NgModule } from '@angular/core';
import { HttpClientModule } from '@angular/common/http';

import { AppComponent } from './app.component';
import { AppRoutingModule } from './app-routing.module';

 import { httpInterceptorProviders } from './services/interceptors';
import { AuthService } from '../app/services/global/auth.service';
import { IAccessControlConfigService } from './services/access-control-config.service';
import { ClientAccessControlConfigService } from './services/global/client-access-control-config.service';

import { ButtonModule, ProgressIndicatorsModule } from '@healthcatalyst/cashmere';

@NgModule({
  declarations: [AppComponent],
  imports: [BrowserModule, AppRoutingModule, HttpClientModule, ButtonModule, ProgressIndicatorsModule, BrowserAnimationsModule],
  providers: [
    AuthService,
    {
      provide: 'IAccessControlConfigService',
      useClass: ClientAccessControlConfigService
    },
    httpInterceptorProviders
  ],
  bootstrap: [AppComponent]
})
export class AppModule {}
