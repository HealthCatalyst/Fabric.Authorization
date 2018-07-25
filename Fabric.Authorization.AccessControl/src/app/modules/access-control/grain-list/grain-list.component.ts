import { Component, OnInit, Inject } from '@angular/core';

import { FabricAuthGrainService } from '../../../services/fabric-auth-grain.service';
import { IGrain } from '../../../models/grain.model';
import { ISecurableItem } from '../../../models/securableItem.model';

import { IAccessControlConfigService } from '../../../services/access-control-config.service';

@Component({
  selector: 'app-grain-list',
  templateUrl: './grain-list.component.html',
  styleUrls: ['./grain-list.component.scss', '../access-control.scss']
})
export class GrainListComponent implements OnInit {

  grains: IGrain[];
  isGrainVisible: boolean;

  constructor(
      private grainService: FabricAuthGrainService
  ) {
    this.isGrainVisible = grainService.isGrainVisible();
  }

  ngOnInit() {
    this.getGrains();
  }

  getGrains() {
    return this.grainService.getAllGrains().subscribe(response => {
      this.grains = response;
    });
  }

  getMemberListForGrain(grain: IGrain){

  }

  getMemberListForSecurableItem(securableItem: ISecurableItem){

  }
}
