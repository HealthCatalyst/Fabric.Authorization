import { Component, OnInit } from '@angular/core';
import { MatTreeFlatDataSource, MatTreeFlattener } from '@angular/material/tree';
import { FlatTreeControl } from '@angular/cdk/tree';
import { Observable } from 'rxjs';
import { of as observableOf } from 'rxjs/observable/of';

import { FabricAuthGrainService } from '../../../services/fabric-auth-grain.service';
import { IGrain } from '../../../models/grain.model';
import { ISecurableItem } from '../../../models/securableItem.model';

@Component({
  selector: 'app-grain-list',
  templateUrl: './grain-list.component.html',
  styleUrls: ['./grain-list.component.scss', '../access-control.scss']
})
export class GrainListComponent implements OnInit {

  treeControl: FlatTreeControl<GrainFlatNode>;
  treeFlattener: MatTreeFlattener<GrainNode, GrainFlatNode>;
  dataSource: MatTreeFlatDataSource<GrainNode, GrainFlatNode>;

  grains: IGrain[];
  isGrainVisible: boolean = false;

  constructor(
      private grainService: FabricAuthGrainService
  ) {
    this.isGrainVisible = grainService.isGrainVisible();
    var database = this.getGrains();

    this.treeFlattener = new MatTreeFlattener(this.transformer, this._getLevel, this._isExpandable, this._getChildren);
    this.treeControl = new FlatTreeControl<GrainFlatNode>(this._getLevel, this._isExpandable);
    this.dataSource = new MatTreeFlatDataSource(this.treeControl, this.treeFlattener);
  }

  transformer = (node: GrainNode, level: number) => {
    return new GrainFlatNode(!!node.children, node.grainName, level);
  }

  private _getLevel = (node: GrainFlatNode) => node.level;

  private _isExpandable = (node: GrainFlatNode) => node.expandable;

  private _getChildren = (node: GrainNode): Observable<GrainNode[]> => observableOf(node.children);

  hasChild = (_: number, _nodeData: GrainFlatNode) => _nodeData.expandable;

  ngOnInit() {
  }

  getGrains() {
    return this.grainService.getAllGrains().subscribe(response => {
      this.dataSource.data = this.AddGrainToGrainNode(response);
    });
  }

  AddGrainToGrainNode(data: IGrain[]): GrainNode[] {
    var result: GrainNode[] = [];

    for(let item of data) {
      var node = new GrainNode();
      node.grainName = item.name;
      node.children = this.AddSecurableItemToGrainNode(item.securableItems);
      result.push(node);
    }

    return result;
  }

  AddSecurableItemToGrainNode(data: ISecurableItem[]): GrainNode[] {
    var result: GrainNode[] = [];

    for(let item of data) {
      var node = new GrainNode();
      node.grainName = item.name;
      node.children = this.AddSecurableItemToGrainNode(item.securableItems);
      result.push(node);
    }

    return result;
  }
}

export class GrainNode {
  children: GrainNode[];
  grainName: string;
}

export class GrainFlatNode {
  constructor(
    public expandable: boolean, public name: string, public level: number) {}
}
