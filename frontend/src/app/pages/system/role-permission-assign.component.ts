import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzGridModule } from 'ng-zorro-antd/grid';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzTreeModule, NzTreeNodeOptions } from 'ng-zorro-antd/tree';
import { catchError, finalize, map, of } from 'rxjs';
import { ApiBusinessError } from '../../core/models/api-result.model';
import { SysRoleDto } from '../../core/models/sys-role.model';
import { SysMenuService } from '../../service/sys-menu.service';
import { SysRoleService } from '../../service/sys-role.service';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import {
  buildMenuPermissionTree,
  collectMenuKeys,
  collectPermissionKeys,
  filterPermissionTree,
  parsePermissionIdFromKey,
  permNodeKey,
  setTreeExpanded
} from '../../shared/utils/permission-tree.util';

type TreeKey = string | number;

@Component({
  selector: 'app-role-permission-assign',
  standalone: true,
  imports: [
    FormsModule,
    PageHeaderComponent,
    HasPermissionDirective,
    NzCardModule,
    NzGridModule,
    NzInputModule,
    NzButtonModule,
    NzTreeModule,
    NzSpinModule,
    NzTagModule,
    NzEmptyModule
  ],
  templateUrl: './role-permission-assign.component.html',
  styleUrl: './role-permission-assign.component.scss'
})
export class RolePermissionAssignComponent implements OnInit {
  private readonly roleService = inject(SysRoleService);
  private readonly menuService = inject(SysMenuService);
  private readonly message = inject(NzMessageService);

  roleLoading = false;
  permLoading = false;
  saving = false;
  dirty = false;

  roles = signal<SysRoleDto[]>([]);
  roleKeyword = signal('');
  permKeyword = signal('');
  selectedRoleId = signal<number | null>(null);
  checkedKeys = signal<TreeKey[]>([]);
  expandedKeys = signal<TreeKey[]>([]);
  /** 完整权限树（不受搜索影响） */
  fullTreeData = signal<NzTreeNodeOptions[]>([]);

  readonly displayTreeData = computed(() =>
    filterPermissionTree(this.fullTreeData(), this.permKeyword())
  );

  readonly selectedRole = computed(() =>
    this.roles().find(r => r.id === this.selectedRoleId()) ?? null
  );

  readonly filteredRoles = computed(() => {
    const kw = this.roleKeyword().trim().toLowerCase();
    const list = this.roles();
    if (!kw) {
      return list;
    }
    return list.filter(r =>
      r.roleCode.toLowerCase().includes(kw) ||
      r.roleName.toLowerCase().includes(kw)
    );
  });

  readonly allPermissionKeys = computed(() => collectPermissionKeys(this.fullTreeData()));

  readonly visiblePermissionKeys = computed(() => collectPermissionKeys(this.displayTreeData()));

  readonly selectedCount = computed(() =>
    this.checkedKeys().filter(k => String(k).startsWith('p-')).length
  );

  readonly totalPermissionCount = computed(() => this.allPermissionKeys().length);

  ngOnInit(): void {
    this.loadRoles();
    this.loadMenuTree();
  }

  onPermKeywordChange(value: string): void {
    this.permKeyword.set(value);
    const display = filterPermissionTree(this.fullTreeData(), value.trim());
    if (value.trim()) {
      this.expandedKeys.set(collectMenuKeys(display));
    }
  }

  selectRole(role: SysRoleDto): void {
    if (this.selectedRoleId() === role.id) {
      return;
    }
    if (this.dirty && !confirm('当前角色权限尚未保存，确认切换？')) {
      return;
    }

    this.selectedRoleId.set(role.id);
    this.dirty = false;
    this.loadRolePermissions(role.id);
  }

  loadRoles(): void {
    this.roleLoading = true;
    this.roleService.loadRoles({ pageIndex: 1, pageSize: 200 })
      .pipe(finalize(() => (this.roleLoading = false)))
      .subscribe({
        next: res => {
          this.roles.set(res.items);
          if (this.selectedRoleId() == null && res.items.length > 0) {
            this.selectRole(res.items[0]);
          }
        },
        error: (err: unknown) =>
          this.message.error(err instanceof ApiBusinessError ? err.message : '加载角色失败')
      });
  }

  loadMenuTree(): void {
    this.menuService.loadMenuTree().subscribe({
      next: tree => {
        const nodes = buildMenuPermissionTree(tree);
        this.fullTreeData.set(setTreeExpanded(nodes, true));
        this.expandedKeys.set(collectMenuKeys(nodes));
      },
      error: (err: unknown) =>
        this.message.error(err instanceof ApiBusinessError ? err.message : '加载权限树失败')
    });
  }

  loadRolePermissions(roleId: number): void {
    this.permLoading = true;
    this.roleService.getRolePermissions(roleId)
      .pipe(finalize(() => (this.permLoading = false)))
      .subscribe({
        next: ids => {
          this.checkedKeys.set(ids.map(id => permNodeKey(id)));
          this.dirty = false;
        },
        error: (err: unknown) =>
          this.message.error(err instanceof ApiBusinessError ? err.message : '加载角色权限失败')
      });
  }

  onCheckedKeysChange(keys: TreeKey[]): void {
    this.checkedKeys.set(keys);
    this.dirty = true;
  }

  expandAll(): void {
    const source = this.permKeyword().trim()
      ? this.displayTreeData()
      : this.fullTreeData();
    this.expandedKeys.set(collectMenuKeys(source));
    if (!this.permKeyword().trim()) {
      this.fullTreeData.set(setTreeExpanded(this.fullTreeData(), true));
    }
  }

  collapseAll(): void {
    this.expandedKeys.set([]);
    if (!this.permKeyword().trim()) {
      this.fullTreeData.set(setTreeExpanded(this.fullTreeData(), false));
    }
  }

  selectAll(): void {
    const keys = this.permKeyword().trim()
      ? this.visiblePermissionKeys()
      : this.allPermissionKeys();
    this.checkedKeys.set([...new Set([...this.checkedKeys(), ...keys])]);
    this.dirty = true;
  }

  clearAll(): void {
    const visible = new Set(
      this.permKeyword().trim() ? this.visiblePermissionKeys() : this.allPermissionKeys()
    );
    this.checkedKeys.set(this.checkedKeys().filter(k => !visible.has(String(k))));
    this.dirty = true;
  }

  resetCurrent(): void {
    const roleId = this.selectedRoleId();
    if (roleId == null) {
      return;
    }
    this.loadRolePermissions(roleId);
  }

  save(): void {
    const roleId = this.selectedRoleId();
    if (roleId == null) {
      this.message.warning('请先选择角色');
      return;
    }

    const permissionIds = this.checkedKeys()
      .map(k => parsePermissionIdFromKey(String(k)))
      .filter((id): id is number => id != null);

    this.saving = true;
    this.roleService.assignRolePermissions(roleId, { permissionIds }).pipe(
      map(() => {
        this.message.success('权限保存成功');
        this.dirty = false;
        return true;
      }),
      catchError((err: unknown) => {
        this.message.error(err instanceof ApiBusinessError ? err.message : '保存失败');
        return of(false);
      }),
      finalize(() => (this.saving = false))
    ).subscribe();
  }
}
