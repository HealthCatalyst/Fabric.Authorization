import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';


import { UserlistComponent } from './userlist/userlist.component';
import { UseraddComponent } from './useradd/useradd.component';

const routes: Routes = [
  {
    path: '',
    component: UserlistComponent
  },
  {
    path: 'adduser',
    component: UseraddComponent
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AccessControlRoutingModule { }
