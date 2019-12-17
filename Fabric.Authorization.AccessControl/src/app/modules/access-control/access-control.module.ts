import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTreeModule } from '@angular/material/tree';
import { CdkTreeModule } from '@angular/cdk/tree';

import {
  ButtonModule,
  IconModule,
  PopModule,
  InputModule,
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
import { FabricAuthGrainService } from '../../services/fabric-auth-grain.service';
import { FabricAuthEdwAdminService } from '../../services/fabric-auth-edwadmin.service';

import { MemberListComponent } from './member-list/member-list.component';
import { MemberComponent } from './member/member.component';
import { CustomGroupComponent } from './custom-group/custom-group.component';
import { GrainListComponent } from './grain-list/grain-list.component';
import { CurrentUserService } from '../../services/current-user.service';
import { InputDirective } from './input.directive';

@NgModule({
  imports: [
    CommonModule,
    AccessControlRoutingModule,
    FormsModule,
    // Cashmere modules
    ButtonModule,
    IconModule,
    PopModule,
    InputModule,
    CheckboxModule,
    SelectModule,
    ProgressIndicatorsModule,
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
    CustomGroupComponent,
    InputDirective
  ],
  providers: [
    FabricBaseService,
    FabricAuthGroupService,
    FabricAuthMemberSearchService,
    FabricAuthUserService,
    FabricExternalIdpSearchService,
    FabricAuthRoleService,
    FabricAuthGrainService,
    FabricAuthEdwAdminService,
    CurrentUserService
  ],
})
export class AccessControlModule { }

