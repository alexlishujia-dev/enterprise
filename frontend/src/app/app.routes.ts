import { Routes } from '@angular/router';

import { authGuard, guestGuard } from './core/guard/auth.guard';



export const routes: Routes = [

  {

    path: 'login',

    canActivate: [guestGuard],

    loadComponent: () => import('./pages/login/login.component').then(m => m.LoginComponent)

  },

  {

    path: '',

    canActivate: [authGuard],

    loadComponent: () => import('./pages/layout/main-layout.component').then(m => m.MainLayoutComponent),

    children: [

      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },

      {

        path: 'dashboard',

        loadComponent: () => import('./pages/dashboard/dashboard.component').then(m => m.DashboardComponent)

      },

      {

        path: 'system/users',

        loadComponent: () => import('./pages/system/user-list.component').then(m => m.UserListComponent)

      },

      {

        path: 'system/users/new',

        loadComponent: () => import('./pages/system/user-form.component').then(m => m.UserFormComponent)

      },

      {

        path: 'system/users/:id/edit',

        loadComponent: () => import('./pages/system/user-form.component').then(m => m.UserFormComponent)

      },

      {

        path: 'system/users/:id/assign-roles',

        loadComponent: () => import('./pages/system/user-assign-roles.component').then(m => m.UserAssignRolesComponent)

      },

      {

        path: 'system/roles',

        loadComponent: () => import('./pages/system/role-list.component').then(m => m.RoleListComponent)

      },

      {

        path: 'system/roles/new',

        loadComponent: () => import('./pages/system/role-form.component').then(m => m.RoleFormComponent)

      },

      {

        path: 'system/roles/:id/edit',

        loadComponent: () => import('./pages/system/role-form.component').then(m => m.RoleFormComponent)

      },

      {

        path: 'system/role-permissions',

        loadComponent: () => import('./pages/system/role-permission-assign.component').then(m => m.RolePermissionAssignComponent)

      },

      {

        path: 'system/logs',

        loadComponent: () => import('./pages/system/log-list.component').then(m => m.LogListComponent)

      }

    ]

  },

  { path: '**', redirectTo: 'dashboard' }

];


