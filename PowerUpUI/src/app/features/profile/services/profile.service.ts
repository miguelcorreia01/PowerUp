import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';

export interface UserProfile {
  id: string;
  name: string;
  email: string;
  phoneNumber?: string;
  role: string;
  createdAt?: string;
  updatedAt?: string;
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
export class ProfileService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5255/api/users';

  // Map enum number to string
  private mapRole(role: string | number | null | undefined, isAdmin?: boolean): string {
    if (isAdmin === true) {
      return 'Admin';
    }
    
    if (typeof role === 'string') {
      return role;
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

  getProfile(userId: string): Observable<UserProfile> {
    return this.http.get<BackendUserResponse>(`${this.baseUrl}/${userId}`).pipe(
      map(response => {
        const roleValue = response.role !== undefined ? response.role : response.Role;
        const isAdminValue = response.isAdmin || response.IsAdmin || false;
        
        return {
          id: response.id || response.Id || '',
          name: response.name || response.Name || '',
          email: response.email || response.Email || '',
          phoneNumber: response.phoneNumber || response.PhoneNumber,
          role: this.mapRole(roleValue, isAdminValue),
          createdAt: response.createdAt || response.CreatedAt,
          updatedAt: response.updatedAt || response.UpdatedAt
        };
      })
    );
  }
  
  updateUserRole(userId: string, role: string): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.baseUrl}/${userId}/role`, { role });
  }
}