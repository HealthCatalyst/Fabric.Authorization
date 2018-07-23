import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

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
