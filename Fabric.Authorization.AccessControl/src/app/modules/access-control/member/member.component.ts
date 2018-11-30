
import {zip as observableZip, throwError as observableThrowError,  Subject ,  Subscription ,  Observable, of } from 'rxjs';

import { tap, takeUntil, mergeMap, catchError, debounceTime, filter, distinctUntilChanged} from 'rxjs/operators';
import { Component, OnInit, OnDestroy, Inject } from '@angular/core';

import { Router, ActivatedRoute } from '@angular/router';

import { IFabricPrincipal } from '../../../models/fabricPrincipal.model';
import { IRole } from '../../../models/role.model';
import { FabricExternalIdpSearchService } from '../../../services/fabric-external-idp-search.service';
import { FabricAuthRoleService } from '../../../services/fabric-auth-role.service';
import { FabricAuthUserService } from '../../../services/fabric-auth-user.service';
import { IAccessControlConfigService } from '../../../services/access-control-config.service';
import { FabricAuthGroupService } from '../../../services/fabric-auth-group.service';
import { FabricAuthEdwAdminService } from '../../../services/fabric-auth-edwadmin.service';
import { IUser } from '../../../models/user.model';
import { IGroup } from '../../../models/group.model';
import { CurrentUserService } from '../../../services/current-user.service';
import { AlertService } from '../../../services/global/alert.service';

@Component({
  selector: 'app-member',
  templateUrl: './member.component.html',
  styleUrls: ['./member.component.scss', '../access-control.scss']
})
export class MemberComponent implements OnInit, OnDestroy {
  public principals: Array<IFabricPrincipal> = [];
  public roles: Array<IRole> = [];
  public searching = false;
  public searchTextSubject = new Subject<string>();
  public searchText: string;
  public selectedPrincipal?: IFabricPrincipal;
  public missingManageAuthorizationPermission = true;
  public savingInProgress = false;
  public disabledSaveReason = '';
  public returnRoute = '/access-control';
  public editMode = true;
  public identityProvider = '';
  public tenantId = '';

  private grain: string;
  private securableItem: string;

  private ngUnsubscribe: any = new Subject();

  constructor(
    private idpSearchService: FabricExternalIdpSearchService,
    private roleService: FabricAuthRoleService,
    private userService: FabricAuthUserService,
    private edwAdminService: FabricAuthEdwAdminService,
    @Inject('IAccessControlConfigService')private configService: IAccessControlConfigService,
    private groupService: FabricAuthGroupService,
    private router: Router,
    private route: ActivatedRoute,
    private currentUserService: CurrentUserService,
    private alertService: AlertService
  ) {
  }

  ngOnInit() {

    const subjectId: string = this.route.snapshot.paramMap.get('subjectid');

    this.route.queryParams
      .subscribe(params => {
        this.identityProvider = params.identityProvider;
        this.tenantId = params.tenantId;
      });

    this.editMode = !!subjectId;
    const principalType: string = (this.route.snapshot.paramMap.get('type') || '').toLowerCase();
    this.savingInProgress = false;

    this.grain = this.route.snapshot.paramMap.get('grain');
    this.securableItem = this.route.snapshot.paramMap.get('securableItem');
    this.returnRoute = `${this.returnRoute}/${this.grain}/${this.securableItem}`;
    this.currentUserService.getPermissions().subscribe(p => {
      const requiredPermission = `${this.grain}/${this.securableItem}.manageauthorization`;
      if (!p.includes(requiredPermission)) {
        this.missingManageAuthorizationPermission = true;
        this.disabledSaveReason = `You are missing the following required permissions to edit: ${requiredPermission}`;
      } else {
        this.missingManageAuthorizationPermission = false;
        this.disabledSaveReason = '';
      }
    });

    this.searchText = subjectId;
    if (subjectId && principalType) {
        this.selectedPrincipal = {
            subjectId,
            principalType,
            identityProvider: this.identityProvider,
            tenantId: this.tenantId
        };
    }

    // Roles
    this.roleService
      .getRolesBySecurableItemAndGrain(
        this.grain,
        this.securableItem
      ).pipe(
      takeUntil(this.ngUnsubscribe),
      mergeMap((roles: IRole[]) => {
        this.roles = roles.map(role => <IRole>role);
        return this.bindExistingRoles(this.selectedPrincipal);
      }))
      .subscribe();

    // Search text
    this.searchTextSubject.pipe(
      takeUntil(this.ngUnsubscribe),
      filter((term) => !this.editMode),
      distinctUntilChanged(),
      debounceTime(500),
      tap((term) => {
        if (this.selectedPrincipal && term !== this.selectedPrincipal.subjectId) {
          this.selectedPrincipal = null;
        }

        this.roles.map(r => r.selected = false);
        this.principals.map(p => p.selected = false);
        if (term && term.length > 2) {
          this.searching = true;
        } else {
          this.searching = false;
          this.principals = [];
        }
      })).subscribe();

    // User / Group IDP search
    this.idpSearchService
      .search(this.searchTextSubject, null).pipe(
      takeUntil(this.ngUnsubscribe),
      filter((term) => !this.editMode))
      .subscribe(result => {
        this.searching = false;
        this.principals =
          result.principals.length === 0
              ? [
                    {
                        subjectId: this.searchText,
                        principalType: 'group',
                        identityProvider: this.identityProvider,
                        tenantId: this.tenantId
                    },
                    {
                        subjectId: this.searchText,
                        principalType: 'user',
                        identityProvider: this.identityProvider,
                        tenantId: this.tenantId
                    }
                ]
              : result.principals;
      });
  }

