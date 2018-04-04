import { NgModule, ModuleWithProviders } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import {
  ButtonModule,
  IconModule,
  PopoverModule,
  InputModule,
  LabelModule,
  CheckboxModule,
  SelectModule,
  ProgressIndicatorsModule
} from '@healthcatalyst/cashmere';

import { AccessControlRoutingModule } from './access-control-routing.module';
import {
  FabricBaseService,
  FabricAuthGroupService,
  FabricAuthMemberSearchService,
  FabricAuthUserService,
  FabricExternalIdpSearchService,
  FabricAuthRoleService,
  AccessControlConfigService
} from '../../services';

import { MemberListComponent } from './member-list/member-list.component';
import { MemberComponent } from './member/member.component';
import { CustomGroupAddComponent } from './custom-group-add/custom-group-add.component';
import { CustomGroupEditComponent } from './custom-group-edit/custom-group-edit.component';

@NgModule({
  imports: [
    CommonModule,
    AccessControlRoutingModule,
    FormsModule,
    // Cashmere modules
    ButtonModule,
    IconModule,
    PopoverModule,
    InputModule,
    CheckboxModule,
    SelectModule,
    ProgressIndicatorsModule,
    LabelModule
  ],
  declarations: [
    MemberListComponent,
    MemberComponent,
    CustomGroupAddComponent,
    CustomGroupEditComponent
  ],
  providers: [
    FabricBaseService,
    FabricAuthGroupService,
    FabricAuthMemberSearchService,
    FabricAuthUserService,
    FabricExternalIdpSearchService,
    FabricAuthRoleService
  ],
  exports: []
})
export class AccessControlModule {
  static forRoot(config: AccessControlConfigService): ModuleWithProviders {
    return {
      ngModule: AccessControlModule,
      providers: [
        {provide: AccessControlConfigService, useValue: config}
      ]
    };
  }
}
