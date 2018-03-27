import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { MemberListComponent } from './member-list/member-list.component';
import { MemberAddComponent } from './member-add/member-add.component';
import { CustomGroupAddComponent } from './custom-group-add/custom-group-add.component';
import { MemberEditComponent } from './member-edit/member-edit.component';
import { CustomGroupEditComponent } from './custom-group-edit/custom-group-edit.component';

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
  },
  {
    path: 'memberedit/:subjectid/:type',
    component: MemberEditComponent
  },
  {
    path: 'customgroupedit/:subjectid',
    component: CustomGroupEditComponent
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AccessControlRoutingModule {}
