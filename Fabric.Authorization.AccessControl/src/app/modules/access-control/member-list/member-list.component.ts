import { Component, OnInit, ViewChild, TemplateRef } from '@angular/core';
import { Subscription } from 'rxjs/Subscription';
import { Subject } from 'rxjs/Subject';
import 'rxjs/add/operator/debounceTime';
import 'rxjs/add/operator/distinctUntilChanged';
import { Router } from '@angular/router';
import { ModalService } from '@healthcatalyst/cashmere';

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
  readonly pageSizes: number[] = [5, 10, 25, 50];
  readonly keyUp = new Subject<Event>();
  readonly maxPageSize = 50;

  private _pageNumber = 1;

  pageSize = 10;
  members: IAuthMemberSearchResult[];
  totalMembers = null;
  filter = '';
  sortKey: SortKey = 'name';
  sortDirection: SortDirection = 'asc';
  searchesInProgress = 0;

  @ViewChild('confirmDialog')
  private confirmDialog: TemplateRef<any>;

  constructor(
    private memberSearchService: FabricAuthMemberSearchService,
    private configService: AccessControlConfigService,
    private userService: FabricAuthUserService,
    private groupService: FabricAuthGroupService,
    private router: Router,
    private modalService: ModalService
  ) {
    this.keyUp
      .debounceTime(500)
      .map(value => this.filter)
      .distinctUntilChanged()
      .subscribe(() => {
        this.onSearchChanged();
      });
  }

  ngOnInit() {
    this.getMembers();
  }

  get pageNumber() {
    return this._pageNumber;
  }
  set pageNumber(value: number) {
    if (this._pageNumber === value) {
      return;
    }

    this._pageNumber = value;
    this.onSearchChanged(true);
  }

  get availablePageSizes() {
    return this.pageSizes.filter(
      s => s < this.totalMembers && s <= this.maxPageSize
    );
  }

  get totalPages() {
    return typeof this.totalMembers === 'number'
      ? Math.ceil(this.totalMembers / this.pageSize)
      : null;
  }

  onSearchChanged(preservePageNumber = false) {
    if (!preservePageNumber) {
      this.pageNumber = 1;
    }

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
      securableItem: this.configService.securableItem
    };
    searchRequest.grain = this.configService.grain;
    searchRequest.securableItem = this.configService.securableItem;

    this.searchesInProgress++;
    return this.memberSearchService
      .searchMembers(searchRequest)
      .subscribe(response => {
        this.totalMembers = response.totalCount;
        this.members = response.results;
        this.searchesInProgress--;
      });
  }

  removeRolesFromMember(member: IAuthMemberSearchResult) {
    this.modalService.open(this.confirmDialog, {
      data: member,
      size: 'md'
    })
      .result
      .filter(r => !!r)
      .subscribe(r => this.doRemoveRoles(member));
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
        '/accesscontrol/member',
        member.subjectId,
        member.entityType
      ]);
    } else {
      this.router.navigate([
        '/accesscontrol/customgroup',
        member.subjectId
      ]);
    }
  }

  private doRemoveRoles(member: IAuthMemberSearchResult) {
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
}


