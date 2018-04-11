import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { IRole, IUser, IFabricPrincipal, IdPSearchResult, IGroup } from '../../../models';
import {
  FabricAuthUserService,
  AccessControlConfigService,
  FabricAuthRoleService,
  FabricAuthGroupService,
  FabricExternalIdpSearchService
} from '../../../services';
import { Subject } from 'rxjs/Subject';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/observable/zip';
import 'rxjs/add/operator/mergeMap';
import 'rxjs/add/operator/map';
import 'rxjs/add/operator/do';

@Component({
  selector: 'app-custom-group',
  templateUrl: './custom-group.component.html',
  styleUrls: ['./custom-group.component.scss']
})
export class CustomGroupComponent implements OnInit, OnDestroy {
  public groupName = '';
  public roles: Array<IRole> = [];
  public principals: Array<IFabricPrincipal> = [];
  public associatedPrincipals: Array<IUser> = [];
  public editMode = true;

  public searchTerm = '';
  public searchTermSubject = new Subject<string>();
  public searching = false;
  public initializing = true;

  private ngUnsubscribe: any = new Subject();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private userService: FabricAuthUserService,
    private configService: AccessControlConfigService,
    private roleService: FabricAuthRoleService,
    private groupService: FabricAuthGroupService,
    private idpSearchService: FabricExternalIdpSearchService
  ) { }

  ngOnInit() {
    this.groupName = this.route.snapshot.paramMap.get('subjectid');

    Observable.zip(this.getGroupRoles(), this.getGroupUsers())

      .do((result: [IRole[], IUser[]]) => {
        this.roles = result[0];
        this.associatedPrincipals = result[1];
      })
      .takeUntil(this.ngUnsubscribe)
      .subscribe(null, null, () => {
        this.initializing = false;
      });

    this.searchTermSubject
      .takeUntil(this.ngUnsubscribe)
      .do((term) => {
        this.principals.map(p => p.selected = false);
        if (term && term.length > 2) {
          this.searching = true;
        } else {
          this.searching = false;
          this.principals = [];
        }
      })
      .takeUntil(this.ngUnsubscribe)
      .subscribe();

    this.idpSearchService
      .search(this.searchTermSubject, 'user')
      .takeUntil(this.ngUnsubscribe)
      .subscribe(result => {
        this.searching = false;
        this.principals = result.principals
          .filter(principal => this.associatedPrincipals
            .some(associatedPrincipal => associatedPrincipal.subjectId !== principal.subjectId));
      });
  }

  ngOnDestroy(): void {
    this.ngUnsubscribe.next();
    this.ngUnsubscribe.complete();
  }

  getGroupUsers(): Observable<IUser[]> {
    return this.groupService
      .getGroupUsers(this.groupName);
  }

  getGroupRoles(): Observable<IRole[]> {
    const rolesObservable = this.roleService
      .getRolesBySecurableItemAndGrain(
        this.configService.grain,
        this.configService.securableItem
      );

    const groupRolesObservable = this.groupService
      .getGroupRoles(
        this.groupName,
        this.configService.grain,
        this.configService.securableItem
      );

    return Observable.zip(rolesObservable, groupRolesObservable)
      .map((result: [IRole[], IRole[]]) => {
        let allRoles = result[0];
        const groupRoles = result[1];

        allRoles = allRoles.map(role => {
          role.selected = groupRoles.some(groupRole => groupRole.name === role.name);
          return role;
        });

        return allRoles;
      });
  }

  associateUsers() {
    const newUsers: IUser[] = this.principals
      .filter(principal => principal.selected === true)
      .map((principal) => {
        const newUser: IUser = {
          subjectId: principal.subjectId,
          identityProvider: this.configService.identityProvider,
          selected: false
        };
        return newUser;
      });

    this.associatedPrincipals = this.associatedPrincipals.concat(newUsers);
    this.principals = this.principals.filter(principal => !principal.selected);
  }

  unAssociateUsers() {
    const removedUsers: IFabricPrincipal[] = this.associatedPrincipals
      .filter(principal => principal.selected === true)
      .map((principal) => {
        const newUser: IFabricPrincipal = {
          subjectId: principal.subjectId,
          firstName: '',
          middleName: '',
          lastName: '',
          principalType: 'user',
          selected: false
        };
        return newUser;
      });

    this.principals = this.principals.concat(removedUsers);
    this.associatedPrincipals = this.associatedPrincipals.filter(principal => !principal.selected);
  }

  save() {
    const newGroup: IGroup = { groupName: this.groupName, groupSource: 'custom' };
    const groupObservable = this.editMode ? Observable.of(newGroup) : this.groupService.createGroup(newGroup);

    return groupObservable
      .mergeMap((group) => {
        const groupRolesObservable = this.groupService
          .getGroupRoles(this.groupName, this.configService.grain, this.configService.securableItem);
        const groupUsersObservable = this.getGroupUsers();

        return Observable.zip(groupRolesObservable, groupUsersObservable)
          .mergeMap((result: [IRole[], IUser[]]) => {
            const existingRoles = result[0];
            const existingUsers = result[1];
            const selectedRoles = this.roles.filter(role => role.selected === true);

            const usersToAdd = this.associatedPrincipals
              .filter(user => !existingUsers.some(existingUser => user.subjectId === existingUser.subjectId));
            const usersToRemove = existingUsers
              .filter(existingUser => !this.associatedPrincipals.some(user => user.subjectId === existingUser.subjectId));

            const rolesToAdd = selectedRoles.filter(userRole => !existingRoles.some(selectedRole => userRole.id === selectedRole.id));
            const rolesToRemove = existingRoles.filter(userRole => !selectedRoles.some(selectedRole => userRole.id === selectedRole.id));

            // adding/removing roles and group members
            const saveObservables = [];
            saveObservables.push(this.groupService.removeRolesFromGroup(group.groupName, rolesToRemove));
            saveObservables.push(this.groupService.addRolesToGroup(group.groupName, rolesToAdd));
            saveObservables.push(this.groupService.addUsersToCustomGroup(group.groupName, usersToAdd));
            for (const userToRemove of usersToRemove) {
              saveObservables.push(this.groupService.removeUserFromCustomGroup(group.groupName, userToRemove));
            }
            return Observable.zip(...saveObservables);
          });
      })
      .takeUntil(this.ngUnsubscribe)
      .subscribe(null, (error) => {
        // TODO: Error handling
        console.error(error);
      }, () => {
        this.router.navigate(['/accesscontrol']);
      });
  }
}
