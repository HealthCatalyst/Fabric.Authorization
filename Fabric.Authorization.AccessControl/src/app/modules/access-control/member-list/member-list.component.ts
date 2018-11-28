
import {debounceTime, map, distinctUntilChanged, filter} from 'rxjs/operators';
import { Component, OnInit, ViewChild, TemplateRef, Inject, Input, OnChanges } from '@angular/core';
import { Subject } from 'rxjs';


import { Router } from '@angular/router';
import { ModalService } from '@healthcatalyst/cashmere';
import { IAuthMemberSearchResult } from '../../../models/authMemberSearchResult.model';
import { SortKey, SortDirection, IAuthMemberSearchRequest } from '../../../models/authMemberSearchRequest.model';
import { FabricAuthMemberSearchService } from '../../../services/fabric-auth-member-search.service';
import { IAccessControlConfigService } from '../../../services/access-control-config.service';
import { FabricAuthUserService } from '../../../services/fabric-auth-user.service';
import { FabricAuthGroupService } from '../../../services/fabric-auth-group.service';
import { FabricAuthEdwAdminService } from '../../../services/fabric-auth-edwadmin.service';
import { IRole } from '../../../models/role.model';
import { GrainFlatNode } from '../grain-list/grain-list.component';
import { CurrentUserService } from '../../../services/current-user.service';

@Component({
  selector: 'app-member-list',
  templateUrl: './member-list.component.html',
  styleUrls: ['./member-list.component.scss', '../access-control.scss']
})
export class MemberListComponent implements OnInit, OnChanges {
  readonly pageSizes: number[] = [5, 10, 25, 50];
  readonly keyUp = new Subject<Event>();
  readonly maxPageSize = 50;
  private _pageNumber = 1;

  hideDeleteButton = true;
  pageSize = 10;
  members: IAuthMemberSearchResult[];
  totalMembers = 10;
  filter = '';
  sortKey: SortKey = 'name';
  sortDirection: SortDirection = 'asc';
  searchesInProgress = 1;
  grain: string = this.configService.grain;
  securableItem: string = this.configService.securableItem;
  @Input() selectedNode: GrainFlatNode;

  @ViewChild('confirmDialog')
  private confirmDialog: TemplateRef<any>;

  constructor(
    private memberSearchService: FabricAuthMemberSearchService,
    private edwAdminService: FabricAuthEdwAdminService,
    @Inject('IAccessControlConfigService') private configService: IAccessControlConfigService,
    private userService: FabricAuthUserService,
    private groupService: FabricAuthGroupService,
    private router: Router,
    private modalService: ModalService,
    private currentUserService: CurrentUserService
  ) {
    this.keyUp.pipe(
      debounceTime(500),
      map(value => this.filter),
      distinctUntilChanged())
      .subscribe(() => {
        this.onSearchChanged();
      });
  }

  ngOnInit() {
    sessionStorage.removeItem('selectedMember');
  }

  initialize() {
    console.log('initializing');
    this.currentUserService.getPermissions().subscribe(p => {
      const requiredPermission = `${this.grain}/${this.securableItem}.manageauthorization`;
      if (p.includes(requiredPermission)) {
        this.hideDeleteButton = false;
      } else {
        this.hideDeleteButton = true;
      }
    });
  }

  ngOnChanges() {
    if (this.selectedNode && this.selectedNode.parentName && this.selectedNode.name) {
      console.log('Changed securableItem to Grain: ' + this.selectedNode.parentName + ', SecurableItem: ' + this.selectedNode.name);

      this.getMembers();
      this.initialize();
    }
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

    if (this.selectedNode) {
      this.grain = this.selectedNode.parentName;
      this.securableItem = this.selectedNode.name;
    }

    const searchRequest: IAuthMemberSearchRequest = {
      pageNumber: this.pageNumber,
      pageSize: this.pageSize,
      filter: this.filter,
      sortKey: this.sortKey,
      sortDirection: this.sortDirection,
      grain: this.grain,
      securableItem: this.securableItem
    };

    searchRequest.grain = this.grain;
    searchRequest.securableItem = this.securableItem;

    this.searchesInProgress = 1;
    return this.memberSearchService
      .searchMembers(searchRequest)
      .subscribe(response => {
        this.totalMembers = response.totalCount;
        this.members = response.results;
        this.searchesInProgress = 0;
      });
  }

  removeRolesFromMember(member: IAuthMemberSearchResult) {
    this.modalService.open(this.confirmDialog, {
      data: {member: member, grain: this.grain, securableItem: this.securableItem},
      size: 'md'
    })
      .result.pipe(
      filter(r => !!r))
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
      return role.displayName || role.name;
    });
  }

  goToMemberEdit(member: IAuthMemberSearchResult) {
    sessionStorage.setItem('selectedMember', JSON.stringify(member));

    const queryParams = {};
    if (member.identityProvider) {
      queryParams['identityProvider'] = member.identityProvider;
    }

    if (member.tenantId) {
      queryParams['tenantId'] = member.tenantId;
    }

    if (member.entityType !== 'CustomGroup') {
      this.router.navigate([
        '/access-control/member',
        this.grain,
        this.securableItem,
        member.subjectId,
        member.entityType
      ],
      {
        queryParams: queryParams
      });
    } else {
      this.router.navigate([
        '/access-control/customgroup',
        this.grain,
        this.securableItem,
        member.subjectId
      ],
      {
        queryParams: queryParams // TODO: remove this when Auth is fixed so that custom groups do not have IdPs
      });
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
        .then(value => {
          return this.edwAdminService.syncUsersWithEdwAdmin([value])
            .toPromise().then(o => value).catch(err => value);
        })
        .then(() => {
          return this.getMembers();
        });
    } else {
      this.groupService
        .removeRolesFromGroup(member.groupName, member.roles, member.identityProvider, member.tenantId)
        .toPromise()
        .then(value => {
          return this.edwAdminService.syncGroupWithEdwAdmin(member.groupName, member.identityProvider, member.tenantId)
            .toPromise().then(o => value).catch(err => value);
        })
        .then(() => {
          return this.getMembers();
        });
    }
  }

  getMemberNameToDisplay(member: IAuthMemberSearchResult): string {
    if (member.displayName !== '' && member.displayName !== null && member.displayName !== undefined) {
      return member.displayName;
    }

    return member.groupName;
  }
}


