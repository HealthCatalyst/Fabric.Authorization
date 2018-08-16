import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
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
  selectedNode: GrainFlatNode;
  selectedGrain: string;
  selectedSecurableItem: string;

  constructor(
      private route: ActivatedRoute,
      private grainService: FabricAuthGrainService
  ) {
    this.isGrainVisible = grainService.isGrainVisible();
    this.treeFlattener = new MatTreeFlattener(this.transformer, this._getLevel, this._isExpandable, this._getChildren);
    this.treeControl = new FlatTreeControl<GrainFlatNode>(this._getLevel, this._isExpandable);
    this.dataSource = new MatTreeFlatDataSource(this.treeControl, this.treeFlattener);
    this.selectedGrain = this.route.snapshot.params['grain'];
    this.selectedSecurableItem = this.route.snapshot.paramMap.get('securableItem');

    this.grainService.getAllGrains().subscribe(
      response => {
        this.dataSource.data = this.AddGrainToGrainNode(response);
        this.initializeSelectedNode();
      }
    );;
  }

  transformer = (node: GrainNode, level: number) => new GrainFlatNode(!!node.children, node.name, node.parentName, level);

  private _getLevel = (node: GrainFlatNode) => node.level;

  private _isExpandable = (node: GrainFlatNode) => node.expandable;

  private _getChildren = (node: GrainNode): Observable<GrainNode[]> => observableOf(node.children);

  hasChild = (_: number, _nodeData: GrainFlatNode) => _nodeData.expandable;

  ngOnInit() {

  }

  initializeSelectedNode() {
    if(!!this.selectedGrain)
    {
      let firstNode = this.treeControl.dataNodes.find(node => { return node.name == this.selectedGrain } );
      this.treeControl.expand(firstNode);
      this.selectedNode = this.treeControl.dataNodes.find( node => { return node.name == this.selectedSecurableItem && node.parentName == this.selectedGrain } );
    }
    else {
      let firstNode = this.treeControl.dataNodes[0];
      this.treeControl.expand(firstNode);
      this.selectedNode = this.treeControl.dataNodes[1];
    }
  }

  AddGrainToGrainNode(data: IGrain[]): GrainNode[] {
    var result: GrainNode[] = [];

    for(let item of data) {
      var node = new GrainNode();
      node.name = item.name;
      node.children = this.AddSecurableItemToGrainNode(item.securableItems, item.name);
      result.push(node);
    }

    return result;
  }

  AddSecurableItemToGrainNode(data: ISecurableItem[], parentName: string): GrainNode[] {
    var result: GrainNode[] = [];

    for(let item of data) {
      var node = new GrainNode();
      node.name = item.name;
      node.parentName = parentName;

      node.children = this.AddSecurableItemToGrainNode(item.securableItems, item.name);
      result.push(node);
    }

    return result;
  }

  onSelect(node: GrainFlatNode): void {
    this.selectedNode = node;
  }

  getIcon(node: GrainFlatNode) {
    var value = "";
    if(node.expandable && !node.parentName) {
      value = "fa-plus-square-o";
    }
    else {
      value = "fa-minus-square-o";
    }

    if(node == this.selectedNode) {
      value = "fa-caret-square-o-right"
    }

    return value;
  }

  isSelectedNode(myNode: GrainFlatNode) {
    return this.selectedNode === myNode ? 'selected' : '';
  }
}


export class GrainNode {
  children: GrainNode[];
  name: string;
  parentName: string;
}

export class GrainFlatNode {
  constructor(
    public expandable: boolean, public name: string, public parentName: string, public level: number) {}
}
