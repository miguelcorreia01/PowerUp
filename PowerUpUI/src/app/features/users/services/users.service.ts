import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';

export interface UserListItem {
  id: string;
  name: string;
  email: string;
  phoneNumber?: string;
  role: string;
  isAdmin: boolean;
  createdAt: string;
  updatedAt: string;
}

interface BackendUserResponse {
  id?: string;
  Id?: string;
  name?: string;
  Name?: string;
  email?: string;
  Email?: string;
  phoneNumber?: string;
  PhoneNumber?: string;
  role?: string | number;
  Role?: string | number;
  isAdmin?: boolean;
  IsAdmin?: boolean;
  createdAt?: string;
  CreatedAt?: string;
  updatedAt?: string;
  UpdatedAt?: string;
}

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5255/api/users';

 
private mapRole(role: string | number | null | undefined, isAdmin?: boolean): string {
    // If explicitly marked as admin, return Admin
    if (isAdmin === true) {
      return 'Admin';
    }

    if (typeof role === 'string') {
      const normalized = role.trim();
      if (normalized.toLowerCase() === 'admin') return 'Admin';
      if (normalized.toLowerCase() === 'instructor') return 'Instructor';
      if (normalized.toLowerCase() === 'member') return 'Member';
      return normalized;
    }
    
    if (typeof role === 'number') {
      const roleMap: { [key: number]: string } = {
        0: 'Admin',
        1: 'Instructor',
        2: 'Member'
      };
      return roleMap[role] || 'Member';
    }
    
    return 'Member';
  }

  getAllUsers(): Observable<UserListItem[]> {
    return this.http.get<BackendUserResponse[]>(this.baseUrl).pipe(
      map(users => 
        users.map(user => {
          const roleValue = user.role !== undefined ? user.role : user.Role;
          const isAdminValue = user.isAdmin || user.IsAdmin || false;
          
          return {
            id: user.id || user.Id || '',
            name: user.name || user.Name || '',
            email: user.email || user.Email || '',
            phoneNumber: user.phoneNumber || user.PhoneNumber,
            role: this.mapRole(roleValue, isAdminValue),
            isAdmin: isAdminValue,
            createdAt: user.createdAt || user.CreatedAt || '',
            updatedAt: user.updatedAt || user.UpdatedAt || ''
          };
        })
      )
    );
  }
}