  ngOnDestroy(): void {
    this.ngUnsubscribe.next();
    this.ngUnsubscribe.complete();
  }

  selectPrincipal(principal: IFabricPrincipal): Subscription {
    this.principals = [];
    this.selectedPrincipal = principal;
    this.searchText = principal.subjectId;
    return this.bindExistingRoles(principal).subscribe();
  }

  save(): Subscription {
    this.savingInProgress = true;
    const selectedRoles: IRole[] = this.roles.filter(r => r.selected);

    const saveObservable: Observable<any> =
        this.selectedPrincipal.principalType === 'user'
            ? this.saveUser(this.selectedPrincipal.subjectId, selectedRoles)
            : this.saveGroup(this.selectedPrincipal, selectedRoles);

    return saveObservable.subscribe(null, null, () => {
        this.router.navigate([this.returnRoute]);
    });
  }

  cancel() {
    this.router.navigate([this.returnRoute]);
  }

  private saveUser(subjectId: string, selectedRoles: IRole[]): Observable<any> {
    const user: IUser = { identityProvider: this.configService.identityProvider, subjectId: subjectId };
    return this.userService
      .getUser(
        this.configService.identityProvider,
        subjectId
      ).pipe(
      mergeMap((userResult: IUser) => {
        return of(userResult);
      }),
      catchError(err => {
        if (err.statusCode === 404) {
          this.savingInProgress = false;
          return this
            .userService
            .createUser(user);
        }

        return observableThrowError(err.message);
      }),
      mergeMap((newUser: IUser) => {
        return this.userService.getUserRoles(user.identityProvider, user.subjectId);
      }),
      mergeMap((userRoles: IRole[]) => {
        const filteredUserRoles = userRoles.filter(role => role.grain === this.grain && role.securableItem === this.securableItem);
        const rolesToAdd = selectedRoles.filter(userRole => !filteredUserRoles.some(selectedRole => userRole.id === selectedRole.id));
        const rolesToDelete = filteredUserRoles.filter(userRole => !selectedRoles.some(selectedRole => userRole.id === selectedRole.id));
        return observableZip(
          this.userService
            .addRolesToUser(
              user.identityProvider,
              user.subjectId,
              rolesToAdd
            ),
          this.userService
            .removeRolesFromUser(
              user.identityProvider,
              user.subjectId,
              rolesToDelete
            )).toPromise()
            .then(value => {
              return this.edwAdminService.syncUsersWithEdwAdmin([user])
              .toPromise()
              .then(o => { this.savingInProgress = false; return value; })
              .catch(err => {
                this.savingInProgress = false;
                this.alertService.showSyncWarning(err.message);
              });
            });
      }),
      catchError(err => {
        return observableThrowError(err.message);
      }));
  }

  private saveGroup(principal: IFabricPrincipal, selectedRoles: IRole[]): Observable<any> {
    const group: IGroup = {
      groupName: principal.subjectId,
      groupSource: 'directory',
      identityProvider: principal.identityProvider,
      tenantId: principal.tenantId
    };

    return this.groupService
      .getGroup(group.groupName, principal.identityProvider, principal.tenantId).pipe(
      mergeMap((groupResult: IGroup) => {
        return of(groupResult);
      }),
      catchError(err => {
        if (err.statusCode === 404) {
          this.savingInProgress = false;
          return this
            .groupService
            .createGroup(group);
        }

        return observableThrowError(err.message);
      }),
      mergeMap((newGroup: IGroup) => {
        return this.groupService.getGroupRoles(
          group.groupName,
          this.grain,
          this.securableItem,
          group.identityProvider,
          group.tenantId);
      }),
      mergeMap((groupRoles: IRole[]) => {
        const rolesToAdd = selectedRoles.filter(userRole => !groupRoles.some(selectedRole => userRole.id === selectedRole.id));
        const rolesToDelete = groupRoles.filter(userRole => !selectedRoles.some(selectedRole => userRole.id === selectedRole.id));
        return observableZip(
          this.groupService.addRolesToGroup(group.groupName, rolesToAdd, group.identityProvider, group.tenantId),
          this.groupService.removeRolesFromGroup(group.groupName, rolesToDelete, group.identityProvider, group.tenantId))
          .toPromise()
          .then(value => {
            return this.edwAdminService.syncGroupWithEdwAdmin(group.groupName, group.identityProvider, group.tenantId)
            .toPromise()
            .then(o => { this.savingInProgress = false; return value; })
            .catch(err => {
              this.savingInProgress = false;
              this.alertService.showSyncWarning(err.message);
            });
          });
      }),
      catchError(err => {
        this.savingInProgress = false;
        return observableThrowError(err.message);
      }));
  }

  private bindExistingRoles(principal: IFabricPrincipal): Observable<any> {
    if (!principal || !principal.subjectId) {
        return of(undefined);
    }
    const roleObservable: Observable<any> =
        principal.principalType === 'user'
            ? this.userService.getUserRoles(this.configService.identityProvider, principal.subjectId)
            : this.groupService.getGroupRoles(
              principal.subjectId,
              this.grain,
              this.securableItem,
              principal.identityProvider,
              principal.tenantId);

    return roleObservable.pipe(tap((existingRoles: IRole[]) => {
        if (!existingRoles) {
            this.roles.map(role => (role.selected = false));
        } else {
            this.roles.map(role => (role.selected = existingRoles.some(userRole => userRole.id === role.id)));
        }
    }));
  }
}
