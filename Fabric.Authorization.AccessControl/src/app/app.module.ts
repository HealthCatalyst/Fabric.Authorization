import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { NgModule, APP_INITIALIZER } from '@angular/core';
import { HttpClientModule } from '@angular/common/http';

import { AppComponent } from './app.component';
import { AppRoutingModule } from './app-routing.module';

import { httpInterceptorProviders } from './services/interceptors';
import { AuthService } from './services/global/auth.service';
import { ClientAccessControlConfigService } from './services/global/client-access-control-config.service';

import { ButtonModule, ProgressIndicatorsModule, NavbarModule, PopoverModule, AppSwitcherModule, IconModule,
  MockAppSwitcherService } from '@healthcatalyst/cashmere';
import { NavbarComponent } from './navbar/navbar.component';
import { LoggedOutComponent } from './logged-out/logged-out.component';
import { ServicesService } from './services/global/services.service';
import { ConfigService } from './services/global/config.service';
import { InitializerService } from './services/global/initializer.service';

@NgModule({
  declarations: [AppComponent, NavbarComponent, LoggedOutComponent],
  imports: [BrowserModule, AppRoutingModule, HttpClientModule, ButtonModule, ProgressIndicatorsModule, BrowserAnimationsModule,
    NavbarModule, PopoverModule, AppSwitcherModule, IconModule],
  providers: [
    {
      provide: 'IAuthService',
      useClass: AuthService
    },
    {
      provide: 'IAccessControlConfigService',
      useClass: ClientAccessControlConfigService
    },
    ServicesService,
    ConfigService,
    InitializerService,
    {
      provide: 'IAppSwitcherService',
      useClass: MockAppSwitcherService
    },
    {
        provide: APP_INITIALIZER,
        useFactory: initialize,
        deps: [InitializerService],
        multi: true
    },
    httpInterceptorProviders
  ],
  bootstrap: [AppComponent]
})
export class AppModule {}

export function initialize(initializer: InitializerService) {
  return () => initializer.initialize();
}
