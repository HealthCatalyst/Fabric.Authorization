import { NgModule, ModuleWithProviders, Type } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { MatTreeModule } from '@angular/material/tree';
import { CdkTreeModule } from '@angular/cdk/tree';

import {
  ButtonModule,
  IconModule,
  PopoverModule,
  InputModule,
  LabelModule,
  CheckboxModule,
  SelectModule,
  ProgressIndicatorsModule,
  PaginationModule,
  ModalModule,
  ListModule,
  TileModule
} from '@healthcatalyst/cashmere';

import { AccessControlRoutingModule } from './access-control-routing.module';
import { FabricBaseService } from '../../services/fabric-base.service';
import { FabricAuthGroupService } from '../../services/fabric-auth-group.service';
import { FabricAuthMemberSearchService } from '../../services/fabric-auth-member-search.service';
import { FabricAuthUserService } from '../../services/fabric-auth-user.service';
import { FabricExternalIdpSearchService } from '../../services/fabric-external-idp-search.service';
import { FabricAuthRoleService } from '../../services/fabric-auth-role.service';
import { IAccessControlConfigService } from '../../services/access-control-config.service';
import { FabricAuthGrainService } from '../../services/fabric-auth-grain.service';
import { FabricAuthEdwadminService } from '../../services/fabric-auth-edwadmin.service';

import { MemberListComponent } from './member-list/member-list.component';
import { MemberComponent } from './member/member.component';
import { CustomGroupComponent } from './custom-group/custom-group.component';
import { GrainListComponent } from './grain-list/grain-list.component';

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
    LabelModule,
    PaginationModule,
    ModalModule,
    ListModule,
    TileModule,
    MatTreeModule,
    CdkTreeModule
  ],
  declarations: [
    GrainListComponent,
    MemberListComponent,
    MemberComponent,
    CustomGroupComponent
  ],
  providers: [
    FabricBaseService,
    FabricAuthGroupService,
    FabricAuthMemberSearchService,
    FabricAuthUserService,
    FabricExternalIdpSearchService,
    FabricAuthRoleService,
    FabricAuthGrainService,
    FabricAuthEdwadminService
  ],
})
export class AccessControlModule { }

