import { Component, OnInit, DoCheck } from '@angular/core';
import { MatTreeFlatDataSource, MatTreeFlattener } from '@angular/material/tree';
import { FlatTreeControl } from '@angular/cdk/tree';
import { Observable } from 'rxjs';
import { of as observableOf } from 'rxjs/observable/of';

import { FabricAuthGrainService } from '../../../services/fabric-auth-grain.service';
import { IGrain } from '../../../models/grain.model';
import { ISecurableItem } from '../../../models/securableItem.model';
import { Router, ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-grain-list',
  templateUrl: './grain-list.component.html',
  styleUrls: ['./grain-list.component.scss', '../access-control.scss']
})
export class GrainListComponent implements OnInit, DoCheck {

  treeControl: FlatTreeControl<GrainFlatNode>;
  treeFlattener: MatTreeFlattener<GrainNode, GrainFlatNode>;
  dataSource: MatTreeFlatDataSource<GrainNode, GrainFlatNode>;

  grains: IGrain[];
  isGrainVisible = false;
  selectedNode: GrainFlatNode;

  private routeGrain: string;
  private routeSecurableItem: string;

  transformer = (node: GrainNode, level: number) => new GrainFlatNode(!!node.children, node.name, node.parentName, level, node.path);

  private _getLevel = (node: GrainFlatNode) => node.level;
  private _isExpandable = (node: GrainFlatNode) => node.expandable;
  private _getChildren = (node: GrainNode): Observable<GrainNode[]> => observableOf(node.children);
  hasChild = (_: number, _nodeData: GrainFlatNode) => _nodeData.expandable;

  constructor(
    private route: ActivatedRoute,
    private grainService: FabricAuthGrainService,
    private router: Router
  ) {
    this.isGrainVisible = grainService.isGrainVisible();
    this.treeFlattener = new MatTreeFlattener(this.transformer, this._getLevel, this._isExpandable, this._getChildren);
    this.treeControl = new FlatTreeControl<GrainFlatNode>(this._getLevel, this._isExpandable);
    this.dataSource = new MatTreeFlatDataSource(this.treeControl, this.treeFlattener);
    this.initialize();
  }

  initialize() {
    this.routeGrain = this.getSelectedGrain();
    if (!!this.routeGrain) {
      this.routeSecurableItem = this.route.snapshot.paramMap.get('securableItem');
    }

    this.initializeGrains();
  }

  initializeGrains() {
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

  ngOnInit() {
  }

  ngDoCheck() {
    // this handles the case when the user enters the app for the
    // first time (no grain or secitem in URL)
    if (!this.routeGrain && !this.getSelectedGrain()) {
      return;
    }

    // this handles the case when the user enters the app for the
    // first time (no grain or secitem in URL), clicks on a different item,
    // and clicks the Back button
    if (!!this.routeGrain && !this.getSelectedGrain()) {
      this.initialize();
      return;
    }

    // this handles the case when the user clicks Back
    // to navigate to a route that had a different grain and sec item
    // selected
    if (this.routeGrain !== this.getSelectedGrain()
          || this.routeSecurableItem !== this.getSelectedSecurableItem()) {
        this.initialize();
    }
  }

  ifExists(obj: any): boolean {
    return !!obj;
  }

  initializeSelectedNode(): void {
    const selectedGrain = this.getSelectedGrain();
    const selectedSecurableItem = this.getSelectedSecurableItem();
    if (this.ifExists(selectedGrain)) {
      const grainNode = this.treeControl.dataNodes.find(node => {
        return node.name === selectedGrain;
      });

      this.treeControl.expand(grainNode);
      this.selectedNode = this.treeControl.dataNodes.find(node => {
        return node.name === selectedSecurableItem && node.parentName === selectedGrain;
      });

      if (!this.selectedNode) {
        this.setSelectedNode('', '');
        this.router.navigateByUrl('/404');
      }

      // if selected node
      if (!this.ifExists(this.selectedNode)) {
        this.selectedNode = this.treeControl.dataNodes.find(node => {
          return node.parentName === selectedGrain;
        });
      }
     } else {
        const grainNode = this.treeControl.dataNodes[0];
        const secondNode = this.treeControl.dataNodes[1];
        this.treeControl.expand(grainNode);

        // if the second node a child, select that.
        // if it is not related, just use the first node.
        if (grainNode.name === secondNode.parentName) {
          this.selectedNode = secondNode;
        } else {
          this.setSelectedNode('', '');
          this.router.navigateByUrl('/404');
        }
      }
  }

  getSelectedGrain() {
    return this.route.snapshot.params['grain'];
  }

  getSelectedSecurableItem() {
    return this.route.snapshot.paramMap.get('securableItem');
  }

  addGrainToGrainNode(data: IGrain[]): GrainNode[] {
    const result: GrainNode[] = [];

    for (const item of data) {
      const node = new GrainNode();
      node.name = item.name;
      node.children = this.addSecurableItemToGrainNode(item.securableItems, item.name);
      node.path = `/access-control/${node.name}`;
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
      node.path = `/access-control/${node.parentName}/${node.name}`;
      result.push(node);
    }

    return result;
  }

  onSelect(node: GrainFlatNode): void {
    if (!!node.parentName) {
      this.selectedNode = node;
    }
  }

  getIcon(node: GrainFlatNode): string {
    const isNodeExpanded = this.treeControl.isExpanded(node);
    const isGrain = !(!!node.parentName);
    const isSelectedNode = this.selectedNode === node;

    if (isGrain && isNodeExpanded) {
      return 'fa-angle-down';
    } else if (isGrain && !isNodeExpanded) {
      return 'fa-angle-right';
    }

    if (!isGrain && isSelectedNode) {
      return 'fa-angle-right';
    } else {
      return 'fa-angle-down';
    }
  }

  isSelectedNode(myNode: GrainFlatNode): string {
    return this.selectedNode === myNode ? 'selected' : '';
  }
}

export class GrainNode {
  children: GrainNode[];
  name: string;
  parentName: string;
  path: string;
}

export class GrainFlatNode {
  constructor(
    public expandable: boolean, public name: string, public parentName: string, public level: number, public path: string) { }
}
