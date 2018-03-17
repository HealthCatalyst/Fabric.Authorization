import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { HttpClientModule } from '@angular/common/http';

import { AppComponent } from './app.component';
import { AppRoutingModule } from './app-routing.module';

import { httpInterceptorProviders } from './services/interceptors';
import { AuthService } from '../app/services/global/auth.service';
import { AccessControlConfigService } from './services/access-control-config.service';
import { ClientAccessControlConfigService } from './services/global/client-access-control-config.service';

import { LoginComponent } from './login/login.component';
import { HomeComponent } from './home/home.component';
import { LogoutComponent } from './logout/logout.component';

import { ButtonModule } from '@healthcatalyst/cashmere';

@NgModule({
  declarations: [
    AppComponent,
    LoginComponent,
    HomeComponent,
    LogoutComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    HttpClientModule,
    ButtonModule
  ],
  providers: [
    AuthService,
    httpInterceptorProviders,
    { provide: AccessControlConfigService, useClass: ClientAccessControlConfigService }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
