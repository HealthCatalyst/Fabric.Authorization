import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { MemberComponent } from './member/member.component';
import { CustomGroupComponent } from './custom-group/custom-group.component';
import { GrainListComponent } from './grain-list/grain-list.component';

const routes: Routes = [
  {
    path: '',
    component: GrainListComponent
  },
  {
    path: 'member/:grain/:securableItem',
    component: MemberComponent
  },
  {
    path: 'member/:grain/:securableItem/:subjectid/:type',
    component: MemberComponent
  },
  {
    path: 'customgroup/:grain/:securableItem',
    component: CustomGroupComponent
  },
  {
    path: 'customgroup/:grain/:securableItem/:subjectid',
    component: CustomGroupComponent
  },
  {
    path: 'grain/:grain/:securableItem',
    component: GrainListComponent
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AccessControlRoutingModule {}
