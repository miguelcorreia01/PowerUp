import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable } from 'rxjs';

export interface GroupClass {
  id: string;
  instructorId: string;
  instructor?: { name: string; email: string };
  type: 'Yoga' | 'Pilates' | 'Spinning' | 'Zumba' | 'Crossfit' | 'HIIT' | 'StrengthTraining' | 'Cardio' | 'Jumping' | 'ABS';
  name: string;
  description: string;
  members: Array<{ id: string; name: string }>;
  startTime: string;
  maxCapacity: number;
  currentEnrollment: number;
  isDeleted: boolean;
  deletedAt?: string;
}

export interface CreateGroupClassRequest {
  instructorId: string;
  type: string;
  name: string;
  description: string;
  startTime: string;
  maxCapacity: number;
}

@Injectable({ providedIn: 'root' })
export class GroupClassService {
  private http = inject(HttpClient);
  private baseUrl = 'http://localhost:5255/api/groupclass';

  getAllGroupClasses(): Observable<GroupClass[]> {
    return this.http.get<GroupClass[]>(this.baseUrl);
  }

  getGroupClass(id: string): Observable<GroupClass> {
    return this.http.get<GroupClass>(`${this.baseUrl}/${id}`);
  }

  createGroupClass(request: CreateGroupClassRequest): Observable<GroupClass> {
    return this.http.post<GroupClass>(this.baseUrl, request);
  }

  updateGroupClass(id: string, groupClass: Partial<GroupClass>): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, groupClass);
  }

  deleteGroupClass(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  // Get group classes for a specific week
  getGroupClassesForWeek(startDate: Date, endDate: Date): Observable<GroupClass[]> {
    return this.getAllGroupClasses().pipe(
      map(classes => classes.filter(gc => {
        const classDate = new Date(gc.startTime);
        return classDate >= startDate && classDate <= endDate && !gc.isDeleted;
      }))
    );
  }

  // Get group classes for today
  getTodayGroupClasses(): Observable<GroupClass[]> {
    const today = new Date();
    const startOfDay = new Date(today.getFullYear(), today.getMonth(), today.getDate());
    const endOfDay = new Date(today.getFullYear(), today.getMonth(), today.getDate(), 23, 59, 59);
    
    return this.getGroupClassesForWeek(startOfDay, endOfDay);
  }
}