import { Component, OnInit } from '@angular/core';
import { Subscription } from 'rxjs/Rx';
import { Router} from '@angular/router';


import { AccessControlConfigService,
        FabricAuthMemberSearchService, 
        FabricAuthUserService,
        FabricAuthGroupService } from '../../../services';
import { AuthMemberSearchRequest, 
        AuthMemberSearchResult, 
        Role, 
        FabricPrincipal } from '../../../models'

@Component({
  selector: 'app-member-list',
  templateUrl: './member-list.component.html',
  styleUrls: ['./member-list.component.css']
})
export class MemberListComponent implements OnInit {

  members: AuthMemberSearchResult[];  

  constructor(private memberSearchService: FabricAuthMemberSearchService, 
    private configService: AccessControlConfigService,
    private userService: FabricAuthUserService,
    private groupService: FabricAuthGroupService,
    private router: Router) { }

  ngOnInit() {
    this.getMembers();
  }

  getMembers() {
    var self = this;
    var searchRequest = new AuthMemberSearchRequest();
    searchRequest.grain = this.configService.grain;
    searchRequest.securableItem = this.configService.securableItem;
    
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

  goToMemberEdit(member: AuthMemberSearchResult){    
    if(member.entityType !== 'CustomGroup'){
      this.router.navigate(['/accesscontrol/memberedit', member.subjectId, member.entityType ]);
    } else{
      this.router.navigate(['/accesscontrol/customgroupedit', member.subjectId ]);
    }    
  }
}
