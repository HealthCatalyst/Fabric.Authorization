import { NgModule, ModuleWithProviders } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';

import { AccessControlRoutingModule } from './access-control-routing.module';
import { FabricBaseService, FabricAuthGroupService, FabricAuthMemberSearchService, FabricAuthUserService, FabricExternalIdpSearchService } from '../../services';


import { MemberListComponent } from './member-list/member-list.component';
import { MemberAddComponent } from './member-add/member-add.component';


@NgModule({
  imports: [
    CommonModule,
    AccessControlRoutingModule
  ],
  declarations: [MemberListComponent, MemberAddComponent],  
  providers: [
    FabricBaseService,
    FabricAuthGroupService,
    FabricAuthMemberSearchService,
    FabricAuthUserService,
    FabricExternalIdpSearchService   
  ],
  exports:[
  ]
})
export class AccessControlModule { }
