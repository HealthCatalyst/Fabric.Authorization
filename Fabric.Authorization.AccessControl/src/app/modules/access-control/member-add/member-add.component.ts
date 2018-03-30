import { Component, OnInit, OnDestroy } from '@angular/core';

import { Subject } from 'rxjs/Subject';
import { Subscription } from 'rxjs/Subscription';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/operator/do';
import 'rxjs/add/operator/takeUntil';
import 'rxjs/add/operator/catch';
import 'rxjs/add/observable/throw';

import {
  FabricExternalIdpSearchService,
  FabricAuthRoleService,
  AccessControlConfigService,
  FabricAuthUserService,
  FabricAuthGroupService
} from '../../../services';
import { IFabricPrincipal, IRole, IUser, IGroup } from '../../../models';
import { inherits } from 'util';
import { Router } from '@angular/router';

interface IRoleModel extends IRole {
  selected: boolean;
}

interface IFabricPrincipalModel extends IFabricPrincipal {
  selected: boolean;
}

@Component({
  selector: 'app-member-add',
  templateUrl: './member-add.component.html',
  styleUrls: ['./member-add.component.scss']
})
export class MemberAddComponent implements OnInit, OnDestroy {
  public principals: Array<IFabricPrincipalModel>;
  public roles: Array<IRoleModel>;
  public searching = false;

  public searchText = new Subject<string>();
  private ngUnsubscribe: any = new Subject();
  private principalSelected = false;

  constructor(
    private idpSearchService: FabricExternalIdpSearchService,
    private roleService: FabricAuthRoleService,
    private userService: FabricAuthUserService,
    private configService: AccessControlConfigService,
    private groupService: FabricAuthGroupService,
    private router: Router
  ) {
  }

  ngOnInit() {
    // Roles
    this.roleService
      .getRolesBySecurableItemAndGrain(
        this.configService.grain,
        this.configService.securableItem
      )
      .takeUntil(this.ngUnsubscribe)
      .subscribe(roleResults => {
        this.roles = roleResults.map(role => {
          // TODO: find user and associated roles on server
          const newRole = <IRoleModel>role;
          newRole.selected = false;
          return newRole;
        });
      });

    // Search text
    this.searchText
      .takeUntil(this.ngUnsubscribe)
      .do(() => {
        this.searching = true;
      }).subscribe();

    // User / Group
    this.idpSearchService
      .searchUser(this.searchText, null)
      .takeUntil(this.ngUnsubscribe)
      .subscribe(result => {
        this.searching = false;
        this.principals = result.principals.map(principal => <IFabricPrincipalModel>principal);
      });
  }

  ngOnDestroy(): void {
    this.ngUnsubscribe.next();
    this.ngUnsubscribe.complete();
  }

  selectPrincipal(principal: IFabricPrincipalModel) {
    for (const otherPrincal of this.principals.filter((p) => p.subjectId !== principal.subjectId)) {
      otherPrincal.selected = false;
    }
    this.principalSelected = !principal.selected;
  }

  addMemberWithRoles() {
    const selected: IFabricPrincipalModel = this.principals.find(p => p.selected);
    if (!selected) {
      return;
    }

    const selectedRoles = this.roles.filter(r => r.selected);

    if (selected.principalType === 'user') {
      const user: IUser = { identityProvider: this.configService.identityProvider, subjectId: selected.subjectId };
      return this.userService
        .getUser(
          this.configService.identityProvider,
          selected.subjectId
        )
        .mergeMap((userResult: IUser) => {
          return Observable.of(userResult);
        })
        .catch(err => {
          if (err.statusCode === 404) {
            return this
              .userService
              .createUser(user);
          } else {
            // TODO: handle error
          }
        })
        .mergeMap((newUser: IUser) => {
          return this.userService
            .addRolesToUser(
              newUser.identityProvider,
              newUser.subjectId,
              selectedRoles
            );
        })
        .subscribe(() => {
          this.router.navigate(['/accesscontrol']);
        });
    } else {
      const group: IGroup = { groupName: selected.subjectId, groupSource: '' };
      return this.groupService
        .getGroup(group.groupName)
        .mergeMap((groupResult: IGroup) => {
          return Observable.of(groupResult);
        })
        .catch(err => {
          if (err.statusCode === 404) {
            return this
              .groupService
              .createGroup(group);
          } else {
            // TODO: handle error
          }
        })
        .mergeMap((newGroup: IGroup) => {
          return this.groupService
            .addRolesToGroup(newGroup.groupName, selectedRoles);
        })
        .subscribe(() => {
          this.router.navigate(['/accesscontrol']);
        });
    }
  }
}
