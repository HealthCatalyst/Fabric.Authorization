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

import { ButtonModule, ProgressIndicatorsModule, NavbarModule, PopoverModule, AppSwitcherModule, IconModule, MockAppSwitcherService, ListModule } from '@healthcatalyst/cashmere';
import { NavbarComponent } from './navbar/navbar.component';

@NgModule({
  declarations: [AppComponent, NavbarComponent],
  imports: [BrowserModule, AppRoutingModule, HttpClientModule, ButtonModule, ProgressIndicatorsModule, BrowserAnimationsModule, NavbarModule, PopoverModule, AppSwitcherModule, IconModule],
  providers: [
    AuthService,
    {
      provide: 'IAccessControlConfigService',
      useClass: ClientAccessControlConfigService
    },
    {
      provide: 'IAppSwitcherService',
      useClass: MockAppSwitcherService
    },
    httpInterceptorProviders
  ],
  bootstrap: [AppComponent]
})
export class AppModule {}
