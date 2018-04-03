import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { MemberListComponent } from './member-list/member-list.component';
import { MemberComponent } from './member/member.component';
import { CustomGroupAddComponent } from './custom-group-add/custom-group-add.component';
import { CustomGroupEditComponent } from './custom-group-edit/custom-group-edit.component';

const routes: Routes = [
  {
    path: '',
    component: MemberListComponent
  },
  {
    path: 'member/:subjectid/:type',
    component: MemberComponent
  },
  {
    path: 'member',
    component: MemberComponent
  },
  {
    path: 'customgroupadd',
    component: CustomGroupAddComponent
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
