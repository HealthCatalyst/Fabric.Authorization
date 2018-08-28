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
  isGrainVisible = false;
  selectedNode: GrainFlatNode;

  constructor(
    private grainService: FabricAuthGrainService
  ) {
    this.isGrainVisible = grainService.isGrainVisible();
    this.treeFlattener = new MatTreeFlattener(this.transformer, this._getLevel, this._isExpandable, this._getChildren);
    this.treeControl = new FlatTreeControl<GrainFlatNode>(this._getLevel, this._isExpandable);
    this.dataSource = new MatTreeFlatDataSource(this.treeControl, this.treeFlattener);

    this.grainService.getAllGrains().subscribe(
      response => {
        const sortedGrains = response;
        if (sortedGrains.length > 1) {
          const dosIndex = response.findIndex(g => g.name === 'dos');
          if (dosIndex > 0) {
            sortedGrains.unshift(sortedGrains[dosIndex]);
            sortedGrains.splice(dosIndex + 1, 1);
          }
        }
        this.dataSource.data = this.addGrainToGrainNode(sortedGrains);
        this.initializeSelectedNode();
      });
  }

  transformer = (node: GrainNode, level: number) => new GrainFlatNode(!!node.children, node.name, node.parentName, level);

  private _getLevel = (node: GrainFlatNode) => node.level;

  private _isExpandable = (node: GrainFlatNode) => node.expandable;

  private _getChildren = (node: GrainNode): Observable<GrainNode[]> => observableOf(node.children);

  hasChild = (_: number, _nodeData: GrainFlatNode) => _nodeData.expandable;

  ngOnInit() {

  }

  initializeSelectedNode(): void {
    if (!!this.getSelectedGrain()) {
      const firstNode = this.treeControl.dataNodes.find(node => {
        return node.name === this.getSelectedGrain();
      });

      this.treeControl.expand(firstNode);
      this.selectedNode = this.treeControl.dataNodes.find(node => {
        return node.name === this.getSelectedSecurableItem() && node.parentName === this.getSelectedGrain();
      });
    } else {
      const firstNode = this.treeControl.dataNodes[0];
      const secondNode = this.treeControl.dataNodes[1];
      this.treeControl.expand(firstNode);

      // if the second node a child, select that.
      // if it is not related, just use the first node.
      if (firstNode.name === secondNode.parentName) {
        this.selectedNode = secondNode;
      } else {
        this.selectedNode = firstNode;
      }
    }
  }

  getSelectedGrain() {
    return sessionStorage.getItem('selectedGrain');
  }

  getSelectedSecurableItem() {
    return sessionStorage.getItem('selectedSecurableItem');
  }

  addGrainToGrainNode(data: IGrain[]): GrainNode[] {
    const result: GrainNode[] = [];

    for (const item of data) {
      const node = new GrainNode();
      node.name = item.name;
      node.children = this.addSecurableItemToGrainNode(item.securableItems, item.name);
      result.push(node);
    }

    return result;
  }

  addSecurableItemToGrainNode(data: ISecurableItem[], parentName: string): GrainNode[] {
    const result: GrainNode[] = [];

    if (data === null) {
      return null;
    }

    for (const item of data) {
      const node = new GrainNode();
      node.name = item.name;
      node.parentName = parentName;

      node.children = this.addSecurableItemToGrainNode(item.securableItems, item.name);
      result.push(node);
    }

    return result;
  }

  onSelect(node: GrainFlatNode): void {
    this.selectedNode = node;

    if (!!node.parentName) {
      sessionStorage.setItem('selectedSecurableItem', node.name);
    } else {
      sessionStorage.removeItem('selectedSecurableItem');
      sessionStorage.setItem('selectedGrain', node.name);
    }
  }

  getIcon(node: GrainFlatNode): string {
    let value = '';
    if (node.expandable && !node.parentName) {
      value = 'fa-plus-square-o';
    } else {
      value = 'fa-minus-square-o';
    }

    if (node === this.selectedNode) {
      value = 'fa-caret-square-o-right';
    }

    return value;
  }

  isSelectedNode(myNode: GrainFlatNode): string {
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
    public expandable: boolean, public name: string, public parentName: string, public level: number) { }
}
