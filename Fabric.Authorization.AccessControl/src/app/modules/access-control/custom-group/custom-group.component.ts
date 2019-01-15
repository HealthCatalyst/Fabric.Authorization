
import {zip as observableZip,  Subject ,  Observable, of } from 'rxjs';

import {map, mergeMap, takeUntil, tap, debounceTime, distinctUntilChanged, filter, switchMap} from 'rxjs/operators';
import { Component, OnInit, OnDestroy, Inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { FabricAuthRoleService } from '../../../services/fabric-auth-role.service';
import { FabricAuthGroupService } from '../../../services/fabric-auth-group.service';
import { FabricExternalIdpSearchService } from '../../../services/fabric-external-idp-search.service';
import { FabricAuthEdwAdminService } from '../../../services/fabric-auth-edwadmin.service';

import { IRole } from '../../../models/role.model';
import { IUser } from '../../../models/user.model';
import { IFabricPrincipal } from '../../../models/fabricPrincipal.model';
import { IGroup } from '../../../models/group.model';

import { CurrentUserService } from '../../../services/current-user.service';
import { AlertService } from '../../../services/global/alert.service';
import { IAccessControlConfigService } from '../../../services/access-control-config.service';

@Component({
  selector: 'app-custom-group',
  templateUrl: './custom-group.component.html',
  styleUrls: ['./custom-group.component.scss', '../access-control.scss']
})
export class CustomGroupComponent implements OnInit, OnDestroy {
  public roles: Array<IRole> = [];
  public principals: Array<IFabricPrincipal> = [];
  public customGroups: Array<IGroup> = [];
  public associatedUsers: Array<IUser> = [];
  public associatedGroups: Array<IGroup> = [];
  public editMode = true;
  public missingManageAuthorizationPermission = false;
  public disabledSaveReason = '';
  public returnRoute = '/access-control';

  public groupName = '';
  public identityProvider = '';
  public tenantId = '';
  public displayName = '';
  public groupNameSubject = new Subject<string>();
  public groupNameInvalid = false;
  public groupNameError: string;
  public associatedNameError: string;
  public searchingGroup = false;

  public searchTerm = '';
  public searchTermSubject = new Subject<string>();
  public searching = false;
  public initializing = true;
  public savingInProgress = false;

  private ngUnsubscribe: any = new Subject();

  private grain: string;
  private securableItem: string;

  private userType = 'user';
  private groupType = 'group';
  private groupRoles: Array<IRole> = [];
  private missingPermissions: Array<string> = [];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private roleService: FabricAuthRoleService,
    private groupService: FabricAuthGroupService,
    private edwAdminService: FabricAuthEdwAdminService,
    private idpSearchService: FabricExternalIdpSearchService,
    private currentUserService: CurrentUserService,
    private alertService: AlertService,
    @Inject('IAccessControlConfigService')
    private configService: IAccessControlConfigService,
  ) { }

  ngOnInit() {

    this.groupName = this.route.snapshot.paramMap.get('subjectid');
    this.route.queryParams
      .subscribe(params => {
        this.identityProvider = params.identityProvider;
        this.tenantId = params.tenantId;
      });

    this.grain = this.route.snapshot.paramMap.get('grain');
    this.securableItem = this.route.snapshot.paramMap.get('securableItem');
    this.returnRoute = `${this.returnRoute}/${this.grain}/${this.securableItem}`;
    this.editMode = !!this.groupName;
    this.savingInProgress = false;

    if (this.editMode) {
      const selectedGroupJson = sessionStorage.getItem('selectedMember');
      if (!!selectedGroupJson) {
        const selectedGroup: IGroup = JSON.parse(selectedGroupJson);
        this.displayName = selectedGroup.displayName;
      } else {
        this.groupService.getGroup(this.groupName, this.identityProvider, this.tenantId).subscribe(g => {
          this.displayName = g.displayName;
        });
      }

      observableZip(this.getGroupRolesBySecurableItemAndGrain(), this.getGroupUsers(), this.getChildGroups()).pipe(
        switchMap((result: [IRole[], IUser[], IGroup[]]) => {
          this.roles = result[0];
          this.associatedUsers = result[1];
          this.associatedGroups = result[2];

          this.associatedUsers.forEach(u => u.type = this.userType);
          this.associatedGroups.forEach(g => g.type = this.groupType);

          return this.currentUserService.getPermissions();
        }),
        tap(p => {
          this.groupRoles.forEach(r => {
            const requiredPermission = `${r.grain}/${r.securableItem}.manageauthorization`;
            if (!p.includes(requiredPermission)) {
              this.missingPermissions.push(requiredPermission);
            }
          });

          this.configureSaveButton();
        }),
        takeUntil(this.ngUnsubscribe))
        .subscribe(null, null, () => {
          this.initializing = false;
        });
    } else {
      this.setupGroupNameErrorCheck();
      this.roleService
        .getRolesBySecurableItemAndGrain(
          this.grain,
          this.securableItem
        ).pipe(
        switchMap((roles: IRole[]) => {
          this.roles = roles;

          return this.currentUserService.getPermissions();
        }),
        tap(p => {
          this.roles.forEach(r => {
            const requiredPermission = `${r.grain}/${r.securableItem}.manageauthorization`;

            if (!p.includes(requiredPermission)) {
              this.missingPermissions.push(requiredPermission);
            }
          });

          this.configureSaveButton();
        }),
        takeUntil(this.ngUnsubscribe))
        .subscribe(null, null, () => {
          this.initializing = false;
        });
    }

    this.searchTermSubject.pipe(
      takeUntil(this.ngUnsubscribe),
      tap((term) => {
        this.principals.map(p => p.selected = false);
        if (term && term.length > 2) {
          this.searching = true;
        } else {
          this.searching = false;
          this.principals = [];
        }
      }),
      takeUntil(this.ngUnsubscribe))
      .subscribe();

    this.groupNameSubject.pipe(
      takeUntil(this.ngUnsubscribe),
      filter((term) => !this.editMode),
      tap((term) => {
        this.roles.map(r => r.selected = false);
        this.principals.map(p => p.selected = false);
        if (term && term.length > 2) {
          this.searchingGroup = true;
        } else {
          this.searchingGroup = false;
          this.customGroups = [];
        }
      })).subscribe();

    // custom group search
    this.groupService
    .search(this.groupNameSubject).pipe(
    takeUntil(this.ngUnsubscribe))
    .subscribe(result => {
      this.searchingGroup = false;
      this.customGroups = result.map(group => <IGroup>group);
    });

    this.idpSearchService
      .search(this.searchTermSubject, null).pipe(
      takeUntil(this.ngUnsubscribe))
      .subscribe(result => {
        this.searching = false;
        const returnedPrincipals: IFabricPrincipal[] =
          result.principals.length === 0
              ? [
                    {
                        subjectId: this.searchTerm,
                        principalType: this.userType,
                        identityProvider: this.configService.identityProvider,
                        identityProviderUserPrincipalName: this.searchTerm
                    },
                    {
                        subjectId: this.searchTerm,
                        principalType: this.groupType,
                        identityProvider: this.configService.identityProvider
                    }
                ]
              : result.principals;

        let unAssociatedUserPrincipals = [];
        let unAssociatedGroupPrincipals = [];

        if (this.associatedUsers && this.associatedUsers.length > 0) {
          unAssociatedUserPrincipals = returnedPrincipals.filter(principal =>
            principal.principalType === this.userType
            && !this.associatedUsers.map(u => u.subjectId.toLowerCase()).includes(principal.subjectId.toLowerCase()));
        } else {
          unAssociatedUserPrincipals = returnedPrincipals.filter(principal =>
            principal.principalType === this.userType);
        }

        if (this.associatedGroups && this.associatedGroups.length > 0) {
          unAssociatedGroupPrincipals = returnedPrincipals.filter(principal =>
            principal.principalType === this.groupType
            && !this.associatedGroups.map(u => u.groupName.toLowerCase()).includes(principal.subjectId.toLowerCase()));
        } else {
          unAssociatedGroupPrincipals = returnedPrincipals.filter(principal =>
            principal.principalType === this.groupType);
        }

        if (unAssociatedUserPrincipals.length > 0 || unAssociatedGroupPrincipals.length > 0
            || (unAssociatedUserPrincipals.length === 0 && unAssociatedGroupPrincipals.length === 0)) {
          this.principals = unAssociatedUserPrincipals.concat(unAssociatedGroupPrincipals);
        } else {
          this.principals = returnedPrincipals;
        }
      });
  }

  configureSaveButton() {
    const currentManageAuthPermission = `${this.grain}/${this.securableItem}.manageauthorization`;
    if (this.roles.length === 0 && !this.missingPermissions.some(p => p === currentManageAuthPermission)) {
      this.missingPermissions.push(currentManageAuthPermission);
    }

    if (this.missingPermissions.length > 0) {
      this.missingManageAuthorizationPermission = true;
      this.disabledSaveReason = `You are missing the following required permissions to edit ` +
        `this group: ${this.missingPermissions.join(',')}.`;
    } else {
      this.missingManageAuthorizationPermission = false;
      this.disabledSaveReason = '';
    }
  }

  ngOnDestroy(): void {
    this.ngUnsubscribe.next();
    this.ngUnsubscribe.complete();
  }

  getGroupUsers(): Observable<IUser[]> {
    return this.groupService
      .getGroupUsers(this.groupName);
  }

  getChildGroups(): Observable<IGroup[]> {
    return this.groupService
      .getChildGroups(this.groupName, this.identityProvider, this.tenantId);
  }

  getRolesBySecurableItemAndGrain(): Observable<IRole[]> {
    return this.roleService
      .getRolesBySecurableItemAndGrain(
        this.grain,
        this.securableItem
      );
  }

  getGroupRoles(): Observable<IRole[]> {
     return this.groupService
      .getGroupRoles(
        this.groupName,
        this.grain,
        this.securableItem,
        this.identityProvider,
        this.tenantId
      );
  }

  getGroupRolesBySecurableItemAndGrain(): Observable<IRole[]> {
    return observableZip(this.getRolesBySecurableItemAndGrain(), this.getGroupRoles()).pipe(
      map((result: [IRole[], IRole[]]) => {
        let allRoles = result[0];
        this.groupRoles = result[1];

        allRoles = allRoles.map(role => {
          role.selected = this.groupRoles.some(groupRole => groupRole.name === role.name);
          return role;
        });

        return allRoles;
      }));
  }

  associateUsersAndGroups() {
    this.associateUsers();
    this.associateGroups();

    this.principals = this.principals.filter(principal => !principal.selected);
  }

  private associateUsers() {
    const newUsers: IUser[] = this.principals
      .filter(principal => principal.selected === true && principal.principalType === this.userType)
      .map((principal) => {
        const newUser: IUser = {
          subjectId: principal.subjectId,
          identityProvider: principal.identityProvider,
          selected: false,
          type: this.userType,
          identityProviderUserPrincipalName: principal.identityProviderUserPrincipalName
        };
        return newUser;
      });

    this.associatedUsers = this.associatedUsers.concat(newUsers);
  }

  private associateGroups() {
    const newGroups: IGroup[] = this.principals
      .filter(principal => principal.selected === true && principal.principalType === this.groupType)
      .map((principal) => {
        console.log('map principal ' + principal.subjectId);
        const newGroup: IGroup = {
          groupName: principal.subjectId,
          groupSource: 'directory',
          selected: false,
          type: this.groupType,
          identityProvider: principal.identityProvider,
          tenantId: principal.tenantId,
          externalIdentifier: principal.externalIdentifier
        };
        return newGroup;
      });

    this.associatedGroups = this.associatedGroups.concat(newGroups);
  }

  unAssociateUsersAndGroups() {
    this.unAssociateUsers();
    this.unAssociateGroups();
  }

  private unAssociateUsers() {
    const removedPrincipals: IFabricPrincipal[] = this.associatedUsers
      .filter(user => user.selected === true)
      .map((user) => {
        const newPrincipal: IFabricPrincipal = {
          subjectId: user.subjectId,
          firstName: '',
          middleName: '',
          lastName: '',
          principalType: this.userType,
          selected: false,
          identityProviderUserPrincipalName: user.identityProviderUserPrincipalName
        };
        return newPrincipal;
      });

    this.principals = this.principals.concat(removedPrincipals);
    this.associatedUsers = this.associatedUsers.filter(user => !user.selected);
  }

  private unAssociateGroups() {
    const removedPrincipals: IFabricPrincipal[] = this.associatedGroups
      .filter(group => group.selected === true)
      .map((group) => {
        const newPrincipal: IFabricPrincipal = {
          subjectId: group.groupName,
          firstName: '',
          middleName: '',
          lastName: '',
          principalType: this.groupType,
          selected: false,
          identityProvider: group.identityProvider,
          tenantId: group.tenantId,
          externalIdentifier: group.externalIdentifier
        };
        return newPrincipal;
      });

    this.principals = this.principals.concat(removedPrincipals);
    this.associatedGroups = this.associatedGroups.filter(group => !group.selected);
  }

  save() {
    this.savingInProgress = true;

    if (!this.checkGroupNameProvided(this.displayName)) {
      this.savingInProgress = false;
      return;
    }

    if (!this.editMode) {
      this.groupName = this.displayName;
    }

    const newGroup: IGroup = {
      groupName: this.groupName,
      displayName: this.displayName,
      groupSource: 'custom',
      identityProvider: this.identityProvider,
      tenantId: this.tenantId
    };

    const groupObservable = this.editMode ? of(newGroup) : this.groupService.createGroup(newGroup);

    return groupObservable.pipe(
      mergeMap((group) => {
        const groupRolesObservable = this.groupService
          .getGroupRoles(this.groupName, this.grain, this.securableItem, this.identityProvider, this.tenantId);
        const groupUsersObservable = this.getGroupUsers();
        const childGroupsObservable = this.getChildGroups();

        return observableZip(groupRolesObservable, groupUsersObservable, childGroupsObservable).pipe(
          mergeMap((result: [IRole[], IUser[], IGroup[]]) => {
            const existingRoles = result[0];
            const existingUsers = result[1];
            const existingChildGroups = result[2];
            const selectedRoles = this.roles.filter(role => role.selected === true);

            // get users to add/remove
            const usersToAdd = this.associatedUsers
              .filter(user => !existingUsers.some(existingUser => user.subjectId === existingUser.subjectId));
            const usersToRemove = existingUsers
              .filter(existingUser => !this.associatedUsers.some(user => user.subjectId === existingUser.subjectId));

            // get child groups to add/remove
            const childGroupsToAdd = this.associatedGroups
              .filter(childGroup => !existingChildGroups.some(existingChildGroup => childGroup.groupName === existingChildGroup.groupName));
            const childGroupsToRemove = existingChildGroups
              .filter(existingChildGroup =>
                !this.associatedGroups.some(childGroup => childGroup.groupName === existingChildGroup.groupName));

            // get roles to add/remove
            const rolesToAdd = selectedRoles.filter(userRole => !existingRoles.some(selectedRole => userRole.id === selectedRole.id));
            const rolesToRemove = existingRoles.filter(userRole => !selectedRoles.some(selectedRole => userRole.id === selectedRole.id));

            const saveObservables = [];

            // roles
            saveObservables.push(this.groupService.removeRolesFromGroup(group.groupName, rolesToRemove));
            saveObservables.push(this.groupService.addRolesToGroup(group.groupName, rolesToAdd));

            // users
            saveObservables.push(this.groupService.addUsersToCustomGroup(group.groupName, usersToAdd));
            for (const userToRemove of usersToRemove) {
              saveObservables.push(this.groupService.removeUserFromCustomGroup(group.groupName, userToRemove));
            }

            // child groups
            saveObservables.push(this.groupService.addChildGroups(group.groupName, childGroupsToAdd, this.identityProvider, this.tenantId));
            saveObservables.push(this.groupService.removeChildGroups(
              group.groupName,
              childGroupsToRemove,
              this.identityProvider,
              this.tenantId));

              return observableZip(...saveObservables)
                  .toPromise()
                  .then(value => {
                      return this.edwAdminService.syncGroupWithEdwAdmin(newGroup.groupName, newGroup.identityProvider, newGroup.tenantId)
                          .toPromise()
                          .then(o => value)
                          .catch(err => {
                            this.alertService.showSyncWarning(err.message);
                          });
                  })
                  .then(value => {
                      if (usersToRemove) {
                        return this.edwAdminService.syncUsersWithEdwAdmin(usersToRemove)
                        .toPromise()
                        .then(o => value)
                        .catch(err => {
                          this.alertService.showSyncWarning(err.message);
                        });
                      }
                  });
          }));
      }),
      takeUntil(this.ngUnsubscribe))
      .subscribe(null, (error) => {
        if (error.statusCode === 409) {
            // the custom group still gets saved, but without the duplicate named user or group, 
            // because this code is in the groupsObservable.
            this.groupNameInvalid = true;
            this.associatedNameError = error.message;
            this.groupNameError = error.message;
        }

        // TODO: Error handling
        this.savingInProgress = false;
      }, () => {
        this.savingInProgress = false;
        this.router.navigate([this.returnRoute]);
      });
  }

  cancel() {
    this.router.navigate([this.returnRoute]);
  }

  private setupGroupNameErrorCheck(): any {
    if (this.groupNameSubject) {
      this.groupNameSubject.unsubscribe();
    }

    this.groupNameSubject = new Subject();

    this.groupNameSubject.pipe(
      debounceTime(250),
      distinctUntilChanged(),
      tap((term) => {
        this.checkGroupNameProvided(term);
      })).subscribe();
  }

  private checkGroupNameProvided(name: string) {
    if (!name) {
      this.groupNameError = 'Group name is required';
      this.groupNameInvalid = true;
      return false;
    }
    return true;
  }

  public customGroupSelected(selectedGroup: IGroup) {
    this.groupName = selectedGroup.groupName;
    this.displayName = selectedGroup.displayName;
    this.editMode = true;
    this.groupNameInvalid = false;
    this.groupNameError = '';
    this.associatedNameError = '';
    this.customGroups = [];
    return observableZip(this.getGroupRolesBySecurableItemAndGrain(), this.getGroupUsers(), this.getChildGroups()).pipe(
        tap((result: [IRole[], IUser[], IGroup[]]) => {
          this.roles = result[0];
          this.associatedUsers = result[1];
          this.associatedGroups = result[2];

          this.associatedUsers.forEach(u => u.type = this.userType);
          this.associatedGroups.forEach(g => g.type = this.groupType);
        }),
        takeUntil(this.ngUnsubscribe))
        .subscribe();
  }

  getGroupNameToDisplay(customGroup: IGroup): string {
    if (customGroup.displayName !== '' && customGroup.displayName !== null && customGroup.displayName !== undefined) {
      return customGroup.displayName;
    }

    return customGroup.groupName;
  }

  getUserNameToDisplay(user: IUser): string {
    if (user.identityProviderUserPrincipalName !== ''
        && user.identityProviderUserPrincipalName !== null
        && user.identityProviderUserPrincipalName !== undefined) {
      return user.identityProviderUserPrincipalName;
    }

    return user.subjectId;
  }
}
