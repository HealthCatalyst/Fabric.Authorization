import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';


import { MemberListComponent } from './member-list/member-list.component';
import { MemberAddComponent } from './member-add/member-add.component';

const routes: Routes = [
  {
    path: '',
    component: MemberListComponent
  },
  {
    path: 'memberadd',
    component: MemberAddComponent
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AccessControlRoutingModule { }
