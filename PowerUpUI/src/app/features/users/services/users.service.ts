import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

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

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5255/api/users';

  getAllUsers(): Observable<UserListItem[]> {
    return this.http.get<UserListItem[]>(this.baseUrl);
  }
}