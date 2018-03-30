import { Component, OnInit } from '@angular/core';
import { Subscription } from 'rxjs/Subscription';
import { Router } from '@angular/router';

import {
  AccessControlConfigService,
  FabricAuthMemberSearchService,
  FabricAuthUserService,
  FabricAuthGroupService
} from '../../../services';
import {
  IAuthMemberSearchRequest,
  IAuthMemberSearchResult,
  IRole,
  IFabricPrincipal
} from '../../../models';

@Component({
  selector: 'app-member-list',
  templateUrl: './member-list.component.html',
  styleUrls: ['./member-list.component.css']
})
export class MemberListComponent implements OnInit {
  members: IAuthMemberSearchResult[];

  constructor(
    private memberSearchService: FabricAuthMemberSearchService,
    private configService: AccessControlConfigService,
    private userService: FabricAuthUserService,
    private groupService: FabricAuthGroupService,
    private router: Router
  ) {}

  ngOnInit() {
    this.getMembers();
  }

  getMembers() {
    const searchRequest: IAuthMemberSearchRequest = {grain: this.configService.grain, securableItem: this.configService.securableItem};

    return this.memberSearchService
      .searchMembers(searchRequest)
      .subscribe((memberList) => {
        this.members = memberList;
      });
  }

  removeRolesFromMember(member: IAuthMemberSearchResult) {
    if (member.entityType === 'user') {
      this.userService
        .removeRolesFromUser(
          member.identityProvider,
          member.subjectId,
          member.roles
        )
        .toPromise()
        .then(() => {
          return this.getMembers();
        });
    } else {
      this.groupService
        .removeRolesFromGroup(member.groupName, member.roles)
        .toPromise()
        .then(() => {
          return this.getMembers();
        });
    }
  }

  selectRoleNames(roles: Array<IRole>) {
    return roles.map(function(role) {
      return role.name;
    });
  }

  goToMemberEdit(member: IAuthMemberSearchResult) {
    if (member.entityType !== 'CustomGroup') {
      this.router.navigate([
        '/accesscontrol/memberedit',
        member.subjectId,
        member.entityType
      ]);
    } else {
      this.router.navigate([
        '/accesscontrol/customgroupedit',
        member.subjectId
      ]);
    }
  }
}
