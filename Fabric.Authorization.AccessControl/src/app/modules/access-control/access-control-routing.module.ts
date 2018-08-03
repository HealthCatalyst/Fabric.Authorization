import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { MemberComponent } from './member/member.component';
import { CustomGroupComponent } from './custom-group/custom-group.component';
import { MemberListComponent } from './member-list/member-list.component';
import { GrainListComponent } from './grain-list/grain-list.component';

const routes: Routes = [
  {
    path: '',
    component: GrainListComponent
  },
  {
    path: 'member',
    component: MemberComponent
  },
  {
    path: 'member/:subjectid/:type',
    component: MemberComponent
  },
  {
    path: 'customgroup',
    component: CustomGroupComponent
  },
  {
    path: 'customgroup/:subjectid',
    component: CustomGroupComponent
  },
  {
    path: 'grain',
    component: GrainListComponent
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AccessControlRoutingModule {}
