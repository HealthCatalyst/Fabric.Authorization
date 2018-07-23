import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { HomeComponent } from './home/home.component';
import { LoginComponent } from './login/login.component';
import { LogoutComponent } from './logout/logout.component';
import { AuthenticationGuard } from './services/guards/authentication.guard';
import { AuthService } from './services/global/auth.service';

const routes: Routes = [
  {
    path: 'access-control',
    canActivate: [AuthenticationGuard],
    loadChildren:
      'app/modules/access-control/access-control.module#AccessControlModule'
  },
  {
    path: '',
    redirectTo: 'access-control',
    pathMatch: 'full'
  },
  {
    path: 'home',
    component: HomeComponent
  },
  {
    path: 'login',
    component: LoginComponent
  },
  {
    path: 'logout',
    component: LogoutComponent
  }
];

@NgModule({
  imports: [
    RouterModule.forRoot(routes)
  ],
  providers: [
    AuthenticationGuard,
    AuthService
  ],
  exports: [RouterModule]
})
export class AppRoutingModule {}
