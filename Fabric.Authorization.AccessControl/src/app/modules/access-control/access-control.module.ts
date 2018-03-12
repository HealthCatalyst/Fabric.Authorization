import { NgModule, ModuleWithProviders } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { FormsModule, NgModel }   from '@angular/forms';

import { AccessControlRoutingModule } from './access-control-routing.module';
import { FabricBaseService, 
         FabricAuthGroupService, 
         FabricAuthMemberSearchService, 
         FabricAuthUserService, 
         FabricExternalIdpSearchService,
         FabricAuthRoleService } from '../../services';

import { MemberListComponent } from './member-list/member-list.component';
import { MemberAddComponent } from './member-add/member-add.component';


@NgModule({
  imports: [
    CommonModule,
    AccessControlRoutingModule,
    FormsModule
  ],
  declarations: [MemberListComponent, MemberAddComponent],  
  providers: [
    FabricBaseService,
    FabricAuthGroupService,
    FabricAuthMemberSearchService,
    FabricAuthUserService,
    FabricExternalIdpSearchService,
    FabricAuthRoleService
  ],
  exports:[
  ]
})
export class AccessControlModule { }
