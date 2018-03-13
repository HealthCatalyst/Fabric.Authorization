import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';


import { MemberListComponent } from './member-list/member-list.component';
import { MemberAddComponent } from './member-add/member-add.component';
import { CustomGroupAddComponent } from './custom-group-add/custom-group-add.component';

const routes: Routes = [
  {
    path: '',
    component: MemberListComponent
  },
  {
    path: 'memberadd',
    component: MemberAddComponent
  },
  {
    path: 'customgroupadd',
    component: CustomGroupAddComponent
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AccessControlRoutingModule { }
