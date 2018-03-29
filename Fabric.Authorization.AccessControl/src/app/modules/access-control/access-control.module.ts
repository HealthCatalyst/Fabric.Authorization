import { NgModule, ModuleWithProviders } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
<<<<<<< HEAD
import {
  ButtonModule,
  IconModule,
  PopoverModule,
  InputModule,
  SelectModule,
  ProgressIndicatorsModule
} from '@healthcatalyst/cashmere';
=======
import { ButtonModule, IconModule, PopoverModule } from '@healthcatalyst/cashmere';
>>>>>>> updates to interceptors to get packaging to work

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

// import { httpInterceptorProviders } from '../../services/interceptors';

import { MemberListComponent } from './member-list/member-list.component';
import { MemberAddComponent } from './member-add/member-add.component';
import { CustomGroupAddComponent } from './custom-group-add/custom-group-add.component';
import { MemberEditComponent } from './member-edit/member-edit.component';
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
    SelectModule,
    ProgressIndicatorsModule
  ],
  declarations: [
    MemberListComponent,
    MemberAddComponent,
    CustomGroupAddComponent,
    MemberEditComponent,
    CustomGroupEditComponent
  ],
  providers: [
  //  httpInterceptorProviders,
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
