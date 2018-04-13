import { Component, OnInit, OnDestroy, Inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { FabricAuthRoleService } from '../../../services/fabric-auth-role.service';
import { FabricAuthUserService } from '../../../services/fabric-auth-user.service';
import { IAccessControlConfigService } from '../../../services/access-control-config.service';
import { FabricAuthGroupService } from '../../../services/fabric-auth-group.service';
import { FabricExternalIdpSearchService } from '../../../services/fabric-external-idp-search.service';
import { IRole } from '../../../models/role.model';
import { IUser } from '../../../models/user.model';
import { IFabricPrincipal } from '../../../models/fabricPrincipal.model';
import { IGroup } from '../../../models/group.model';
import { Subscription } from 'rxjs/Subscription';
import { Subject } from 'rxjs/Subject';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/observable/zip';
import 'rxjs/add/operator/mergeMap';
import 'rxjs/add/operator/map';
import 'rxjs/add/operator/do';
import 'rxjs/add/operator/takeUntil';
import 'rxjs/add/observable/empty';

@Component({
  selector: 'app-custom-group',
  templateUrl: './custom-group.component.html',
  styleUrls: ['./custom-group.component.scss', '../access-control.scss']
})
export class CustomGroupComponent implements OnInit, OnDestroy {
  public roles: Array<IRole> = [];
  public principals: Array<IFabricPrincipal> = [];
  public associatedPrincipals: Array<IUser> = [];
  public editMode = true;

  public groupName = '';
  public groupNameSubject: Subject<string>;
  public groupNameinvalid = false;
  public groupNameError: string;

  public searchTerm = '';
  public searchTermSubject = new Subject<string>();
  public searching = false;
  public initializing = true;

  private ngUnsubscribe: any = new Subject();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private userService: FabricAuthUserService,
    @Inject('IAccessControlConfigService') private configService: IAccessControlConfigService,
    private roleService: FabricAuthRoleService,
    private groupService: FabricAuthGroupService,
    private idpSearchService: FabricExternalIdpSearchService
  ) { }

  ngOnInit() {
    this.groupName = this.route.snapshot.paramMap.get('subjectid');
    this.editMode = !!this.groupName;

    if (this.editMode) {
      Observable.zip(this.getGroupRoles(), this.getGroupUsers())
        .do((result: [IRole[], IUser[]]) => {
          this.roles = result[0];
          this.associatedPrincipals = result[1];
        })
        .takeUntil(this.ngUnsubscribe)
        .subscribe(null, null, () => {
          this.initializing = false;
        });
    } else {
      this.setupGroupNameErrorCheck();

      this.roleService
        .getRolesBySecurableItemAndGrain(
          this.configService.grain,
          this.configService.securableItem
        )
        .do((roles: IRole[]) => {
          this.roles = roles;
        })
        .takeUntil(this.ngUnsubscribe)
        .subscribe(null, null, () => {
          this.initializing = false;
        });
    }

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
        if (this.associatedPrincipals && this.associatedPrincipals.length > 0) {
          this.principals = result.principals
            .filter(principal => this.associatedPrincipals
              .some(associatedPrincipal => associatedPrincipal.subjectId !== principal.subjectId));
        } else {
          this.principals = result.principals;
        }
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
        this.router.navigate(['/access-control']);
      });
  }

  private setupGroupNameErrorCheck(): any {
    if (this.groupNameSubject) {
      this.groupNameSubject.unsubscribe();
    }

    this.groupNameSubject = new Subject();

    this.groupNameSubject
      .debounceTime(250)
      .distinctUntilChanged()
      .do((term) => {
        if (!term) {
          this.groupNameError = 'Group name is required';
          this.groupNameinvalid = true;
        }
      })
      .filter(term => term && term.length > 0)
      .mergeMap((term) => {
        return this.groupService.getGroup(term);
      })
      .catch(err => {
        if (err.statusCode === 404) {
          return Observable.of(undefined);
        }
        return Observable.throw(err.message);
      })
      .takeUntil(this.ngUnsubscribe)
      .subscribe((group) => {
        if (!!group) {
          this.groupNameinvalid = true;
          this.groupNameError = `Group ${this.groupName} already exists`;
        } else {
          this.groupNameinvalid = false;
          this.groupNameError = '';
          this.setupGroupNameErrorCheck();
        }
      });
  }
}
