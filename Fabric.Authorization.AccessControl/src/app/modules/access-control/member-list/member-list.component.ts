import { Component, OnInit } from '@angular/core';
import { Subscription } from 'rxjs/Rx';

import { AccessControlConfigService } from '../../../services';

import { FabricAuthMemberSearchService, 
        FabricAuthUserService,
        FabricAuthGroupService } from '../../../services';
import { AuthMemberSearchRequest, AuthMemberSearchResult, Role } from '../../../models'

@Component({
  selector: 'app-member-list',
  templateUrl: './member-list.component.html',
  styleUrls: ['./member-list.component.css']
})
export class MemberListComponent implements OnInit {

  members: AuthMemberSearchResult[];
  filterText: string;

  constructor(private memberSearchService: FabricAuthMemberSearchService, 
    private configService: AccessControlConfigService,
    private userService: FabricAuthUserService,
    private groupService: FabricAuthGroupService) { }

  ngOnInit() {
    this.getMembers();
  }

  getMembers() {
    var self = this;
    var searchRequest = new AuthMemberSearchRequest();
    searchRequest.clientId = this.configService.clientId;
    searchRequest.filter = this.filterText;
    
    return this.memberSearchService.searchMembers(searchRequest)
    .subscribe(function(memberList){
      self.members = memberList;
    });
  }

  removeRolesFromMember(member: AuthMemberSearchResult) {
    if(member.entityType === 'user'){
      this.userService.removeRolesFromUser(member.identityProvider, member.subjectId, member.roles)
      .toPromise()
      .then(() =>{
        return this.getMembers();
      });      
    } else{
      this.groupService.removeRolesFromGroup(member.groupName, member.roles)
      .toPromise()
      .then(() =>{
        return this.getMembers();
      });
    }
  }

  selectRoleNames(roles: Array<Role>){
    return roles.map(function(role){
      return role.name;
    });
  }

}
