import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface UserProfile {
  id: string;
  name: string;
  email: string;
  phoneNumber?: string;
  role: string;
  createdAt?: string;
  updatedAt?: string;
}

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5255/api/users';

  getProfile(userId: string) {
    return this.http.get<UserProfile>(`${this.baseUrl}/${userId}`);
  }
}