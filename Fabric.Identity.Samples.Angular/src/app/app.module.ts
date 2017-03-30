import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpModule } from '@angular/http';
import { RouterModule } from '@angular/router';

import { AppComponent } from './app.component';
import { HomeComponent } from './home/home.component';
import { NavmenuComponent } from './navmenu/navmenu.component';
import { ViewpatientComponent } from './viewpatient/viewpatient.component';
import { UnauthorizedComponent } from './unauthorized/unauthorized.component';

import { AuthGuardService } from './shared/services/auth-guard.service';
import { AuthService } from './shared/services/auth.service';
import { UserManager, Log, MetadataService, User } from 'oidc-client';
import { OidccallbackComponent } from './oidccallback/oidccallback.component';
import { LoginComponent } from './login/login.component';
import { LogoutComponent } from './logout/logout.component';

@NgModule({
  declarations: [
    AppComponent,
    HomeComponent,
    NavmenuComponent,
    ViewpatientComponent,
    UnauthorizedComponent,
    OidccallbackComponent,
    LoginComponent,
    LogoutComponent
  ],
  imports: [
      BrowserModule,
      FormsModule,
      HttpModule,
      RouterModule.forRoot([
          { path: '', redirectTo: 'home', pathMatch: 'full' },
          { path: 'home', component: HomeComponent },
          { path: 'oidc-callback', component: OidccallbackComponent},
          { path: 'viewpatient', component: ViewpatientComponent, canActivate: [AuthGuardService] },
          { path: 'unauthorized', component: UnauthorizedComponent },
          { path: 'login', component: LoginComponent },
          { path: 'logout', component: LogoutComponent }
      ])
  ],
  providers: [
      AuthGuardService,
      AuthService
  ],
  bootstrap: [AppComponent]
})
export class AppModule {
  
}
