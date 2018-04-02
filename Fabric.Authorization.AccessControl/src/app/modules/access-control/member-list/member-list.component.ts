import { Component, OnInit } from '@angular/core';
import { Subscription } from 'rxjs/Subscription';
import { Subject } from 'rxjs/Subject';
import 'rxjs/add/operator/debounceTime';
import 'rxjs/add/operator/distinctUntilChanged';
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
  IFabricPrincipal,
  SortDirection,
  SortKey
} from '../../../models';

@Component({
  selector: 'app-member-list',
  templateUrl: './member-list.component.html',
  styleUrls: ['./member-list.component.scss']
})
export class MemberListComponent implements OnInit {
  members: IAuthMemberSearchResult[];
  pageNumber = 0;
  pageSize = 20;
  filter = '';
  sortKey: SortKey = 'name';
  sortDirection: SortDirection = 'asc';
  searchesInProgress = 0;
  readonly keyUp = new Subject<Event>();

  constructor(
    private memberSearchService: FabricAuthMemberSearchService,
    private configService: AccessControlConfigService,
    private userService: FabricAuthUserService,
    private groupService: FabricAuthGroupService,
    private router: Router
  ) {
    this.keyUp
      .debounceTime(500)
      .map(value => this.filter)
      .distinctUntilChanged()
      .subscribe(() => this.onSearchChanged());
  }

  ngOnInit() {
    this.getMembers();
  }

  onSearchChanged() {
    this.getMembers();
  }

  getMembers() {
    const searchRequest: IAuthMemberSearchRequest = {
      pageNumber: this.pageNumber,
      pageSize: this.pageSize,
      filter: this.filter,
      sortKey: this.sortKey,
      sortDirection: this.sortDirection,

      grain: this.configService.grain,
      securableItem: this.configService.securableItem,
    };
    searchRequest.grain = this.configService.grain;
    searchRequest.securableItem = this.configService.securableItem;

    this.searchesInProgress++;
    return this.memberSearchService
      .searchMembers(searchRequest)
      .subscribe((memberList) => {
        this.members = memberList;
        this.searchesInProgress--;
      });
  }

  removeRolesFromMember(member: IAuthMemberSearchResult) {
    console.dir(member);
    if (member.entityType === 'User') {
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

  changeSort(sortColumn: SortKey) {
    if (sortColumn === this.sortKey) {
      if (this.sortDirection === 'asc') {
        this.sortDirection = 'desc';
      } else {
        this.sortDirection = 'asc';
      }
    } else {
      this.sortKey = sortColumn;
      this.sortDirection = 'asc';
    }
    this.getMembers();
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
